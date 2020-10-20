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
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;


namespace IntelligentKioskSample.Controls.Overlays
{
    public sealed partial class CustomVisionOverlayPresenter : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ImageSource), typeof(CustomVisionOverlayPresenter), new PropertyMetadata(default(ImageSource)));
        public static readonly DependencyProperty ObjectInfoProperty = DependencyProperty.Register("ObjectInfo", typeof(IList<PredictedObjectOverlayInfo>), typeof(CustomVisionOverlayPresenter), new PropertyMetadata(default(IList<PredictedObjectOverlayInfo>)));
        public static readonly DependencyProperty MatchInfoProperty = DependencyProperty.Register("MatchInfo", typeof(MatchOverlayInfo), typeof(CustomVisionOverlayPresenter), new PropertyMetadata(default(MatchOverlayInfo)));

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public IList<PredictedObjectOverlayInfo> ObjectInfo
        {
            get { return (IList<PredictedObjectOverlayInfo>)GetValue(ObjectInfoProperty); }
            set { SetValue(ObjectInfoProperty, value); }
        }

        public MatchOverlayInfo MatchInfo
        {
            get { return (MatchOverlayInfo)GetValue(MatchInfoProperty); }
            set { SetValue(MatchInfoProperty, value); }
        }

        public CustomVisionOverlayPresenter()
        {
            this.InitializeComponent();
        }
    }


    public class PredictedObjectOverlayInfo : OverlayInfo<PredictionModel, ConfidenceInfo>
    {
        public PredictedObjectOverlayInfo(PredictionModel entity)
        {
            //set fields
            OverlaySize = new Size(1, 1);
            Rect = new Rect(entity.BoundingBox.Left, entity.BoundingBox.Top, entity.BoundingBox.Width, entity.BoundingBox.Height);
            var nameInfo = new ConfidenceInfo { Name = entity.TagName, Confidence = entity.Probability };
            LabelsExt = new ConfidenceInfo[] { nameInfo };
            EntityExt = entity;
        }
    }

    public class MatchOverlayInfo : OverlayInfo<IList<ConfidenceInfo>>
    {
        public MatchOverlayInfo(IEnumerable<PredictionModel> entity)
        {
            //set fields
            OverlaySize = new Size(1, 1);
            Rect = new Rect(0, 0, 1, 1);
            EntityExt = entity.Select(i => new ConfidenceInfo { Name = i.TagName, Confidence = i.Probability }).ToList();
        }
    }
}
