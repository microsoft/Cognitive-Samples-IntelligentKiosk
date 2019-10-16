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

using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace IntelligentKioskSample.Models.InsuranceClaimAutomation
{
    public class TokenOverlayInfo
    {
        public int PageWidth { get; set; }
        public int PageHeight { get; set; }
        public IList<Rect> RectList { get; set; }
        public string Text { get; set; }

        public TokenOverlayInfo() { }

        public TokenOverlayInfo(IList<ExtractedToken> entityList, int width, int height)
        {
            PageWidth = width;
            PageHeight = height;
            RectList = new List<Rect>();
            foreach (var entity in entityList)
            {
                // ExtractedToken.BoundingBox : The co-ordinate pairs are arranged by top-left, top-right, 
                // bottom -right and bottom-left endpoints box with origin reference from the bottom-left of the page.
                double x = entity.BoundingBox?.Where((i, index) => index % 2 == 0).Min() ?? 0; //evens are X's
                double y = entity.BoundingBox?.Where((i, index) => index % 2 != 0).Select(i => height - i.Value).Min() ?? 0; //odds are Y's
                double x2 = entity.BoundingBox?.Where((i, index) => index % 2 == 0).Max() ?? 0;
                double y2 = entity.BoundingBox?.Where((i, index) => index % 2 != 0).Select(i => height - i.Value).Max() ?? 0;

                RectList.Add(new Rect(x, y, x2 - x, y2 - y));
            }

            Text = string.Join(" ", entityList.Select(e => e.Text));
        }
    }
}
