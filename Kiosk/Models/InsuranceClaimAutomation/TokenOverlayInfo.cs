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

using ServiceHelpers.Models.FormRecognizer;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace IntelligentKioskSample.Models.InsuranceClaimAutomation
{
    public class TokenOverlayInfo
    {
        public double PageWidth { get; set; }
        public double PageHeight { get; set; }
        public IList<Rect> RectList { get; set; }
        public string Text { get; set; }

        public TokenOverlayInfo() { }

        public TokenOverlayInfo(WordResult entity, double width, double height)
        {
            Initialize(entity.Text, entity.BoundingBox, width, height);
        }

        void Initialize(string name, IList<double?> boundingBox, double width, double height)
        {
            PageWidth = width;
            PageHeight = height;
            Text = name;

            // ExtractedToken.BoundingBox : The co-ordinate pairs are arranged by top-left, top-right, 
            // bottom-right and bottom-left endpoints box.
            double x = boundingBox?.Where((i, index) => index % 2 == 0).Min() ?? 0; //evens are X's
            double y = boundingBox?.Where((i, index) => index % 2 != 0).Min() ?? 0; //odds are Y's
            double x2 = boundingBox?.Where((i, index) => index % 2 == 0).Max() ?? 0;
            double y2 = boundingBox?.Where((i, index) => index % 2 != 0).Max() ?? 0;

            RectList = new List<Rect>() { new Rect(x, y, x2 - x, y2 - y) };
        }
    }
}
