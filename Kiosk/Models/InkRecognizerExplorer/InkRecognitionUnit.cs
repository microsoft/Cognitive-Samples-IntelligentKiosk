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

using System.Collections.Generic;

namespace IntelligentKioskSample.Models.InkRecognizerExplorer
{
    public class InkRecognitionUnit
    {
        public IList<Alternate> alternates { get; set; }
        public BoundingRectangle boundingRectangle { get; set; }
        public string category { get; set; }
        public int id { get; set; }
        public int parentId { get; set; }
        public string recognizedText { get; set; }
        public IList<RotatedBoundingRectangle> rotatedBoundingRectangle { get; set; }
        public IList<int> strokeIds { get; set; }
        public IList<int?> childIds { get; set; }
        public Center center { get; set; }
        public int? confidence { get; set; }
        public IList<Point> points { get; set; }
        public string recognizedObject { get; set; }
        public int? rotationAngle { get; set; }
    }

    public class Alternate
    {
        public string category { get; set; }
        public string recognizedString { get; set; }
    }

    public class BoundingRectangle
    {
        public double height { get; set; }
        public double topX { get; set; }
        public double topY { get; set; }
        public double width { get; set; }
    }

    public class RotatedBoundingRectangle
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class Center
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class Point
    {
        public double x { get; set; }
        public double y { get; set; }
    }
}
