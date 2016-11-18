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

using ServiceHelpers;
using Microsoft.ProjectOxford.Emotion.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Microsoft.ProjectOxford.Face.Contract;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public sealed partial class FaceIdentificationBorder : UserControl
    {
        public static readonly DependencyProperty BalloonBackgroundProperty =
            DependencyProperty.Register(
            "BalloonBackground",
            typeof(SolidColorBrush),
            typeof(FaceIdentificationBorder),
            new PropertyMetadata(null)
            );

        public SolidColorBrush BalloonBackground
        {
            get { return (SolidColorBrush)GetValue(BalloonBackgroundProperty); }
            set { SetValue(BalloonBackgroundProperty, (SolidColorBrush)value); }
        }

        public static readonly DependencyProperty BalloonForegroundProperty =
            DependencyProperty.Register(
            "BalloonForeground",
            typeof(SolidColorBrush),
            typeof(FaceIdentificationBorder),
            new PropertyMetadata(null)
            );

        public SolidColorBrush BalloonForeground
        {
            get { return (SolidColorBrush)GetValue(BalloonForegroundProperty); }
            set { SetValue(BalloonForegroundProperty, (SolidColorBrush)value); }
        }

        public string CaptionText { get; set; }

        public EmotionData[] EmotionData { get; set; }

        public FaceIdentificationBorder()
        {
            this.InitializeComponent();
        }

        public void ShowFaceRectangle(double width, double height)
        {
            this.faceRectangle.Width = width;
            this.faceRectangle.Height = height;

            this.faceRectangle.Visibility = Visibility.Visible;
        }

        public void ShowFaceLandmarks(double renderedImageXTransform, double renderedImageYTransform, Face face)
        {
            // Mouth (6)
            AddFacialLandmark(face, face.FaceLandmarks.MouthLeft, renderedImageXTransform, renderedImageYTransform, Colors.White);
            AddFacialLandmark(face, face.FaceLandmarks.MouthRight, renderedImageXTransform, renderedImageYTransform, Colors.White);
            AddFacialLandmark(face, face.FaceLandmarks.UpperLipBottom, renderedImageXTransform, renderedImageYTransform, Colors.White);
            AddFacialLandmark(face, face.FaceLandmarks.UpperLipTop, renderedImageXTransform, renderedImageYTransform, Colors.White);
            AddFacialLandmark(face, face.FaceLandmarks.UnderLipBottom, renderedImageXTransform, renderedImageYTransform, Colors.White);
            AddFacialLandmark(face, face.FaceLandmarks.UnderLipTop, renderedImageXTransform, renderedImageYTransform, Colors.White);

            // Eyes (10)
            AddFacialLandmark(face, face.FaceLandmarks.EyeLeftBottom, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.EyeLeftTop, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.EyeLeftInner, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.EyeLeftOuter, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.EyeRightBottom, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.EyeRightTop, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.EyeRightInner, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.EyeRightOuter, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.PupilLeft, renderedImageXTransform, renderedImageYTransform, Colors.Red);
            AddFacialLandmark(face, face.FaceLandmarks.PupilRight, renderedImageXTransform, renderedImageYTransform, Colors.Red);

            // nose (7)
            AddFacialLandmark(face, face.FaceLandmarks.NoseLeftAlarOutTip, renderedImageXTransform, renderedImageYTransform, Colors.LimeGreen);
            AddFacialLandmark(face, face.FaceLandmarks.NoseLeftAlarTop, renderedImageXTransform, renderedImageYTransform, Colors.LimeGreen);
            AddFacialLandmark(face, face.FaceLandmarks.NoseRightAlarOutTip, renderedImageXTransform, renderedImageYTransform, Colors.LimeGreen);
            AddFacialLandmark(face, face.FaceLandmarks.NoseRightAlarTop, renderedImageXTransform, renderedImageYTransform, Colors.LimeGreen);
            AddFacialLandmark(face, face.FaceLandmarks.NoseRootLeft, renderedImageXTransform, renderedImageYTransform, Colors.LimeGreen);
            AddFacialLandmark(face, face.FaceLandmarks.NoseRootRight, renderedImageXTransform, renderedImageYTransform, Colors.LimeGreen);
            AddFacialLandmark(face, face.FaceLandmarks.NoseTip, renderedImageXTransform, renderedImageYTransform, Colors.LimeGreen);

            // eyebrows (4)
            AddFacialLandmark(face, face.FaceLandmarks.EyebrowLeftInner, renderedImageXTransform, renderedImageYTransform, Colors.Yellow);
            AddFacialLandmark(face, face.FaceLandmarks.EyebrowLeftOuter, renderedImageXTransform, renderedImageYTransform, Colors.Yellow);
            AddFacialLandmark(face, face.FaceLandmarks.EyebrowRightInner, renderedImageXTransform, renderedImageYTransform, Colors.Yellow);
            AddFacialLandmark(face, face.FaceLandmarks.EyebrowRightOuter, renderedImageXTransform, renderedImageYTransform, Colors.Yellow);
        }

        private void AddFacialLandmark(Face face, FeatureCoordinate feature, double renderedImageXTransform, double renderedImageYTransform, Color color)
        {
            double dotSize = 3;
            Rectangle b = new Rectangle { Fill = new SolidColorBrush(color), Width = dotSize, Height = dotSize, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            b.Margin = new Thickness(((feature.X - face.FaceRectangle.Left) * renderedImageXTransform) - dotSize / 2, ((feature.Y - face.FaceRectangle.Top) * renderedImageYTransform) - dotSize / 2, 0, 0);
            this.hostGrid.Children.Add(b);
        }

        public void ShowIdentificationData(double age, string gender, uint confidence, string name = null)
        {
            int roundedAge = (int)Math.Round(age);

            if (!string.IsNullOrEmpty(name))
            {
                this.CaptionText = string.Format("{0}, {1} ({2}%)", name, roundedAge, confidence);
                this.genderIcon.Visibility = Visibility.Collapsed;
            }
            else if (!string.IsNullOrEmpty(gender))
            {
                this.CaptionText = roundedAge.ToString();
                if (string.Compare(gender, "male", true) == 0)
                {
                    this.genderIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/male.png"));
                }
                else if (string.Compare(gender, "female", true) == 0)
                {
                    this.genderIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/female.png"));
                }
            }

            this.DataContext = this;
            this.captionCanvas.Visibility = Visibility.Visible;
        }

        public void ShowEmotionData(Emotion emotion)
        {
            this.EmotionData = EmotionServiceHelper.ScoresToEmotionData(emotion.Scores).OrderByDescending(e => e.EmotionScore).ToArray();

            this.DataContext = this;

            this.genderAgeGrid.Visibility = Visibility.Collapsed;
            this.emotionGrid.Visibility = Visibility.Visible;
            this.captionCanvas.Visibility = Visibility.Visible;
        }

        private void OnCaptionSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.captionCanvas.Margin = new Thickness(this.faceRectangle.Margin.Left - (this.captionCanvas.ActualWidth - this.faceRectangle.ActualWidth) / 2,
                                                      -this.captionCanvas.ActualHeight, 0, 0);
        }
    }
}