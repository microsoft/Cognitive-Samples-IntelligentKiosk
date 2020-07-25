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
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls.Overlays
{
    public sealed partial class VisionApiOverlayPresenter : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ImageSource), typeof(VisionApiOverlayPresenter), new PropertyMetadata(default(ImageSource)));
        public static readonly DependencyProperty ObjectInfoProperty = DependencyProperty.Register("ObjectInfo", typeof(IList<ObjectOverlayInfo>), typeof(VisionApiOverlayPresenter), new PropertyMetadata(default(IList<ObjectOverlayInfo>)));
        public static readonly DependencyProperty FaceInfoProperty = DependencyProperty.Register("FaceInfo", typeof(IList<FaceOverlayInfo>), typeof(VisionApiOverlayPresenter), new PropertyMetadata(default(IList<FaceOverlayInfo>)));
        public static readonly DependencyProperty TextInfoProperty = DependencyProperty.Register("TextInfo", typeof(IList<TextOverlayInfo>), typeof(VisionApiOverlayPresenter), new PropertyMetadata(default(IList<TextOverlayInfo>)));
        public static readonly DependencyProperty EnableHoverSelectionProperty = DependencyProperty.Register("EnableHoverSelection", typeof(bool), typeof(OverlayPresenter), new PropertyMetadata(true));

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public IList<ObjectOverlayInfo> ObjectInfo
        {
            get { return (IList<ObjectOverlayInfo>)GetValue(ObjectInfoProperty); }
            set { SetValue(ObjectInfoProperty, value); }
        }

        public IList<FaceOverlayInfo> FaceInfo
        {
            get { return (IList<FaceOverlayInfo>)GetValue(FaceInfoProperty); }
            set { SetValue(FaceInfoProperty, value); }
        }

        public IList<TextOverlayInfo> TextInfo
        {
            get { return (IList<TextOverlayInfo>)GetValue(TextInfoProperty); }
            set { SetValue(TextInfoProperty, value); }
        }

        public bool EnableHoverSelection
        {
            get { return (bool)GetValue(EnableHoverSelectionProperty); }
            set { SetValue(EnableHoverSelectionProperty, value); }
        }

        public VisionApiOverlayPresenter()
        {
            this.InitializeComponent();
        }
    }

    public class FaceOverlayInfo : OverlayInfo<ImageCrop<AgeInfo>, AgeInfo>
    {
        public bool IsCelebrity { get; set; }

        public FaceOverlayInfo(FaceDescription entity, CelebritiesModel celebrity)
        {
            //set fields
            Rect = new Rect(entity.FaceRectangle.Left, entity.FaceRectangle.Top, entity.FaceRectangle.Width, entity.FaceRectangle.Height);
            var ageInfo = new AgeInfo { Age = entity.Age, Gender = entity.Gender != Gender.Female ? AgeInfoGender.Male : AgeInfoGender.Female };
            AgeInfo celebrityInfo = null;
            if (celebrity != null)
            {
                ageInfo.Name = celebrity.Name;
                ageInfo.Confidence = celebrity.Confidence;
                celebrityInfo = ageInfo;
            }
            LabelsExt = new AgeInfo[] { ageInfo, celebrityInfo };
            EntityExt = new ImageCrop<AgeInfo> { Entity = ageInfo };
            IsCelebrity = celebrity != null;
        }
    }

    public class ObjectOverlayInfo : OverlayInfo<ImageCrop<ConfidenceInfo>, ConfidenceInfo>
    {
        public ObjectOverlayInfo(DetectedObject entity)
        {
            //set fields
            Rect = new Rect(entity.Rectangle.X, entity.Rectangle.Y, entity.Rectangle.W, entity.Rectangle.H);
            var nameInfo = new ConfidenceInfo { Name = entity.ObjectProperty, Confidence = entity.Confidence };
            LabelsExt = new ConfidenceInfo[] { nameInfo };
            EntityExt = new ImageCrop<ConfidenceInfo> { Entity = nameInfo };
        }

        public ObjectOverlayInfo(DetectedBrand entity)
        {
            //set fields
            Rect = new Rect(entity.Rectangle.X, entity.Rectangle.Y, entity.Rectangle.W, entity.Rectangle.H);
            var nameInfo = new ConfidenceInfo { Name = entity.Name, Confidence = entity.Confidence };
            LabelsExt = new ConfidenceInfo[] { nameInfo };
            EntityExt = new ImageCrop<ConfidenceInfo> { Entity = nameInfo };
        }
        public ObjectOverlayInfo() { }
    }

    public class TextOverlayInfo : OverlayInfo<PolygonInfo, object, string>
    {
        public TextOverlayInfo(string name, IList<double?> boundingBox)
        {
            Initialize(name, boundingBox.Select(n => (double)n).ToArray());
        }

        void Initialize(string name, IList<double> boundingBox)
        {
            //set fields
            var x =  boundingBox?.Where((i, index) => index % 2 == 0).Min() ?? 0; //evens are X's
            var y =  boundingBox?.Where((i, index) => index % 2 != 0).Min() ?? 0; //odds are Y's
            var x2 = boundingBox?.Where((i, index) => index % 2 == 0).Max() ?? 0;
            var y2 = boundingBox?.Where((i, index) => index % 2 != 0).Max() ?? 0;
            Rect = new Rect(x, y, x2 - x, y2 - y);
            EntityExt = new PolygonInfo() { Name = name, Points = GetPoints(boundingBox).ToArray() };
            ToolTipExt = name;
        }

        IEnumerable<Point> GetPoints(IList<double> boundingBox)
        {
            //validate
            if (boundingBox == null)
            {
                yield break;
            }

            for (int i = 0; i < boundingBox.Count; i = i + 2)
            {
                yield return new Point(boundingBox[i], boundingBox[i + 1]);
            }
        }
    }

    public class PolygonInfo
    {
        public string Name { get; set; }
        public IList<Point> Points { get; set; }
    }

    public class AgeInfo : ConfidenceInfo
    {
        public int Age { get; set; }
        public AgeInfoGender Gender { get; set; }
    }

    public enum AgeInfoGender
    {
        Male,
        Female
    }

    public class ConfidenceInfo
    {
        public double Confidence { get; set; }
        public string Name { get; set; }
    }

    public class ImageCrop<T> : ImageCrop
    {
        public T Entity { get; set; }
    }
    public class ImageCrop
    {
        public ImageSource Image { get; set; }
    }
}
