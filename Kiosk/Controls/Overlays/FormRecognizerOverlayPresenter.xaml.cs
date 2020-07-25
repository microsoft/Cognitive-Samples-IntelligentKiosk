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

using IntelligentKioskSample.Controls.Overlays.Primitives;
using ServiceHelpers.Models.FormRecognizer;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls.Overlays
{
    public sealed partial class FormRecognizerOverlayPresenter : UserControl
    {
        public static readonly DependencyProperty SourceProperty = 
            DependencyProperty.Register(
                "Source", 
                typeof(ImageSource), 
                typeof(FormRecognizerOverlayPresenter), 
                new PropertyMetadata(default(ImageSource)));

        public static readonly DependencyProperty TokenInfoProperty = 
            DependencyProperty.Register(
                "TokenInfo", 
                typeof(IList<TokenOverlayInfo>), 
                typeof(FormRecognizerOverlayPresenter), 
                new PropertyMetadata(default(IList<TokenOverlayInfo>)));

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public IList<TokenOverlayInfo> TokenInfo
        {
            get { return (IList<TokenOverlayInfo>)GetValue(TokenInfoProperty); }
            set { SetValue(TokenInfoProperty, value); }
        }

        public FormRecognizerOverlayPresenter()
        {
            this.InitializeComponent();
        }
    }

    public class TokenOverlayInfo : OverlayInfo<PolygonInfo, object, string>
    {
        public TokenOverlayInfo(WordResult entity)
        {
            Initialize(entity.Text, entity.BoundingBox);
        }

        void Initialize(string name, IList<double?> boundingBox, double? height = null)
        {
            // ExtractedToken.BoundingBox : The co-ordinate pairs are arranged by top-left, top-right, 
            // bottom-right and bottom-left endpoints box.
            double x =  boundingBox?.Where((i, index) => index % 2 == 0).Min() ?? 0; //evens are X's
            double y =  boundingBox?.Where((i, index) => index % 2 != 0).Select(i => height.HasValue ? height - i.Value : i.Value).Min() ?? 0; //odds are Y's
            double x2 = boundingBox?.Where((i, index) => index % 2 == 0).Max() ?? 0;
            double y2 = boundingBox?.Where((i, index) => index % 2 != 0).Select(i => height.HasValue ? height - i.Value : i.Value).Max() ?? 0;
            Rect = new Rect(x, y, x2 - x, y2 - y);

            EntityExt = new PolygonInfo() { Name = name, Points = GetPoints(boundingBox, height).ToArray() };
            ToolTipExt = name;
        }

        IEnumerable<Point> GetPoints(IList<double?> boundingBox, double? height)
        {
            //validate
            if (boundingBox == null)
            {
                yield break;
            }

            for (int i = 0; i < boundingBox.Count; i += 2)
            {
                double x = boundingBox[i] ?? 0;
                double y = (height.HasValue ? height - boundingBox[i + 1] : boundingBox[i + 1]) ?? 0;
                yield return new Point(x, y);
            }
        }
    }
}
