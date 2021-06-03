// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Rest;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace IntelligentKioskSample.Views.DigitalAssetManagement
{
    public class ImageProcessor
    {
        public async Task<DigitalAssetData> ProcessImagesAsync(ImageProcessorSource source, IImageProcessorService[] services, int? fileLimit, int? startingFileIndex, Func<ImageInsights, Task> callback)
        {
            //validate
            if (services == null || services.Length == 0)
            {
                throw new ApplicationException("No Azure services provided for image processing pipeline. Need at least 1.");
            }

            //get images
            var insights = new List<ImageInsights>();
            var (filePaths, reachedEndOfFiles) = await source.GetFilePaths(fileLimit, startingFileIndex);

            //process each image - in batches
            var tasks = new List<Task<ImageInsights>>();
            var lastFile = filePaths.Last();
            foreach (var filePath in filePaths)
            {
                tasks.Add(ProcessImageAsync(filePath, services));
                if (tasks.Count == 8 || filePath == lastFile)
                {
                    var results = await Task.WhenAll(tasks);
                    foreach (var task in tasks)
                    {
                        var insight = task.Result;
                        insights.Add(insight);
                        await callback(insight);
                    }
                    tasks.Clear();
                }
            }

            var serviceTypes = services.Select(i => i.GetProcessorServiceType).Aggregate((i, e) => i | e);
            var customVisionProjects = services.OfType<CustomVisionProcessorService>().SelectMany(i => i.ProjectIterations.Select(e => e.Project)).ToArray();
            return new DigitalAssetData()
            {
                Info = new DigitalAssetInfo
                {
                    Path = source.Path,
                    FileLimit = fileLimit,
                    Services = serviceTypes,
                    CustomVisionProjects = customVisionProjects,
                    Name = source.GetName(),
                    LastFileIndex = filePaths.Count() + (startingFileIndex ?? 0),
                    ReachedEndOfFiles = reachedEndOfFiles,
                    Source = source.GetType().Name
                },
                Insights = insights.ToArray()
            };
        }

        async Task<ImageInsights> ProcessImageAsync(Uri filePath, IImageProcessorService[] services)
        {
            ImageAnalyzer analyzer = null;
            if (filePath.IsFile)
            {
                analyzer = new ImageAnalyzer((await StorageFile.GetFileFromPathAsync(filePath.LocalPath)).OpenStreamForReadAsync);
            }
            else
            {
                analyzer = new ImageAnalyzer(filePath.AbsoluteUri);
            }
            analyzer.ShowDialogOnFaceApiErrors = true;

            //run image processors
            var tasks = services.Select(i => (i.ProcessImage(analyzer).ToArray())).ToArray();
            await Task.WhenAll(tasks.SelectMany(i => i));

            //run post image processor
            await Task.WhenAll(services.SelectMany(i => (i.PostProcessImage(analyzer))));

            //assign the results
            var result = new ImageInsights { ImageUri = filePath };
            for (int index = 0; index < services.Length; index++)
            {
                services[index].AssignResult(tasks[index], analyzer, result);
            }

            return result;
        }
    }

    public interface IImageProcessorService
    {
        ImageProcessorServiceType GetProcessorServiceType { get; }
        IEnumerable<Task> ProcessImage(ImageAnalyzer analyzer);
        IEnumerable<Task> PostProcessImage(ImageAnalyzer analyzer);
        void AssignResult(IEnumerable<Task> completeTask, ImageAnalyzer analyzer, ImageInsights result);
    }

    public class FaceProcessorService : IImageProcessorService
    {
        static readonly FaceAttributeType[] _faceFeatures = new[]
        {
            FaceAttributeType.Accessories,
            FaceAttributeType.Age,
            FaceAttributeType.Blur,
            FaceAttributeType.Emotion,
            FaceAttributeType.Exposure,
            FaceAttributeType.FacialHair,
            FaceAttributeType.Gender,
            FaceAttributeType.Glasses,
            FaceAttributeType.Hair,
            FaceAttributeType.HeadPose,
            FaceAttributeType.Makeup,
            FaceAttributeType.Noise,
            FaceAttributeType.Occlusion,
            FaceAttributeType.Smile
        };

        public ImageProcessorServiceType GetProcessorServiceType { get => ImageProcessorServiceType.Face; }
        public IEnumerable<Task> ProcessImage(ImageAnalyzer analyzer)
        {
            yield return analyzer.DetectFacesAsync(true, false, _faceFeatures);
        }

        public IEnumerable<Task> PostProcessImage(ImageAnalyzer analyzer)
        {
            yield return analyzer.FindSimilarPersistedFacesAsync();
        }

        public void AssignResult(IEnumerable<Task> completeTask, ImageAnalyzer analyzer, ImageInsights result)
        {
            // assign face api results
            List<FaceInsights> faceInsightsList = new List<FaceInsights>();
            foreach (var face in analyzer.DetectedFaces ?? Array.Empty<DetectedFace>())
            {
                FaceInsights faceInsights = new FaceInsights { FaceRectangle = face.FaceRectangle, FaceAttributes = face.FaceAttributes };

                var similarFaceMatch = analyzer.SimilarFaceMatches?.FirstOrDefault(s => s.Face.FaceId == face.FaceId);
                if (similarFaceMatch != null)
                {
                    faceInsights.UniqueFaceId = similarFaceMatch.SimilarPersistedFace.PersistedFaceId.GetValueOrDefault();
                }

                faceInsightsList.Add(faceInsights);
            }
            result.FaceInsights = faceInsightsList.ToArray();
        }
    }

    public class ComputerVisionProcessorService : IImageProcessorService
    {
        static readonly VisualFeatureTypes?[] _visionFeatures = new VisualFeatureTypes?[]
        {
            VisualFeatureTypes.Tags,
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Objects,
            VisualFeatureTypes.Brands,
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Adult,
            VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Color
        };

        public ImageProcessorServiceType GetProcessorServiceType { get => ImageProcessorServiceType.ComputerVision; }
        public IEnumerable<Task> ProcessImage(ImageAnalyzer analyzer)
        {
            yield return analyzer.AnalyzeImageAsync(null, visualFeatures: _visionFeatures);
            yield return analyzer.RecognizeTextAsync();
        }

        public IEnumerable<Task> PostProcessImage(ImageAnalyzer analyzer)
        {
            yield break;
        }

        public void AssignResult(IEnumerable<Task> completeTask, ImageAnalyzer analyzer, ImageInsights result)
        {
            // assign computer vision results
            result.VisionInsights = new VisionInsights
            {
                Caption = analyzer.AnalysisResult?.Description?.Captions.FirstOrDefault()?.Text,
                Tags = analyzer.AnalysisResult?.Tags != null ? analyzer.AnalysisResult.Tags.Select(t => t.Name).ToArray() : new string[0],
                Objects = analyzer.AnalysisResult?.Objects != null ? analyzer.AnalysisResult.Objects.Select(t => t.ObjectProperty).ToArray() : new string[0],
                Celebrities = analyzer.AnalysisResult?.Categories != null ? analyzer.AnalysisResult.Categories.Where(i => i.Detail?.Celebrities != null && i.Detail.Celebrities.Count != 0).SelectMany(i => i.Detail.Celebrities).Select(i => i.Name).ToArray() : new string[0],
                Landmarks = analyzer.AnalysisResult?.Categories != null ? analyzer.AnalysisResult.Categories.Where(i => i.Detail?.Landmarks != null && i.Detail.Landmarks.Count != 0).SelectMany(i => i.Detail.Landmarks).Select(i => i.Name).ToArray() : new string[0],
                Brands = analyzer.AnalysisResult?.Brands != null ? analyzer.AnalysisResult.Brands.Select(t => t.Name).ToArray() : new string[0],
                Adult = analyzer.AnalysisResult?.Adult,
                Color = analyzer.AnalysisResult?.Color,
                ImageType = analyzer.AnalysisResult?.ImageType,
                Metadata = analyzer.AnalysisResult?.Metadata,
                Words = analyzer.TextOperationResult?.Lines != null ? analyzer.TextOperationResult.Lines.SelectMany(i => i.Words).Select(i => i.Text).ToArray() : new string[0],
            };
        }
    }

    public class CustomVisionProcessorService : IImageProcessorService
    {
        SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        ICustomVisionPredictionClient _predictionClient;
        public ProjectIteration[] ProjectIterations { get; }

        public CustomVisionProcessorService(ICustomVisionPredictionClient predictionClient, ProjectIteration[] projectIterations)
        {
            //set fields
            _predictionClient = predictionClient;
            ProjectIterations = projectIterations;
        }

        public static async Task<ProjectIteration[]> GetProjectIterations(ICustomVisionTrainingClient trainingClient, string predictionResourceId, Guid[] projects)
        {
            var result = new List<ProjectIteration>();
            foreach (var project in projects)
            {
                var iterations = await trainingClient.GetIterationsAsync(project);
                var projectEntity = await trainingClient.GetProjectAsync(project);
                var domain = await trainingClient.GetDomainAsync(projectEntity.Settings.DomainId);
                var iteration = iterations.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();
                
                if (iteration != null)
                {
                    if (string.IsNullOrEmpty(iteration.PublishName))
                    {
                        await trainingClient.PublishIterationAsync(project, iteration.Id, publishName: iteration.Id.ToString(), predictionId: predictionResourceId);
                        iteration = await trainingClient.GetIterationAsync(project, iteration.Id);
                    }

                    result.Add(new ProjectIteration() { Project = project, Iteration = iteration.Id, ProjectName = projectEntity.Name, PublishedName = iteration.PublishName, IsObjectDetection = domain.Type == "ObjectDetection" });
                }
            }
            return result.ToArray();
        }

        public ImageProcessorServiceType GetProcessorServiceType { get => ImageProcessorServiceType.CustomVision; }
        public IEnumerable<Task> ProcessImage(ImageAnalyzer analyzer)
        {
            foreach (var projectIteration in ProjectIterations)
            {
                yield return PredictImage(analyzer, projectIteration, ProjectIterations.Length);
            }
        }

        async Task<ValueTuple<ImagePrediction, ProjectIteration>> PredictImage(ImageAnalyzer analyzer, ProjectIteration projectIteration, int projectCount)
        {
            if (analyzer.ImageUrl != null)
            {
                return (await AutoRetry(async () => await _predictionClient.ClassifyImageUrlAsync(projectIteration.Project, projectIteration.PublishedName, new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl(analyzer.ImageUrl))), projectIteration);
            }
            else
            {
                //bug fix: if multiple project needed to be processed for a local file, create requests for them one at a time. 
                //(for some reason when the CustomVision client creates multiple requests using the same file stream it locks up creating the request - a better fix would be good as this one degrades speed)
                var allowAsync = projectCount == 1;

                return (await RunSequentially(async () => await AutoRetry(async () => await _predictionClient.ClassifyImageAsync(projectIteration.Project, projectIteration.PublishedName, await analyzer.GetImageStreamCallback())), allowAsync), projectIteration);
            }
        }

        async Task<TResponse> RunSequentially<TResponse>(Func<Task<TResponse>> action, bool allowAsync)
        {
            if (!allowAsync)
            {
                await _semaphore.WaitAsync();
            }
            try
            {
                return await action();
            }
            finally
            {
                if (!allowAsync)
                {
                    _semaphore.Release();
                }
            }
        }

        async Task<TResponse> AutoRetry<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = 6;
            int delay = 500;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (HttpOperationException exception) when (exception.Response.StatusCode == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    ErrorTrackingHelper.TrackException(exception, "Custom Vision API throttling error");

                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        public IEnumerable<Task> PostProcessImage(ImageAnalyzer analyzer)
        {
            yield break;
        }

        public void AssignResult(IEnumerable<Task> completeTask, ImageAnalyzer analyzer, ImageInsights result)
        {
            result.CustomVisionInsights = completeTask.OfType<Task<ValueTuple<ImagePrediction, ProjectIteration>>>().Select(i => new CustomVisionInsights
            {
                Name = i.Result.Item2.ProjectName,
                IsObjectDetection = i.Result.Item2.IsObjectDetection,
                Predictions = i.Result.Item1.Predictions.Select(e => new CustomVisionPrediction
                {
                    Name = e.TagName,
                    Probability = e.Probability
                }).ToArray()
            }).ToArray();
        }

        public class ProjectIteration
        {
            public Guid Project { get; set; }
            public Guid Iteration { get; set; }
            public string ProjectName { get; set; }
            public string PublishedName { get; set; }
            public bool IsObjectDetection { get; set; }
        }
    }

    public abstract class ImageProcessorSource
    {
        protected string[] ValidExtentions = { ".png", ".jpg", ".bmp", ".jpeg", ".gif" };
        public Uri Path { get; }

        public ImageProcessorSource(Uri path)
        {
            //set fields
            Path = path;
        }

        public abstract Task<(IEnumerable<Uri>, bool)> GetFilePaths(int? fileLimit, int? startingIndex);
        public abstract string GetName();
    }

    public class StorageSource : ImageProcessorSource
    {
        public StorageSource(Uri path) : base(path) { }

        public override async Task<(IEnumerable<Uri>, bool)> GetFilePaths(int? fileLimit, int? startingIndex)
        {
            //calculate max results
            var maxResults = fileLimit;
            if (fileLimit != null && startingIndex != null)
            {
                maxResults = fileLimit + startingIndex;
            }

            var container = new CloudBlobContainer(Path);
            var results = new List<Uri>();
            var reachedFileLimit = false;
            var skipped = 0;
            var files = await container.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, maxResults, null, null, null);
            BlobContinuationToken continuationToken = null;
            do
            {
                foreach (var file in files.Results)
                {
                    //skip if not the right extention
                    var extentionIndex = file.Uri.LocalPath.LastIndexOf('.');
                    if (extentionIndex >= 0 && ValidExtentions.Contains(file.Uri.LocalPath.Substring(extentionIndex), StringComparer.OrdinalIgnoreCase))
                    {
                        //skip
                        if (startingIndex != null && skipped != startingIndex)
                        {
                            skipped++;
                            continue;
                        }

                        //create file URI
                        var root = Path.AbsoluteUri.Remove(Path.AbsoluteUri.Length - Path.PathAndQuery.Length);
                        var query = Path.Query;
                        var path = file.Uri.LocalPath;
                        var fileUri = new Uri(root + path + query);
                        results.Add(fileUri);

                        //at file limit
                        if (fileLimit != null && results.Count >= fileLimit.Value)
                        {
                            reachedFileLimit = true;
                            break;
                        }
                    }
                }

                //get more results
                continuationToken = files.ContinuationToken;
                if (files.ContinuationToken != null && !reachedFileLimit)
                {
                    files = await container.ListBlobsSegmentedAsync(null, false, BlobListingDetails.Metadata, maxResults, files.ContinuationToken, null, null);
                }
                else
                {
                    break;
                }
            } while (continuationToken != null);

            //determine if we reached the end of all files
            var reachedEndOfFiles = !reachedFileLimit;
            if (reachedEndOfFiles && files.ContinuationToken != null)
            {
                reachedEndOfFiles = false;
            }

            return (results.ToArray(), reachedEndOfFiles);
        }

        public override string GetName()
        {
            return Uri.UnescapeDataString(Path.LocalPath.Replace(@"/", string.Empty));
        }
    }

    public class FileSource : ImageProcessorSource
    {
        public FileSource(Uri path) : base(path) { }

        public override async Task<(IEnumerable<Uri>, bool)> GetFilePaths(int? fileLimit, int? startingIndex)
        {
            //calulate new file limit
            if (fileLimit != null && startingIndex != null)
            {
                fileLimit = fileLimit + startingIndex;
            }

            var folder = await StorageFolder.GetFolderFromPathAsync(Path.LocalPath);
            var query = folder.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.DefaultQuery, ValidExtentions));
            var files = fileLimit != null ? await query.GetFilesAsync(0, (uint)fileLimit.Value) : await query.GetFilesAsync();
            var filePaths = files.Select(i => new Uri(i.Path));
            if (startingIndex != null)
            {
                filePaths = filePaths.Skip(startingIndex.Value);
            }
            var result = filePaths.ToArray();
            var reachedEndOfFiles = fileLimit == null || result.Length < fileLimit;
            return (result, reachedEndOfFiles);
        }

        public override string GetName()
        {
            return Uri.UnescapeDataString(Path.Segments.Last());
        }
    }
}
