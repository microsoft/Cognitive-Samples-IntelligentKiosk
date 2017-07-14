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

using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using System.IO;
using ServiceHelpers;
using Newtonsoft.Json.Linq;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public class EmotionFeedback
    {
        public string Text { get; set; }
        public SolidColorBrush AccentColor { get; set; }
        public string ImageFileName { get; set; }
    }

    public interface IEmotionFeedbackDataProvider
    {
        EmotionFeedback GetEmotionFeedback(Emotion emotion);
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

            ImageAnalyzer imageWithFace = this.DataContext as ImageAnalyzer;
            if (imageWithFace != null)
            {
                if (imageWithFace.DetectedFaces == null)
                {
                    await imageWithFace.DetectFacesAsync(detectFaceAttributes: this.DetectFaceAttributes, detectFaceLandmarks: this.DetectFaceLandmarks);
                }

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                foreach (Face face in imageWithFace.DetectedFaces)
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
                        foreach (Face face in imageWithFace.DetectedFaces)
                        {
                            // Get the border for the associated face id
                            FaceIdentificationBorder faceUI = (FaceIdentificationBorder)this.hostGrid.Children.FirstOrDefault(e => e is FaceIdentificationBorder && (Guid)(e as FaceIdentificationBorder).Tag == face.FaceId);

                            if (faceUI != null)
                            {
                                IdentifiedPerson faceIdIdentification = imageWithFace.IdentifiedPersons.FirstOrDefault(p => p.FaceId == face.FaceId);

                                string name = this.DetectFaceAttributes && faceIdIdentification != null ? faceIdIdentification.Person.Name : null;
                                string gender = this.DetectFaceAttributes ? face.FaceAttributes.Gender : null;
                                double age = this.DetectFaceAttributes ? face.FaceAttributes.Age : 0;
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

            ImageAnalyzer img = this.DataContext as ImageAnalyzer;
            if (img != null)
            {
                if (this.PerformOCRAnalysis && img.OcrResults == null)
                {
                    await Task.WhenAll(img.AnalyzeImageAsync(detectCelebrities: true), img.RecognizeTextAsync());
                }
                else if (img.AnalysisResult == null)
                {
                    await img.AnalyzeImageAsync(detectCelebrities: true);
                }

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                if (img.AnalysisResult.Faces != null)
                {
                    foreach (Microsoft.ProjectOxford.Vision.Contract.Face face in img.AnalysisResult.Faces)
                    {
                        FaceIdentificationBorder faceUI = new FaceIdentificationBorder();

                        faceUI.Margin = new Thickness((face.FaceRectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                      (face.FaceRectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0);

                        faceUI.BalloonBackground = this.BalloonBackground;
                        faceUI.BalloonForeground = this.BalloonForeground;
                        faceUI.ShowFaceRectangle(face.FaceRectangle.Width * renderedImageXTransform, face.FaceRectangle.Height * renderedImageYTransform);

                        faceUI.ShowIdentificationData(face.Age, face.Gender, 0, null);
                        this.hostGrid.Children.Add(faceUI);

                        double celebRecoConfidence = 0;
                        string celebRecoName;
                        this.GetCelebrityInfoIfAvailable(img, face.FaceRectangle, out celebRecoName, out celebRecoConfidence);
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

                if (this.PerformOCRAnalysis && img.OcrResults.Regions != null)
                {
                    if (img.OcrResults.TextAngle.HasValue)
                    {
                        this.imageControl.RenderTransform = new RotateTransform { Angle = -img.OcrResults.TextAngle.Value, CenterX = this.imageControl.RenderSize.Width / 2, CenterY = this.imageControl.RenderSize.Height / 2 };
                    }

                    foreach (Microsoft.ProjectOxford.Vision.Contract.Region ocrRegion in img.OcrResults.Regions)
                    {
                        foreach (var line in ocrRegion.Lines)
                        {
                            foreach (var word in line.Words)
                            {
                                OCRBorder ocrUI = new OCRBorder();

                                ocrUI.Margin = new Thickness((word.Rectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                      (word.Rectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0);

                                ocrUI.SetData(word.Rectangle.Width * renderedImageXTransform, word.Rectangle.Height * renderedImageYTransform, word.Text);

                                this.hostGrid.Children.Add(ocrUI);
                            }
                        }
                    }
                }
            }

            this.progressIndicator.IsActive = false;
        }

        private void GetCelebrityInfoIfAvailable(ImageAnalyzer analyzer, Microsoft.ProjectOxford.Vision.Contract.FaceRectangle rectangle, out string name, out double confidence)
        {
            if (analyzer.AnalysisResult?.Categories != null)
            {
                foreach (var category in analyzer.AnalysisResult.Categories.Where(c => c.Detail != null))
                {
                    dynamic detail = JObject.Parse(category.Detail.ToString());
                    if (detail.celebrities != null)
                    {
                        foreach (var celebrity in detail.celebrities)
                        {
                            int left = Int32.Parse(celebrity.faceRectangle.left.ToString());
                            int top = Int32.Parse(celebrity.faceRectangle.top.ToString());

                            if (Math.Abs(left - rectangle.Left) <= 20 && Math.Abs(top - rectangle.Top) <= 20)
                            {
                                name = celebrity.name.ToString();
                                confidence = celebrity.confidence;
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

            ImageAnalyzer imageWithFace = this.DataContext as ImageAnalyzer;
            if (imageWithFace != null)
            {
                if (imageWithFace.DetectedEmotion == null)
                {
                    await imageWithFace.DetectEmotionAsync();
                }

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                foreach (Emotion emotion in imageWithFace.DetectedEmotion)
                {
                    FaceIdentificationBorder faceUI = new FaceIdentificationBorder();

                    faceUI.Margin = new Thickness((emotion.FaceRectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                    (emotion.FaceRectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0);

                    faceUI.BalloonBackground = this.BalloonBackground;
                    faceUI.BalloonForeground = this.BalloonForeground;

                    faceUI.ShowFaceRectangle(emotion.FaceRectangle.Width * renderedImageXTransform, emotion.FaceRectangle.Height * renderedImageYTransform);

                    faceUI.ShowEmotionData(emotion);

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

            ImageAnalyzer img = this.DataContext as ImageAnalyzer;
            if (img != null)
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
