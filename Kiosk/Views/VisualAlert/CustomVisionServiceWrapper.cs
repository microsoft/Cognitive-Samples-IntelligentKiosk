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

using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace IntelligentKioskSample.Views.VisualAlert
{
    public class CustomVisionServiceWrapper
    {
        public const string NegativeTag = "Negative";

        private readonly CustomVisionTrainingClient trainingApi;
        private readonly ProjectDomainViewModel projectDomain = new ProjectDomainViewModel
        {
            DomainId = new Guid("0732100f-1a38-4e49-a514-c9b44c697ab5"),
            DisplayName = "Image Classification, General (exportable)"
        };

        public CustomVisionServiceWrapper(string apiKey, string endpoint)
        {
            trainingApi = new CustomVisionTrainingClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(apiKey))
            {
                Endpoint = endpoint
            };
        }

        public async Task<Project> CreateVisualAlertProjectAsync(string name, List<ImageAnalyzer> posImages, List<ImageAnalyzer> negImages)
        {
            Project project = null;
            try
            {
                // project
                project = await trainingApi.CreateProjectAsync(name, name, projectDomain.DomainId);

                // tag: regular and negative
                Task<Tag> tagTask = trainingApi.CreateTagAsync(project.Id, name);
                Task<Tag> negativeTagTask = trainingApi.CreateTagAsync(project.Id, NegativeTag, type: NegativeTag);
                await Task.WhenAll(tagTask, negativeTagTask);

                // images: with and w/o subject
                Task addPosImagesTask = AddTrainingImagesAsync(posImages, project.Id, tagTask.Result);
                Task addNegImagesTask = AddTrainingImagesAsync(negImages, project.Id, negativeTagTask.Result);
                await Task.WhenAll(addPosImagesTask, addNegImagesTask);
            }
            catch (Exception ex)
            {
                if (SettingsHelper.Instance.ShowDebugInfo)
                {
                    await Util.GenericApiCallExceptionHandler(ex, "Failure creating project");
                }
            }

            return project;
        }

        public async Task AddTrainingImagesAsync(IEnumerable<ImageAnalyzer> images, Guid projectId, Tag tag = null)
        {
            foreach (var item in images)
            {
                ImageCreateSummary addResult;
                if (item.GetImageStreamCallback != null)
                {
                    addResult = await trainingApi.CreateImagesFromDataAsync(
                        projectId,
                        await item.GetImageStreamCallback(), tag != null ? new List<Guid> { tag.Id } : null);
                }
                else
                {
                    addResult = await trainingApi.CreateImagesFromUrlsAsync(
                        projectId,
                        new ImageUrlCreateBatch(new ImageUrlCreateEntry[] { new ImageUrlCreateEntry(item.ImageUrl) }, tag != null ? new Guid[] { tag.Id } : null));
                }
            }
        }

        public async Task<Iteration> TrainProjectAsync(Guid projectId)
        {
            Iteration iteration = null;
            try
            {
                iteration = await trainingApi.TrainProjectAsync(projectId);

                while (true)
                {
                    iteration = await trainingApi.GetIterationAsync(projectId, iteration.Id);

                    if (iteration.Status != "Training")
                    {
                        break;
                    }
                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                if (SettingsHelper.Instance.ShowDebugInfo)
                {
                    await Util.GenericApiCallExceptionHandler(ex, "Failure requesting training");
                }
            }

            return iteration;
        }

        public async Task<VisualAlertScenarioData> ExportOnnxProject(Project project)
        {
            // get latest iteration
            IList<Iteration> iterations = await trainingApi.GetIterationsAsync(project.Id);
            Iteration latestTrainedIteration = iterations.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();

            // export iteration - get download url
            Export exportProject = null;
            if (latestTrainedIteration != null && latestTrainedIteration.Exportable)
            {
                // get project's download Url for the particular platform Windows (ONNX) model
                exportProject = await CustomVisionServiceHelper.ExportIteration(trainingApi, project.Id, latestTrainedIteration.Id);
            }

            if (string.IsNullOrEmpty(exportProject?.DownloadUri))
            {
                throw new ArgumentNullException("Download Uri");
            }

            // download onnx model
            Guid newModelId = Guid.NewGuid();
            StorageFolder onnxProjectDataFolder = await VisualAlertDataLoader.GetOnnxModelStorageFolderAsync();
            StorageFile file = await onnxProjectDataFolder.CreateFileAsync($"{newModelId}.onnx", CreationCollisionOption.ReplaceExisting);
            bool success = await Util.UnzipModelFileAsync(exportProject.DownloadUri, file);

            if (!success)
            {
                await file.DeleteAsync();
                return null;
            }

            return new VisualAlertScenarioData
            {
                Id = newModelId,
                Name = project.Name,
                ExportDate = DateTime.UtcNow,
                FileName = file.Name,
                FilePath = file.Path
            };
        }

        public async Task DeleteProjectAsync(Project project)
        {
            try
            {
                await trainingApi.DeleteProjectAsync(project.Id);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting project");
            }
        }
    }
}
