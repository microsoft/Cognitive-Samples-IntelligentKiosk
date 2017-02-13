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

using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class ImageAnalyzer
    {
        private static FaceAttributeType[] DefaultFaceAttributeTypes = new FaceAttributeType[] { FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.HeadPose };

        public event EventHandler FaceDetectionCompleted;
        public event EventHandler FaceRecognitionCompleted;
        public event EventHandler EmotionRecognitionCompleted;

        public static string PeopleGroupsUserDataFilter = null;

        public Func<Task<Stream>> GetImageStreamCallback { get; set; }
        public string LocalImagePath { get; set; }
        public string ImageUrl { get; set; }

        public IEnumerable<Face> DetectedFaces { get; set; }

        public IEnumerable<Emotion> DetectedEmotion { get; set; }

        public IEnumerable<IdentifiedPerson> IdentifiedPersons { get; set; }

        public IEnumerable<SimilarFaceMatch> SimilarFaceMatches { get; set; }

        public Microsoft.ProjectOxford.Vision.Contract.AnalysisResult AnalysisResult { get; set; }

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

        public async Task DetectFacesAsync(bool detectFaceAttributes = false, bool detectFaceLandmarks = false)
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.DetectedFaces = await FaceServiceHelper.DetectAsync(
                        this.ImageUrl,
                        returnFaceId: true,
                        returnFaceLandmarks: detectFaceLandmarks,
                        returnFaceAttributes: detectFaceAttributes ? DefaultFaceAttributeTypes : null);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.DetectedFaces = await FaceServiceHelper.DetectAsync(
                        this.GetImageStreamCallback,
                        returnFaceId: true,
                        returnFaceLandmarks: detectFaceLandmarks,
                        returnFaceAttributes: detectFaceAttributes ? DefaultFaceAttributeTypes : null);
                }

                if (this.FilterOutSmallFaces)
                {
                    this.DetectedFaces = this.DetectedFaces.Where(f => CoreUtil.IsFaceBigEnoughForDetection(f.FaceRectangle.Height, this.DecodedImageHeight));
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Face API DetectAsync error");

                this.DetectedFaces = Enumerable.Empty<Face>();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Face detection failed.");
                }
            }
            finally
            {
                this.OnFaceDetectionCompleted();
            }
        }

        public async Task DetectEmotionAsync()
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.DetectedEmotion = await EmotionServiceHelper.RecognizeAsync(this.ImageUrl);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.DetectedEmotion = await EmotionServiceHelper.RecognizeAsync(this.GetImageStreamCallback);
                }

                if (this.FilterOutSmallFaces)
                {
                    this.DetectedEmotion = this.DetectedEmotion.Where(f => CoreUtil.IsFaceBigEnoughForDetection(f.FaceRectangle.Height, this.DecodedImageHeight));
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Emotion API RecognizeAsync error");

                this.DetectedEmotion = Enumerable.Empty<Emotion>();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Emotion detection failed.");
                }
            }
            finally
            {
                this.OnEmotionRecognitionCompleted();
            }
        }

        public async Task DescribeAsync()
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.AnalysisResult = await VisionServiceHelper.DescribeAsync(this.ImageUrl);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.AnalysisResult = await VisionServiceHelper.DescribeAsync(this.GetImageStreamCallback);
                }
            }
            catch (Exception e)
            {
                this.AnalysisResult = new Microsoft.ProjectOxford.Vision.Contract.AnalysisResult();

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
                    this.AnalysisResult = await VisionServiceHelper.AnalyzeImageAsync(this.GetImageStreamCallback, new VisualFeature[] { VisualFeature.Categories }, new string[] { "Celebrities" });
                }
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Vision API AnalyzeImageAsync error");

                this.AnalysisResult = new Microsoft.ProjectOxford.Vision.Contract.AnalysisResult();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Vision API failed.");
                }
            }
        }

        public async Task IdentifyFacesAsync()
        {
            this.IdentifiedPersons = Enumerable.Empty<IdentifiedPerson>();

            Guid[] detectedFaceIds = this.DetectedFaces?.Select(f => f.FaceId).ToArray();
            if (detectedFaceIds != null && detectedFaceIds.Any())
            {
                List<IdentifiedPerson> result = new List<IdentifiedPerson>();

                IEnumerable<PersonGroup> personGroups = Enumerable.Empty<PersonGroup>();
                try
                {
                    personGroups = await FaceServiceHelper.GetPersonGroupsAsync(PeopleGroupsUserDataFilter);
                }
                catch (Exception e)
                {
                    ErrorTrackingHelper.TrackException(e, "Face API GetPersonGroupsAsync error");

                    if (this.ShowDialogOnFaceApiErrors)
                    {
                        await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Failure getting PersonGroups");
                    }
                }

                foreach (var group in personGroups)
                {
                    try
                    {
                        IdentifyResult[] groupResults = await FaceServiceHelper.IdentifyAsync(group.PersonGroupId, detectedFaceIds);
                        foreach (var match in groupResults)
                        {
                            if (!match.Candidates.Any())
                            {
                                continue;
                            }

                            Person person = await FaceServiceHelper.GetPersonAsync(group.PersonGroupId, match.Candidates[0].PersonId);

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
                }

                this.IdentifiedPersons = result;
            }

            this.OnFaceRecognitionCompleted();
        }

        public async Task FindSimilarPersistedFacesAsync()
        {
            this.SimilarFaceMatches = Enumerable.Empty<SimilarFaceMatch>();

            if (this.DetectedFaces == null || !this.DetectedFaces.Any())
            {
                return;
            }

            List<SimilarFaceMatch> result = new List<SimilarFaceMatch>();

            foreach (Face detectedFace in this.DetectedFaces)
            {
                try
                {
                    SimilarPersistedFace similarPersistedFace = await FaceListManager.FindSimilarPersistedFaceAsync(this.GetImageStreamCallback, detectedFace.FaceId, detectedFace);
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

        private void OnFaceDetectionCompleted()
        {
            if (this.FaceDetectionCompleted != null)
            {
                this.FaceDetectionCompleted(this, EventArgs.Empty);
            }
        }

        private void OnFaceRecognitionCompleted()
        {
            if (this.FaceRecognitionCompleted != null)
            {
                this.FaceRecognitionCompleted(this, EventArgs.Empty);
            }
        }

        private void OnEmotionRecognitionCompleted()
        {
            if (this.EmotionRecognitionCompleted != null)
            {
                this.EmotionRecognitionCompleted(this, EventArgs.Empty);
            }
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
        public Face Face
        {
            get; set;
        }

        public SimilarPersistedFace SimilarPersistedFace
        {
            get; set;
        }
    }
}
