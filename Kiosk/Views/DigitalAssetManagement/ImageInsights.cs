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
using System;
using Face = Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace IntelligentKioskSample.Views.DigitalAssetManagement
{
    public class ImageInsights
    {
        public Uri ImageUri { get; set; }
        public FaceInsights[] FaceInsights { get; set; }
        public VisionInsights VisionInsights { get; set; }
        public CustomVisionInsights[] CustomVisionInsights { get; set; }
    }

    public class VisionInsights
    {
        public string Caption { get; set; }
        public string[] Tags { get; set; }
        public string[] Objects { get; set; }
        public string[] Landmarks { get; set; }
        public string[] Celebrities { get; set; }
        public string[] Brands { get; set; }
        public string[] Words { get; set; }
        public AdultInfo Adult { get; set; }
        public ColorInfo Color { get; set; }
        public ImageType ImageType { get; set; }
        public ImageMetadata Metadata { get; set; }
    }

    public class FaceInsights
    {
        public Guid UniqueFaceId { get; set; }
        public Face.FaceRectangle FaceRectangle { get; set; }
        public Face.FaceAttributes FaceAttributes { get; set; }
    }

    public class CustomVisionInsights
    {
        public string Name { get; set; }
        public CustomVisionPrediction[] Predictions { get; set; }
        public bool IsObjectDetection { get; set; }
    }

    public class CustomVisionPrediction
    {
        public string Name { get; set; }
        public double Probability { get; set; }
    }
}
