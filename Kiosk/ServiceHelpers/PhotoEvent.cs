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

using Microsoft.ProjectOxford.Emotion.Contract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class AgeGenderInfo
    {
        public double Age { get; set; }
        public string Gender { get; set; }
    }

    public class FaceInfo
    {
        public AgeGenderInfo AgeGenderInfo { get; set; }
        public Scores Emotion { get; set; }
        public string Name { get; set; }
        public string UniqueId { get; set; }
    }

    public class PhotoEvent
    {
        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        {
            // Stream Analytics will readjust all dates to UTC time so dont specify the timezone to preserve the localtime
            DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
        };

        public FaceInfo[] FaceInfo { get; set; }
        public DateTime LocalTime { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, jsonSettings);
        }

        public PhotoEvent(ImageAnalyzer capture)
        {
            LocalTime = DateTime.Now;

            List<FaceInfo> faceInfoList = new List<FaceInfo>();

            if (capture.DetectedFaces != null)
            {
                foreach (var detectedFace in capture.DetectedFaces)
                {
                    FaceInfo faceInfo = new FaceInfo();

                    // Check if we have age/gender for this face.
                    if (detectedFace.FaceAttributes != null)
                    {
                        faceInfo.AgeGenderInfo = new AgeGenderInfo { Age = detectedFace.FaceAttributes.Age, Gender = detectedFace.FaceAttributes.Gender };
                    }

                    // Check if we identified this face. If so send the name along.
                    if (capture.IdentifiedPersons != null)
                    {
                        var matchingPerson = capture.IdentifiedPersons.FirstOrDefault(p => p.FaceId == detectedFace.FaceId);
                        if (matchingPerson != null)
                        {
                            faceInfo.Name = matchingPerson.Person.Name;
                        }
                    }

                    // Check if we have emotion for this face. If so send it along.
                    if (capture.DetectedEmotion != null)
                    {
                        Emotion matchingEmotion = CoreUtil.FindFaceClosestToRegion(capture.DetectedEmotion, detectedFace.FaceRectangle);
                        if (matchingEmotion != null)
                        {
                            faceInfo.Emotion = matchingEmotion.Scores;
                        }
                    }

                    // Check if we have an unique Id for this face. If so send it along.
                    if (capture.SimilarFaceMatches != null)
                    {
                        var matchingPerson = capture.SimilarFaceMatches.FirstOrDefault(p => p.Face.FaceId == detectedFace.FaceId);
                        if (matchingPerson != null)
                        {
                            faceInfo.UniqueId = matchingPerson.SimilarPersistedFace.PersistedFaceId.ToString("N").Substring(0, 4);
                        }
                    }

                    faceInfoList.Add(faceInfo);
                }
            }
            else if (capture.DetectedEmotion != null)
            {
                // If we are here we only have emotion. No age/gender or id.
                faceInfoList.AddRange(capture.DetectedEmotion.Select(emotion => new FaceInfo { Emotion = emotion.Scores }));
            }

            this.FaceInfo = faceInfoList.ToArray();
        }
    }
}
