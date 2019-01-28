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

using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ServiceHelpers;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public sealed partial class VideoTrack : UserControl
    {
        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(
            "DisplayText",
            typeof(string),
            typeof(VideoTrack),
            new PropertyMetadata("")
            );

        public static readonly DependencyProperty CroppedFaceProperty =
            DependencyProperty.Register(
            "CroppedFace",
            typeof(ImageSource),
            typeof(VideoTrack),
            new PropertyMetadata(null)
            );

        public string DisplayText
        {
            get { return (string)GetValue(DisplayTextProperty); }
            set { SetValue(DisplayTextProperty, (string)value); }
        }

        public ImageSource CroppedFace
        {
            get { return (ImageSource)GetValue(CroppedFaceProperty); }
            set { SetValue(CroppedFaceProperty, (ImageSource)value); }
        }

        public VideoTrack()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private int duration;
        public int Duration
        {
            set
            {
                this.duration = value;

                this.chart.Children.Clear();
            }
        }

        public void SetVideoFrameState(int videoFrameTimestampInSeconds, Emotion emotion, ImageAnalyzer analysisResult = null)
        {
            EmotionToColoredBar emotionResponse = new EmotionToColoredBar();
            emotionResponse.UpdateEmotion(emotion);
            emotionResponse.Tag = videoFrameTimestampInSeconds;
            emotionResponse.Width = Math.Max(this.chart.ActualWidth / this.duration, 0.5);
            emotionResponse.HorizontalAlignment = HorizontalAlignment.Left;

            emotionResponse.Margin = new Thickness
            {
                Left = ((double)videoFrameTimestampInSeconds / this.duration) * this.chart.ActualWidth
            };

            this.chart.Children.Add(emotionResponse);

            if (analysisResult != null)
            {
                this.AddFlyoutToElement(emotionResponse, analysisResult);
            }
        }

        private void ChartSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = Math.Max(this.chart.ActualWidth / this.duration, 0.5);

            foreach (var item in this.chart.Children)
            {
                var element = (FrameworkElement)item;
                element.Width = width;
                element.Margin = new Thickness
                {
                    Left = ((double) ((int)element.Tag) / this.duration) * this.chart.ActualWidth
                };
            }
        }

        private void AddFlyoutToElement(FrameworkElement element, ImageAnalyzer analysisResult)
        {
            var content = new ImageWithFaceBorderUserControl
            {
                PerformComputerVisionAnalysis = true,
                PerformObjectDetection = true,
                DetectFacesOnLoad = true,
                DataContext = analysisResult,
                Width = 400
            };

            FlyoutBase.SetAttachedFlyout(element, new Flyout { Content = content });

            element.PointerReleased += (s, e) =>
            {
                FlyoutBase.ShowAttachedFlyout(element);
            };
        }
    }
}
