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
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ServiceHelpers
{
    public partial class ImageAnalyzer
    {
        private static readonly FaceAttributeType[] DefaultFaceAttributeTypes = new FaceAttributeType[]
        {
            FaceAttributeType.Age,
            FaceAttributeType.Gender,
            FaceAttributeType.HeadPose,
            FaceAttributeType.Emotion
        };
        private static readonly List<VisualFeatureTypes?> DefaultVisualFeatures = new List<VisualFeatureTypes?>()
        {
            VisualFeatureTypes.Tags,
            VisualFeatureTypes.Faces,
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Color,
            VisualFeatureTypes.Brands,
            VisualFeatureTypes.Adult,
            VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Objects
        };

        public static string PeopleGroupsUserDataFilter = null;

        public Func<Task<Stream>> GetImageStreamCallback { get; set; }
        public string LocalImagePath { get; set; }
        public string ImageUrl { get; set; }

        public IEnumerable<DetectedFace> DetectedFaces { get; set; }

        public IEnumerable<IdentifiedPerson> IdentifiedPersons { get; set; }

        public IEnumerable<SimilarFaceMatch> SimilarFaceMatches { get; set; }

        public ImageAnalysis AnalysisResult { get; set; }
        public ImageDescription ImageDescription { get; set; }
        public ReadResult TextOperationResult { get; set; }

        // Default to no errors, since this could trigger a stream of popup errors since we might call this
        // for several images at once while auto-detecting the Bing Image Search results.
        public bool ShowDialogOnFaceApiErrors { get; set; } = false;

        public bool FilterOutSmallFaces { get; set; } = false;

        public int DecodedImageHeight { get; private set; }
        public int DecodedImageWidth { get; private set; }
        public byte[] Data { get; set; }

        public ImageAnalyzer(string url)
        {
            this.ImageUrl = url;
        }

        public ImageAnalyzer(Func<Task<Stream>> getStreamCallback, string path = null)
        {
            this.GetImageStreamCallback = getStreamCallback;
            this.LocalImagePath = path;
        }

        public ImageAnalyzer(byte[] data)
        {
            this.Data = data;
            this.GetImageStreamCallback = () => Task.FromResult<Stream>(new MemoryStream(this.Data));
        }

        public void UpdateDecodedImageSize(int height, int width)
        {
            this.DecodedImageHeight = height;
            this.DecodedImageWidth = width;
        }

        public async Task<ImageSource> GetImageSource()
        {
            //get image from url
            if (ImageUrl != null)
            {
                return new BitmapImage(new Uri(ImageUrl));
            }

            //get image from stream
            else if (GetImageStreamCallback != null)
            {
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync((await GetImageStreamCallback()).AsRandomAccessStream());
                return bitmap;
            }

            return null;
        }

        public async Task DetectFacesAsync(bool detectFaceAttributes = false, bool detectFaceLandmarks = false, FaceAttributeType[] faceAttributeTypes = null)
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.DetectedFaces = await FaceServiceHelper.DetectWithUrlAsync(
                        this.ImageUrl,
                        returnFaceId: true,
                        returnFaceLandmarks: detectFaceLandmarks,
                        returnFaceAttributes: detectFaceAttributes ? faceAttributeTypes ?? DefaultFaceAttributeTypes : null);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.DetectedFaces = await FaceServiceHelper.DetectWithStreamAsync(
                        this.GetImageStreamCallback,
                        returnFaceId: true,
                        returnFaceLandmarks: detectFaceLandmarks,
                        returnFaceAttributes: detectFaceAttributes ? faceAttributeTypes ?? DefaultFaceAttributeTypes : null);
                }

                if (this.FilterOutSmallFaces)
                {
                    this.DetectedFaces = this.DetectedFaces.Where(f => CoreUtil.IsFaceBigEnoughForDetection(f.FaceRectangle.Height, this.DecodedImageHeight));
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Face API DetectAsync error");

                this.DetectedFaces = Enumerable.Empty<DetectedFace>();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Face API failed.");
                }
            }
        }

        public async Task DescribeAsync()
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.ImageDescription = await VisionServiceHelper.DescribeAsync(this.ImageUrl);
                    this.AnalysisResult = GetAnalysisResult(this.ImageDescription);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.ImageDescription = await VisionServiceHelper.DescribeAsync(this.GetImageStreamCallback);
                    this.AnalysisResult = GetAnalysisResult(this.ImageDescription);
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Vision API DescribeAsync error");

                this.AnalysisResult = new ImageAnalysis();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Vision API failed.");
                }
            }
        }

        public async Task IdentifyCelebrityAsync()
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.AnalysisResult = await VisionServiceHelper.AnalyzeImageAsync(this.ImageUrl);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.AnalysisResult = await VisionServiceHelper.AnalyzeImageAsync(
                        this.GetImageStreamCallback,
                        new List<VisualFeatureTypes?>() { VisualFeatureTypes.Categories },
                        new List<Details?>() { Details.Celebrities });
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Vision API AnalyzeImageAsync error");

                this.AnalysisResult = new ImageAnalysis();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Vision API failed.");
                }
            }
        }

        public async Task AnalyzeImageAsync(IList<Details?> details = null, IList<VisualFeatureTypes?> visualFeatures = null)
        {
            try
            {
                if (visualFeatures == null)
                {
                    visualFeatures = DefaultVisualFeatures;
                }

                if (this.ImageUrl != null)
                {
                    this.AnalysisResult = await VisionServiceHelper.AnalyzeImageAsync(
                        this.ImageUrl,
                        visualFeatures,
                        details);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.AnalysisResult = await VisionServiceHelper.AnalyzeImageAsync(
                        this.GetImageStreamCallback,
                        visualFeatures,
                        details);
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Vision API AnalyzeImageAsync error");

                this.AnalysisResult = new ImageAnalysis();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Vision API failed.");
                }
            }
        }

        public async Task RecognizeTextAsync()
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.TextOperationResult = (await VisionServiceHelper.ReadFileAsync(this.ImageUrl))?.ReadResults?.FirstOrDefault();
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.TextOperationResult = (await VisionServiceHelper.ReadFileAsync(this.GetImageStreamCallback))?.ReadResults?.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorTrackingHelper.TrackException(ex, "Vision API RecognizeTextAsync error");

                this.TextOperationResult = new ReadResult();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(ex, "Vision API failed.");
                }
            }
        }

        public async Task IdentifyFacesAsync()
        {
            this.IdentifiedPersons = Enumerable.Empty<IdentifiedPerson>();

            Guid[] detectedFaceIds = this.DetectedFaces?.Where(f => f.FaceId.HasValue).Select(f => f.FaceId.GetValueOrDefault()).ToArray();
            if (detectedFaceIds != null && detectedFaceIds.Any())
            {
                List<IdentifiedPerson> result = new List<IdentifiedPerson>();

                var personGroupResultTasks = new List<Tuple<string, Task<IList<IdentifyResult>>>>();
                IEnumerable<PersonGroup> personGroups = await GetPersonGroupsAsync();
                foreach (PersonGroup group in personGroups)
                {
                    personGroupResultTasks.Add(new Tuple<string, Task<IList<IdentifyResult>>>(group.PersonGroupId, GetFaceIdentificationResultsOrDefaultAsync(group.PersonGroupId, detectedFaceIds)));
                }

                // identify detected faces using person groups
                await Task.WhenAll(personGroupResultTasks.Select(x => x.Item2));

                // check matches
                foreach (var group in personGroupResultTasks)
                {
                    foreach (IdentifyResult match in group.Item2.Result)
                    {
                        if (!match.Candidates.Any())
                        {
                            continue;
                        }

                        Person person = null;
                        try
                        {
                            person = await FaceServiceHelper.GetPersonAsync(group.Item1, match.Candidates[0].PersonId);
                        }
                        catch { /* Ignore errors */ }

                        if (person == null)
                        {
                            continue;
                        }

                        IdentifiedPerson alreadyIdentifiedPerson = result.FirstOrDefault(p => p.Person.PersonId == match.Candidates[0].PersonId);
                        if (alreadyIdentifiedPerson != null)
                        {
                            // We already tagged this person in another group. Replace the existing one if this new one if the confidence is higher.
                            if (alreadyIdentifiedPerson.Confidence < match.Candidates[0].Confidence)
                            {
                                alreadyIdentifiedPerson.Person = person;
                                alreadyIdentifiedPerson.Confidence = match.Candidates[0].Confidence;
                                alreadyIdentifiedPerson.FaceId = match.FaceId;
                            }
                        }
                        else
                        {
                            result.Add(new IdentifiedPerson { Person = person, Confidence = match.Candidates[0].Confidence, FaceId = match.FaceId });
                        }
                    }
                }

                this.IdentifiedPersons = result;
            }
        }

        private async Task<IList<IdentifyResult>> GetFaceIdentificationResultsOrDefaultAsync(string personGroupId, Guid[] detectedFaceIds)
        {
            try
            {
                return await FaceServiceHelper.IdentifyAsync(personGroupId, detectedFaceIds);
            }
            catch (Exception e)
            {
                // Catch errors with individual groups so we can continue looping through all groups. Maybe an answer will come from
                // another one.
                ErrorTrackingHelper.TrackException(e, "Face API IdentifyAsync error");

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Failure identifying faces");
                }
            }
            return new List<IdentifyResult>();
        }

        public async Task FindSimilarPersistedFacesAsync()
        {
            this.SimilarFaceMatches = Enumerable.Empty<SimilarFaceMatch>();

            if (this.DetectedFaces == null || !this.DetectedFaces.Any())
            {
                return;
            }

            List<SimilarFaceMatch> result = new List<SimilarFaceMatch>();

            foreach (DetectedFace detectedFace in this.DetectedFaces)
            {
                try
                {
                    SimilarFace similarPersistedFace = null;
                    if (this.ImageUrl != null)
                    {
                        similarPersistedFace = await FaceListManager.FindSimilarPersistedFaceAsync(ImageUrl, detectedFace.FaceId.GetValueOrDefault(), detectedFace);
                    }
                    else
                    {
                        similarPersistedFace = await FaceListManager.FindSimilarPersistedFaceAsync(this.GetImageStreamCallback, detectedFace.FaceId.GetValueOrDefault(), detectedFace);
                    }
                    if (similarPersistedFace != null)
                    {
                        result.Add(new SimilarFaceMatch { Face = detectedFace, SimilarPersistedFace = similarPersistedFace });
                    }
                }
                catch (Exception e)
                {
                    ErrorTrackingHelper.TrackException(e, "FaceListManager.FindSimilarPersistedFaceAsync error");

                    if (this.ShowDialogOnFaceApiErrors)
                    {
                        await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Failure finding similar faces");
                    }
                }
            }

            this.SimilarFaceMatches = result;
        }

        private ImageAnalysis GetAnalysisResult(ImageDescription imageDescription)
        {
            return new ImageAnalysis()
            {
                RequestId = imageDescription.RequestId,
                Metadata = imageDescription.Metadata,
                Description = new ImageDescriptionDetails(imageDescription.Tags, imageDescription.Captions)
            };
        }

        private async Task<IEnumerable<PersonGroup>> GetPersonGroupsAsync()
        {
            IEnumerable<PersonGroup> personGroups = Enumerable.Empty<PersonGroup>();
            try
            {
                personGroups = (await FaceServiceHelper.ListPersonGroupsAsync(PeopleGroupsUserDataFilter))
                    .Where(x => x.RecognitionModel.Equals(FaceServiceHelper.LatestRecognitionModelName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Face API GetPersonGroupsAsync error");

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Failure getting PersonGroups");
                }
            }
            return personGroups;
        }

    }

    public class IdentifiedPerson
    {
        public double Confidence
        {
            get; set;
        }

        public Person Person
        {
            get; set;
        }

        public Guid FaceId
        {
            get; set;
        }
    }

    public class SimilarFaceMatch
    {
        public DetectedFace Face
        {
            get; set;
        }

        public SimilarFace SimilarPersistedFace
        {
            get; set;
        }
    }
}
