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
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Title = "Vision API Explorer", ImagePath = "ms-appx:/Assets/VisionAPI.jpg")]
    public sealed partial class VisionApiExplorer : Page
    {
        public VisionApiExplorer()
        {
            this.InitializeComponent();

            this.cameraControl.ImageCaptured += CameraControl_ImageCaptured;
            this.cameraControl.CameraRestarted += CameraControl_CameraRestarted;

            this.favoritePhotosGridView.ItemsSource = new string[] 
                {
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/1.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/2.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/3.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/4.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/5.jpg",


                    "https://intelligentkioskstore.blob.core.windows.net/visionapi/suggestedphotos/3.png",
                    "https://intelligentkioskstore.blob.core.windows.net/visionapi/suggestedphotos/1.png",
                    "https://intelligentkioskstore.blob.core.windows.net/visionapi/suggestedphotos/2.png",
                };
        }

        private async void CameraControl_CameraRestarted(object sender, EventArgs e)
        {
            // We induce a delay here to give the camera some time to start rendering before we hide the last captured photo.
            // This avoids a black flash.
            await Task.Delay(500);

            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;
            this.resultsDetails.Visibility = Visibility.Collapsed;
        }

        private void DisplayProcessingUI()
        {
            this.tagsGridView.ItemsSource = new[] { new { Name = "Analyzing..." } };
            this.descriptionGridView.ItemsSource = new[] { new { Description = "Analyzing..." } };
            this.celebritiesTextBlock.Text = "Analyzing...";
            this.colorInfoListView.ItemsSource = new[] { new { Description = "Analyzing..." } };

            this.ocrToggle.IsEnabled = false;
            this.objectDetectionToggle.IsEnabled = false;
            this.ocrTextBox.Text = "";
        }

        private void UpdateResults(ImageAnalyzer img)
        {
            if (img.AnalysisResult.Tags == null || !img.AnalysisResult.Tags.Any())
            {
                this.tagsGridView.ItemsSource = new[] { new { Name = "No tags" } };
            }
            else
            {
                this.tagsGridView.ItemsSource = img.AnalysisResult.Tags.Select(t => new { Confidence = string.Format("({0}%)", Math.Round(t.Confidence * 100)), Name = t.Name });
            }

            if (img.AnalysisResult.Description == null || !img.AnalysisResult.Description.Captions.Any(d => d.Confidence >= 0.2))
            {
                this.descriptionGridView.ItemsSource = new[] { new { Description = "Not sure what that is" } };
            }
            else
            {
                this.descriptionGridView.ItemsSource = img.AnalysisResult.Description.Captions.Select(d => new { Confidence = string.Format("({0}%)", Math.Round(d.Confidence * 100)), Description = d.Text });
            }

            var celebNames = this.GetCelebrityNames(img);
            if (celebNames == null || !celebNames.Any())
            {
                this.celebritiesTextBlock.Text = "None";
            }
            else
            {
                this.celebritiesTextBlock.Text = string.Join(", ", celebNames.OrderBy(name => name));
            }

            if (img.AnalysisResult.Color == null)
            {
                this.colorInfoListView.ItemsSource = new[] { new { Description = "Not available" } };
            }
            else
            { 
                this.colorInfoListView.ItemsSource = new[]
                {
                    new { Description = "Dominant background color:", Colors = new string[] { img.AnalysisResult.Color.DominantColorBackground } },
                    new { Description = "Dominant foreground color:", Colors = new string[] { img.AnalysisResult.Color.DominantColorForeground } },
                    new { Description = "Dominant colors:", Colors = img.AnalysisResult.Color.DominantColors?.ToArray() },
                    new { Description = "Accent color:", Colors = new string[] { "#" + img.AnalysisResult.Color.AccentColor } }
                };
            }

            this.ocrToggle.IsEnabled = true;
            this.objectDetectionToggle.IsEnabled = true;
        }

        private IEnumerable<string> GetCelebrityNames(ImageAnalyzer analyzer)
        {
            if (analyzer.AnalysisResult?.Categories != null)
            {
                foreach (var category in analyzer.AnalysisResult.Categories?.Where(c => c.Detail != null))
                {
                    if (category.Detail.Celebrities != null)
                    {
                        foreach (var celebrity in category.Detail.Celebrities)
                        {
                            yield return celebrity.Name;
                        }
                    }
                }
            }
        }

        private async void CameraControl_ImageCaptured(object sender, ImageAnalyzer e)
        {
            this.UpdateActivePhoto(e);

            this.imageFromCameraWithFaces.DataContext = e;
            this.imageFromCameraWithFaces.Visibility = Visibility.Visible;

            await this.cameraControl.StopStreamAsync();
        }

        private void UpdateActivePhoto(ImageAnalyzer img)
        {
            this.landingMessage.Visibility = Visibility.Collapsed;
            this.resultsDetails.Visibility = Visibility.Visible;

            if (img.AnalysisResult != null)
            {
                this.UpdateResults(img);
            }
            else
            {
                this.DisplayProcessingUI();
                img.ComputerVisionAnalysisCompleted += (s, args) =>
                {
                    this.UpdateResults(img);
                };
            }

            if (this.ocrToggle.IsOn)
            {
                if (img.TextOperationResult?.RecognitionResult != null)
                {
                    this.UpdateOcrTextBoxContent(img);
                }
                else
                {
                    img.TextRecognitionCompleted += (s, args) =>
                    {
                        this.UpdateOcrTextBoxContent(img);
                    };
                }
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey))
            {
                await new MessageDialog("Missing Computer Vision API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            this.favoritePhotosGridView.SelectedItem = null;

            this.imageSearchFlyout.Hide();
            ImageAnalyzer image = args.First();
            image.ShowDialogOnFaceApiErrors = true;

            this.imageWithFacesControl.Visibility = Visibility.Visible;
            this.webCamHostGrid.Visibility = Visibility.Collapsed;
            await this.cameraControl.StopStreamAsync();

            this.UpdateActivePhoto(image);

            this.imageWithFacesControl.DataContext = image;
        }

        private void OnImageSearchCanceled(object sender, EventArgs e)
        {
            this.imageSearchFlyout.Hide();
        }

        private async void OnWebCamButtonClicked(object sender, RoutedEventArgs e)
        {
            await StartWebCameraAsync();
        }

        private async Task StartWebCameraAsync()
        {
            this.favoritePhotosGridView.SelectedItem = null;
            this.landingMessage.Visibility = Visibility.Collapsed;
            this.webCamHostGrid.Visibility = Visibility.Visible;
            this.imageWithFacesControl.Visibility = Visibility.Collapsed;
            this.resultsDetails.Visibility = Visibility.Collapsed;

            await this.cameraControl.StartStreamAsync();
            await Task.Delay(250);
            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;

            UpdateWebCamHostGridSize();
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWebCamHostGridSize();
        }

        private void UpdateWebCamHostGridSize()
        {
            this.webCamHostGrid.Height = this.webCamHostGrid.ActualWidth / (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }

        private async void OnFavoriteSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.favoriteImagePickerFlyout.Hide();

            if (!string.IsNullOrEmpty((string)this.favoritePhotosGridView.SelectedValue))
            {
                this.landingMessage.Visibility = Visibility.Collapsed;

                ImageAnalyzer image = new ImageAnalyzer((string)this.favoritePhotosGridView.SelectedValue);
                image.ShowDialogOnFaceApiErrors = true;

                this.imageWithFacesControl.Visibility = Visibility.Visible;
                this.webCamHostGrid.Visibility = Visibility.Collapsed;
                await this.cameraControl.StopStreamAsync();

                this.UpdateActivePhoto(image);

                this.imageWithFacesControl.DataContext = image;
            }
        }

        private void OnOCRToggled(object sender, RoutedEventArgs e)
        {
            this.printedOCRComboBoxItem.IsSelected = true;
            UpdateTextRecognition(TextRecognitionMode.Printed);
        }

        private void OcrModeSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (printedOCRComboBoxItem.IsSelected)
            {
                UpdateTextRecognition(TextRecognitionMode.Printed);
            }
            else if (handwrittigOCRComboBoxItem.IsSelected)
            {
                UpdateTextRecognition(TextRecognitionMode.Handwritten);
            }
        }

        private void UpdateTextRecognition(TextRecognitionMode textRecognitionMode)
        {
            imageFromCameraWithFaces.TextRecognitionMode = textRecognitionMode;
            imageWithFacesControl.TextRecognitionMode = textRecognitionMode;

            var currentImageDisplay = this.imageWithFacesControl.Visibility == Visibility.Visible ? this.imageWithFacesControl : this.imageFromCameraWithFaces;
            if (currentImageDisplay.DataContext != null)
            {
                var img = currentImageDisplay.DataContext;

                ImageAnalyzer analyzer = (ImageAnalyzer)img;

                if (analyzer.TextOperationResult?.RecognitionResult != null)
                {
                    UpdateOcrTextBoxContent(analyzer);
                }
                else
                {
                    analyzer.TextRecognitionCompleted += (s, args) =>
                    {
                        UpdateOcrTextBoxContent(analyzer);
                    };
                }

                currentImageDisplay.DataContext = null;
                currentImageDisplay.DataContext = img;
            }
        }

        private void UpdateOcrTextBoxContent(ImageAnalyzer imageAnalyzer)
        {
            this.ocrTextBox.Text = string.Empty;
            if (imageAnalyzer.TextOperationResult?.RecognitionResult?.Lines != null)
            {
                IEnumerable<string> lines = imageAnalyzer.TextOperationResult.RecognitionResult.Lines.Select(l => string.Join(" ", l?.Words?.Select(w => w.Text)));
                this.ocrTextBox.Text = string.Join("\n", lines);
            }
        }

        private void OnObjectDetectionToggled(object sender, RoutedEventArgs e)
        {
            var currentImageDisplay = this.imageWithFacesControl.Visibility == Visibility.Visible ? this.imageWithFacesControl : this.imageFromCameraWithFaces;
            if (currentImageDisplay.DataContext != null)
            {
                var img = currentImageDisplay.DataContext;

                ImageAnalyzer analyzer = (ImageAnalyzer)img;

                currentImageDisplay.DataContext = null;
                currentImageDisplay.DataContext = img;
            }
        }
    }
}
