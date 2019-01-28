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
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public class EmotionFeedback
    {
        public string Text { get; set; }
        public SolidColorBrush AccentColor { get; set; }
        public string ImageFileName { get; set; }
    }

    public sealed partial class ImageWithFaceBorderUserControl : UserControl
    {
        private ImageAnalyzer currentImage;

        public ImageWithFaceBorderUserControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty DetectFacesOnLoadProperty =
            DependencyProperty.Register(
            "DetectFacesOnLoad",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowMultipleFacesProperty =
            DependencyProperty.Register(
            "ShowMultipleFaces",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty PerformRecognitionProperty =
            DependencyProperty.Register(
            "PerformRecognition",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty DetectFaceAttributesProperty =
            DependencyProperty.Register(
            "DetectFaceAttributes",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowRecognitionResultsProperty =
            DependencyProperty.Register(
            "ShowRecognitionResults",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowDialogOnApiErrorsProperty =
            DependencyProperty.Register(
            "ShowDialogOnApiErrors",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowEmotionRecognitionProperty =
            DependencyProperty.Register(
            "ShowEmotionRecognition",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty BalloonBackgroundProperty =
            DependencyProperty.Register(
            "BalloonBackground",
            typeof(SolidColorBrush),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty BalloonForegroundProperty =
            DependencyProperty.Register(
            "BalloonForeground",
            typeof(SolidColorBrush),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty DetectFaceLandmarksProperty =
            DependencyProperty.Register(
            "DetectFaceLandmarks",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty PerformComputerVisionAnalysisProperty =
            DependencyProperty.Register(
            "PerformComputerVisionAnalysis",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty PerformOCRAnalysisProperty =
            DependencyProperty.Register(
            "PerformOCRAnalysis",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty PerformObjectDetectionProperty =
            DependencyProperty.Register(
            "PerformObjectDetection",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public SolidColorBrush BalloonBackground
        {
            get { return (SolidColorBrush)GetValue(BalloonBackgroundProperty); }
            set { SetValue(BalloonBackgroundProperty, (SolidColorBrush)value); }
        }

        public SolidColorBrush BalloonForeground
        {
            get { return (SolidColorBrush)GetValue(BalloonForegroundProperty); }
            set { SetValue(BalloonForegroundProperty, (SolidColorBrush)value); }
        }

        public bool ShowEmotionRecognition
        {
            get { return (bool)GetValue(ShowEmotionRecognitionProperty); }
            set { SetValue(ShowEmotionRecognitionProperty, (bool)value); }
        }

        public bool ShowMultipleFaces
        {
            get { return (bool)GetValue(ShowMultipleFacesProperty); }
            set { SetValue(ShowMultipleFacesProperty, (bool)value); }
        }

        public bool DetectFacesOnLoad
        {
            get { return (bool)GetValue(DetectFacesOnLoadProperty); }
            set { SetValue(DetectFacesOnLoadProperty, (bool)value); }
        }

        public bool DetectFaceAttributes
        {
            get { return (bool)GetValue(DetectFaceAttributesProperty); }
            set { SetValue(DetectFaceAttributesProperty, (bool)value); }
        }

        public bool PerformRecognition
        {
            get { return (bool)GetValue(PerformRecognitionProperty); }
            set { SetValue(PerformRecognitionProperty, (bool)value); }
        }

        public bool ShowRecognitionResults
        {
            get { return (bool)GetValue(ShowRecognitionResultsProperty); }
            set { SetValue(ShowRecognitionResultsProperty, (bool)value); }
        }

        public bool ShowDialogOnApiErrors
        {
            get { return (bool)GetValue(ShowDialogOnApiErrorsProperty); }
            set { SetValue(ShowDialogOnApiErrorsProperty, (bool)value); }
        }

        public bool DetectFaceLandmarks
        {
            get { return (bool)GetValue(DetectFaceLandmarksProperty); }
            set { SetValue(DetectFaceLandmarksProperty, (bool)value); }
        }

        public bool PerformComputerVisionAnalysis
        {
            get { return (bool)GetValue(PerformComputerVisionAnalysisProperty); }
            set { SetValue(PerformComputerVisionAnalysisProperty, (bool)value); }
        }

        public bool PerformOCRAnalysis
        {
            get { return (bool)GetValue(PerformOCRAnalysisProperty); }
            set { SetValue(PerformOCRAnalysisProperty, (bool)value); }
        }

        public bool PerformObjectDetection
        {
            get { return (bool)GetValue(PerformObjectDetectionProperty); }
            set { SetValue(PerformObjectDetectionProperty, (bool)value); }
        }

        public TextRecognitionMode TextRecognitionMode { get; set; }

        private async void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ImageAnalyzer dataContext = this.DataContext as ImageAnalyzer;

            if (this.currentImage != dataContext)
            {
                this.currentImage = dataContext;
            }
            else
            {
                // Windows sometimes fires multiple DataContextChanged events. 
                // If we are here that is one of those cases, and since we already set 
                // the data context to this value we can ignore it
                return;
            }

            foreach (var child in this.hostGrid.Children.Where(c => !(c is Image)).ToArray())
            {
                this.hostGrid.Children.Remove(child);
            }

            // remove the current source
            this.bitmapImage.UriSource = null;

            if (dataContext != null)
            {
                try
                {
                    if (dataContext.ImageUrl != null)
                    {
                        this.bitmapImage.UriSource = new Uri(dataContext.ImageUrl);
                    }
                    else if (dataContext.GetImageStreamCallback != null)
                    {
                        await this.bitmapImage.SetSourceAsync((await dataContext.GetImageStreamCallback()).AsRandomAccessStream());
                    }
                }
                catch (Exception ex)
                {
                    // If we fail to load the image we will just not display it
                    this.bitmapImage.UriSource = null;
                    if (this.ShowDialogOnApiErrors)
                    {
                        await Util.GenericApiCallExceptionHandler(ex, "Error loading captured image.");
                    }
                }
            }
        }

        private async Task DetectAndShowFaceBorders()
        {
            this.progressIndicator.IsActive = true;

            foreach (var child in this.hostGrid.Children.Where(c => !(c is Image)).ToArray())
            {
                this.hostGrid.Children.Remove(child);
            }

            if (this.DataContext is ImageAnalyzer imageWithFace)
            {
                if (imageWithFace.DetectedFaces == null)
                {
                    await imageWithFace.DetectFacesAsync(detectFaceAttributes: this.DetectFaceAttributes, detectFaceLandmarks: this.DetectFaceLandmarks);
                }

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                foreach (DetectedFace face in imageWithFace.DetectedFaces)
                {
                    FaceIdentificationBorder faceUI = new FaceIdentificationBorder()
                    {
                        Tag = face.FaceId,
                    };

                    faceUI.Margin = new Thickness((face.FaceRectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                  (face.FaceRectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0);

                    faceUI.BalloonBackground = this.BalloonBackground;
                    faceUI.BalloonForeground = this.BalloonForeground;
                    faceUI.ShowFaceRectangle(face.FaceRectangle.Width * renderedImageXTransform, face.FaceRectangle.Height * renderedImageYTransform);

                    if (this.DetectFaceLandmarks)
                    {
                        faceUI.ShowFaceLandmarks(renderedImageXTransform, renderedImageYTransform, face);
                    }

                    this.hostGrid.Children.Add(faceUI);

                    if (!this.ShowMultipleFaces)
                    {
                        break;
                    }
                }

                if (this.PerformRecognition)
                {
                    if (imageWithFace.IdentifiedPersons == null)
                    {
                        await imageWithFace.IdentifyFacesAsync();
                    }

                    if (this.ShowRecognitionResults)
                    {
                        foreach (DetectedFace face in imageWithFace.DetectedFaces)
                        {
                            // Get the border for the associated face id
                            FaceIdentificationBorder faceUI = (FaceIdentificationBorder)this.hostGrid.Children.FirstOrDefault(e => e is FaceIdentificationBorder && (Guid)(e as FaceIdentificationBorder).Tag == face.FaceId);

                            if (faceUI != null)
                            {
                                IdentifiedPerson faceIdIdentification = imageWithFace.IdentifiedPersons.FirstOrDefault(p => p.FaceId == face.FaceId);

                                string name = this.DetectFaceAttributes && faceIdIdentification != null ? faceIdIdentification.Person.Name : null;
                                Microsoft.Azure.CognitiveServices.Vision.Face.Models.Gender? gender = this.DetectFaceAttributes ? face.FaceAttributes.Gender : null;
                                double age = this.DetectFaceAttributes ? face.FaceAttributes.Age.GetValueOrDefault() : 0;
                                double confidence = this.DetectFaceAttributes && faceIdIdentification != null ? faceIdIdentification.Confidence : 0;

                                faceUI.ShowIdentificationData(age, gender, (uint)Math.Round(confidence * 100), name);
                            }
                        }
                    }
                }
            }

            this.progressIndicator.IsActive = false;
        }

        private async Task DetectAndShowComputerVisionAnalysis()
        {
            this.progressIndicator.IsActive = true;

            this.imageControl.RenderTransform = null;
            foreach (var child in this.hostGrid.Children.Where(c => !(c is Image)).ToArray())
            {
                this.hostGrid.Children.Remove(child);
            }

            if (this.DataContext is ImageAnalyzer img)
            {
                List<Task> tasks = new List<Task>();
                if (img.AnalysisResult == null)
                {
                    tasks.Add(img.AnalyzeImageAsync(detectCelebrities: true));
                }

                if (this.PerformOCRAnalysis && (img.TextOperationResult == null || img.TextRecognitionMode != this.TextRecognitionMode))
                {
                    tasks.Add(img.RecognizeTextAsync(this.TextRecognitionMode));
                }

                if (this.PerformObjectDetection && img.DetectedObjects == null)
                {
                    tasks.Add(img.DetectObjectsAsync());
                }

                await Task.WhenAll(tasks);

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                if (img.AnalysisResult.Faces != null)
                {
                    foreach (FaceDescription face in img.AnalysisResult.Faces)
                    {
                        FaceIdentificationBorder faceUI = new FaceIdentificationBorder
                        {
                            Margin = new Thickness((face.FaceRectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                      (face.FaceRectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0),
                            BalloonBackground = this.BalloonBackground,
                            BalloonForeground = this.BalloonForeground
                        };
                        faceUI.ShowFaceRectangle(face.FaceRectangle.Width * renderedImageXTransform, face.FaceRectangle.Height * renderedImageYTransform);

                        var faceGender = Util.GetFaceGender(face.Gender);
                        faceUI.ShowIdentificationData(face.Age, faceGender, 0, null);
                        this.hostGrid.Children.Add(faceUI);

                        this.GetCelebrityInfoIfAvailable(img, face.FaceRectangle, out string celebRecoName, out double celebRecoConfidence);
                        if (!string.IsNullOrEmpty(celebRecoName))
                        {
                            Border celebUI = new Border
                            {
                                Child = new TextBlock
                                {
                                    Text = string.Format("{0} ({1}%)", celebRecoName, (uint)Math.Round(celebRecoConfidence * 100)),
                                    Foreground = this.BalloonForeground,
                                    FontSize = 14
                                },
                                Background = this.BalloonBackground,
                                VerticalAlignment = VerticalAlignment.Top,
                                HorizontalAlignment = HorizontalAlignment.Left
                            };

                            celebUI.SizeChanged += (ev, ar) =>
                            {
                                celebUI.Margin = new Thickness(faceUI.Margin.Left - (celebUI.ActualWidth - face.FaceRectangle.Width * renderedImageXTransform) / 2,
                                                               faceUI.Margin.Top + 2 + face.FaceRectangle.Height * renderedImageYTransform, 0, 0);
                            };
                            this.hostGrid.Children.Add(celebUI);
                        }
                    }
                }

                // Clean up any old results
                foreach (var child in this.hostGrid.Children.Where(c => (c is OCRBorder)).ToArray())
                {
                    this.hostGrid.Children.Remove(child);
                }

                // OCR request (Printed / Handwritten)
                if (this.PerformOCRAnalysis && img.TextOperationResult?.RecognitionResult?.Lines != null)
                {
                    this.imageControl.RenderTransform = new RotateTransform { Angle = 0, CenterX = this.imageControl.RenderSize.Width / 2, CenterY = this.imageControl.RenderSize.Height / 2 };

                    foreach (Line line in img.TextOperationResult.RecognitionResult.Lines)
                    {
                        foreach (var word in line.Words)
                        {
                            int[] boundingBox = word?.BoundingBox?.ToArray() ?? new int[] { };
                            if (boundingBox.Length == 8)
                            {
                                double minLeft = renderedImageXTransform * (new List<int>() { boundingBox[0], boundingBox[2], boundingBox[4], boundingBox[6] }).Min();
                                double minTop = renderedImageYTransform * (new List<int>() { boundingBox[1], boundingBox[3], boundingBox[5], boundingBox[7] }).Min();
                                var points = new PointCollection()
                                {
                                    new Windows.Foundation.Point(boundingBox[0] * renderedImageXTransform - minLeft, boundingBox[1] * renderedImageYTransform - minTop),
                                    new Windows.Foundation.Point(boundingBox[2] * renderedImageXTransform - minLeft, boundingBox[3] * renderedImageYTransform - minTop),
                                    new Windows.Foundation.Point(boundingBox[4] * renderedImageXTransform - minLeft, boundingBox[5] * renderedImageYTransform - minTop),
                                    new Windows.Foundation.Point(boundingBox[6] * renderedImageXTransform - minLeft, boundingBox[7] * renderedImageYTransform - minTop)
                                };

                                // The four points (x-coordinate, y-coordinate) of the detected rectangle from the left-top corner and clockwise
                                IEnumerable<Windows.Foundation.Point> leftPoints = points.OrderBy(p => p.X).Take(2);
                                IEnumerable<Windows.Foundation.Point> rightPoints = points.OrderByDescending(p => p.X).Take(2);
                                Windows.Foundation.Point leftTop = leftPoints.OrderBy(p => p.Y).FirstOrDefault();
                                Windows.Foundation.Point leftBottom = leftPoints.OrderByDescending(p => p.Y).FirstOrDefault();
                                Windows.Foundation.Point rightTop = rightPoints.OrderBy(p => p.Y).FirstOrDefault();
                                Windows.Foundation.Point rightBottom = rightPoints.OrderByDescending(p => p.Y).FirstOrDefault();
                                var orderedPoints = new PointCollection()
                                {
                                    leftTop, rightTop, rightBottom, leftBottom
                                };

                                // simple math to get angle of the text
                                double diffWidth = Math.Abs(leftTop.X - rightTop.X);
                                double diffHeight = Math.Abs(leftTop.Y - rightTop.Y);
                                double sign = leftTop.Y > rightTop.Y ? -1 : 1;
                                double angle = sign * Math.Atan2(diffHeight, diffWidth) * (180 / Math.PI); // angle in degrees

                                OCRBorder ocrUI = new OCRBorder
                                {
                                    Margin = new Thickness(minLeft + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                      minTop + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0)
                                };
                                ocrUI.SetPoints(orderedPoints, word.Text, angle);
                                this.hostGrid.Children.Add(ocrUI);
                            }
                        }
                    }
                }

                if (this.PerformObjectDetection && img.DetectedObjects != null)
                {
                    this.ShowObjectDetectionRegions(img.DetectedObjects);
                }
            }

            this.progressIndicator.IsActive = false;
        }

        private void ShowObjectDetectionRegions(IEnumerable<DetectedObject> predictions)
        {
            double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
            double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

            foreach (DetectedObject prediction in predictions)
            {
                this.hostGrid.Children.Add(
                    new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Lime),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(prediction.Rectangle.X * renderedImageXTransform + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                prediction.Rectangle.Y * renderedImageYTransform + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0),
                        Width = prediction.Rectangle.W * renderedImageXTransform,
                        Height = prediction.Rectangle.H * renderedImageYTransform,
                    });

                this.hostGrid.Children.Add(
                    new Border
                    {
                        Height = 40,
                        FlowDirection = FlowDirection.LeftToRight,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(prediction.Rectangle.X * renderedImageXTransform + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                prediction.Rectangle.Y * renderedImageYTransform + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2) - 40, 0, 0),

                        Child = new Border
                        {
                            Background = new SolidColorBrush(Colors.Lime),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Child =
                                new TextBlock
                                {
                                    Foreground = new SolidColorBrush(Colors.Black),
                                    Text = $"{prediction.ObjectProperty} ({Math.Round(prediction.Confidence * 100)}%)",
                                    FontSize = 16,
                                    Margin = new Thickness(6, 0, 6, 0)
                                }
                        }
                    });
            }
        }

        private void GetCelebrityInfoIfAvailable(ImageAnalyzer analyzer, Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.FaceRectangle rectangle, out string name, out double confidence)
        {
            if (analyzer.AnalysisResult?.Categories != null)
            {
                foreach (var category in analyzer.AnalysisResult.Categories.Where(c => c.Detail != null))
                {
                    if (category.Detail.Celebrities != null)
                    {
                        foreach (var celebrity in category.Detail.Celebrities)
                        {
                            int left = celebrity.FaceRectangle.Left;
                            int top = celebrity.FaceRectangle.Top;

                            if (Math.Abs(left - rectangle.Left) <= 20 && Math.Abs(top - rectangle.Top) <= 20)
                            {
                                name = celebrity.Name;
                                confidence = celebrity.Confidence;
                                return;
                            }
                        }
                    }
                }
            }

            name = null;
            confidence = 0;
        }

        private async Task DetectAndShowEmotion()
        {
            this.progressIndicator.IsActive = true;

            foreach (var child in this.hostGrid.Children.Where(c => !(c is Image)).ToArray())
            {
                this.hostGrid.Children.Remove(child);
            }

            if (this.DataContext is ImageAnalyzer imageWithFace)
            {
                if (imageWithFace.DetectedFaces == null)
                {
                    await imageWithFace.DetectFacesAsync(detectFaceAttributes: true);
                }

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                foreach (DetectedFace face in imageWithFace.DetectedFaces)
                {
                    FaceIdentificationBorder faceUI = new FaceIdentificationBorder
                    {
                        Margin = new Thickness((face.FaceRectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                    (face.FaceRectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0),
                        BalloonBackground = this.BalloonBackground,
                        BalloonForeground = this.BalloonForeground
                    };

                    faceUI.ShowFaceRectangle(face.FaceRectangle.Width * renderedImageXTransform, face.FaceRectangle.Height * renderedImageYTransform);

                    faceUI.ShowEmotionData(face.FaceAttributes.Emotion);

                    this.hostGrid.Children.Add(faceUI);

                    if (!this.ShowMultipleFaces)
                    {
                        break;
                    }
                }
            }

            this.progressIndicator.IsActive = false;
        }

        private async Task PreviewImageFaces()
        {
            if (!this.DetectFacesOnLoad || this.progressIndicator.IsActive)
            {
                return;
            }

            if (this.DataContext is ImageAnalyzer img)
            {
                img.UpdateDecodedImageSize(this.bitmapImage.PixelHeight, this.bitmapImage.PixelWidth);
            }

            if (this.ShowEmotionRecognition)
            {
                await this.DetectAndShowEmotion();
            }
            else if (this.DetectFaceAttributes)
            {
                await this.DetectAndShowFaceBorders();
            }
            else if (this.PerformComputerVisionAnalysis)
            {
                await this.DetectAndShowComputerVisionAnalysis();
            }
        }

        private async void OnBitmapImageOpened(object sender, RoutedEventArgs e)
        {
            await this.PreviewImageFaces();
        }

        private async void OnImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            await this.PreviewImageFaces();
        }
    }
}
