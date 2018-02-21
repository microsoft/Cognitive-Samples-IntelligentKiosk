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

using Microsoft.ProjectOxford.Vision;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentKioskSample.Views.ImageCollectionInsights
{
    public class ImageProcessor
    {
        private static VisualFeature[] DefaultVisualFeatureTypes = new VisualFeature[] { VisualFeature.Tags, VisualFeature.Description };

        public static async Task<ImageInsights> ProcessImageAsync(Func<Task<Stream>> imageStream, string imageId)
        {
            ImageAnalyzer analyzer = new ImageAnalyzer(imageStream);
            analyzer.ShowDialogOnFaceApiErrors = true;

            // trigger vision, face and emotion requests
            await Task.WhenAll(analyzer.AnalyzeImageAsync(detectCelebrities: false, visualFeatures: DefaultVisualFeatureTypes), analyzer.DetectFacesAsync(detectFaceAttributes: true));

            // trigger face match against previously seen faces
            await analyzer.FindSimilarPersistedFacesAsync();

            ImageInsights result = new ImageInsights { ImageId = imageId };

            // assign computer vision results
            result.VisionInsights = new VisionInsights
            {
                Caption = analyzer.AnalysisResult.Description?.Captions[0].Text,
                Tags = analyzer.AnalysisResult.Tags != null ? analyzer.AnalysisResult.Tags.Select(t => t.Name).ToArray() : new string[0]
            };

            // assign face api and emotion api results
            List<FaceInsights> faceInsightsList = new List<FaceInsights>();
            foreach (var face in analyzer.DetectedFaces)
            {
                FaceInsights faceInsights = new FaceInsights
                {
                    FaceRectangle = face.FaceRectangle,
                    Age = face.FaceAttributes.Age,
                    Gender = face.FaceAttributes.Gender,
                    TopEmotion = face.FaceAttributes.Emotion.ToRankedList().First().Key
                };

                SimilarFaceMatch similarFaceMatch = analyzer.SimilarFaceMatches.FirstOrDefault(s => s.Face.FaceId == face.FaceId);
                if (similarFaceMatch != null)
                {
                    faceInsights.UniqueFaceId = similarFaceMatch.SimilarPersistedFace.PersistedFaceId;
                }

                faceInsightsList.Add(faceInsights);
            }

            result.FaceInsights = faceInsightsList.ToArray();

            return result;
        }
    }
}
