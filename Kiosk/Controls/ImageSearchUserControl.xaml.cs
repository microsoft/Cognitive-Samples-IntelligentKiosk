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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace IntelligentKioskSample.Controls
{
    public sealed partial class ImageSearchUserControl : UserControl
    {
        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;

        public static readonly DependencyProperty ClearStateWhenClosedProperty =
            DependencyProperty.Register(
            "ClearStateWhenClosed",
            typeof(bool),
            typeof(ImageSearchUserControl),
            new PropertyMetadata(true)
            );

        public static readonly DependencyProperty ImageContentTypeProperty =
            DependencyProperty.Register(
            "ImageContentType",
            typeof(string),
            typeof(ImageSearchUserControl),
            new PropertyMetadata("Face")
            );

        public static readonly DependencyProperty EnableCameraCaptureProperty =
            DependencyProperty.Register(
            "EnableCameraCapture",
            typeof(bool),
            typeof(ImageSearchUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty RequireFaceInCameraCaptureProperty =
            DependencyProperty.Register(
            "RequireFaceInCameraCapture",
            typeof(bool),
            typeof(ImageSearchUserControl),
            new PropertyMetadata(true)
            );

        public static readonly DependencyProperty ImageSelectionModeProperty =
            DependencyProperty.Register(
            "ImageSelectionMode",
            typeof(ListViewSelectionMode),
            typeof(ImageSearchUserControl),
            new PropertyMetadata(ListViewSelectionMode.Single)
            );

        public event EventHandler<IEnumerable<ImageAnalyzer>> OnImageSearchCompleted;
        public event EventHandler<IEnumerable<ImageAnalyzer>> OnCameraFrameCaptured;
        public event EventHandler OnImageSearchCanceled;

        public bool ClearStateWhenClosed
        {
            get { return (bool)GetValue(ClearStateWhenClosedProperty); }
            set { SetValue(ClearStateWhenClosedProperty, value); }
        }

        public string ImageContentType
        {
            get { return (string)GetValue(ImageContentTypeProperty); }
            set { SetValue(ImageContentTypeProperty, value); }
        }

        public bool EnableCameraCapture
        {
            get { return (bool)GetValue(EnableCameraCaptureProperty); }
            set { SetValue(EnableCameraCaptureProperty, value); }
        }

        public bool RequireFaceInCameraCapture
        {
            get { return (bool)GetValue(RequireFaceInCameraCaptureProperty); }
            set { SetValue(RequireFaceInCameraCaptureProperty, value); }
        }

        public ListViewSelectionMode ImageSelectionMode
        {
            get { return (ListViewSelectionMode)GetValue(ImageSelectionModeProperty); }
            set { SetValue(ImageSelectionModeProperty, value); }
        }

        public ImageSearchUserControl()
        {
            this.InitializeComponent();
        }

        public void TriggerSearch(string query)
        {
            this.imageResultsGrid.ItemsSource = Enumerable.Empty<string>();
            this.autoSuggestBox.Text = query;
        }

        private async void onQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await QueryBingImages(args.QueryText);
        }

        private async Task QueryBingImages(string query)
        {
            this.progressRing.IsActive = true;

            try
            {
                IEnumerable<string> imageUrls = await BingSearchHelper.GetImageSearchResults(query, imageContent: this.ImageContentType, count: 30);
                this.imageResultsGrid.ItemsSource = imageUrls.Select(url => new ImageAnalyzer(url));
                this.autoSuggestBox.IsSuggestionListOpen = false;
            }
            catch (Exception ex)
            {
                this.imageResultsGrid.ItemsSource = null;
                await Util.GenericApiCallExceptionHandler(ex, "Failure querying Bing Images");
            }

            this.progressRing.IsActive = false;
        }

        private void ClearFlyoutState()
        {
            if (this.ClearStateWhenClosed)
            {
                this.autoSuggestBox.Text = "";
                this.imageResultsGrid.ItemsSource = Enumerable.Empty<string>();
            }
            else
            {
                this.imageResultsGrid.DeselectRange(new ItemIndexRange(0, (uint)this.imageResultsGrid.Items.Count));
            }
        }

        private async void onTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try
                {
                    this.autoSuggestBox.ItemsSource = await BingSearchHelper.GetAutoSuggestResults(this.autoSuggestBox.Text);
                }
                catch (Exception)
                {
                    // Default to no suggestions
                    this.autoSuggestBox.ItemsSource = null;
                }
            }
            else if (args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange && !string.IsNullOrEmpty(this.autoSuggestBox.Text))
            {
                await QueryBingImages(this.autoSuggestBox.Text);
            }

        }

        private void OnAcceptButtonClicked(object sender, RoutedEventArgs e)
        {
            this.OnImageSearchCompleted?.Invoke(this, this.imageResultsGrid.SelectedItems.Cast<ImageAnalyzer>().ToArray());
            this.ClearFlyoutState();
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.OnImageSearchCanceled?.Invoke(this, EventArgs.Empty);
            this.ClearFlyoutState();
        }

        private void OnImageResultSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.addSelectedPhotosButton.IsEnabled = this.imageResultsGrid.SelectedItems.Any();
        }

        private async void LoadImagesFromFileClicked(object sender, RoutedEventArgs e)
        {
            this.progressRing.IsActive = true;

            try
            {
                FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary, ViewMode = PickerViewMode.Thumbnail };
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".jpeg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.FileTypeFilter.Add(".bmp");
                IReadOnlyList<StorageFile> selectedFiles = await fileOpenPicker.PickMultipleFilesAsync();

                if (selectedFiles != null && selectedFiles.Any())
                {
                    this.OnImageSearchCompleted?.Invoke(this, selectedFiles.Select(file => new ImageAnalyzer(file.OpenStreamForReadAsync, file.Path)));
                }
            }
            catch (Exception ex)
            {
                this.imageResultsGrid.ItemsSource = null;
                await Util.GenericApiCallExceptionHandler(ex, "Failure processing local images");
            }

            this.progressRing.IsActive = false;
        }

        private async void OnCameraImageCaptured(object sender, ImageAnalyzer e)
        {
            this.cameraCaptureFlyout.Hide();
            await this.HandleTrainingImageCapture(e);
        }

        private async Task HandleTrainingImageCapture(ImageAnalyzer img, bool dismissImageSearchFlyout = true)
        {
            var croppedImage = img;

            if (this.RequireFaceInCameraCapture)
            {
                croppedImage = await GetPrimaryFaceFromCameraCaptureAsync(img);
            }

            if (croppedImage != null)
            {
                if (dismissImageSearchFlyout)
                {
                    this.OnImageSearchCompleted?.Invoke(this, new ImageAnalyzer[] { croppedImage });
                }
                else
                {
                    this.OnCameraFrameCaptured?.Invoke(this, new ImageAnalyzer[] { croppedImage });
                }
            }
        }

        private async Task<ImageAnalyzer> GetPrimaryFaceFromCameraCaptureAsync(ImageAnalyzer img)
        {
            if (img == null)
            {
                return null;
            }

            await img.DetectFacesAsync();

            if (img.DetectedFaces == null || !img.DetectedFaces.Any())
            {
                return null;
            }

            // Crop the primary face and return it as the result
            FaceRectangle rect = img.DetectedFaces.First().FaceRectangle;
            double heightScaleFactor = 1.8;
            double widthScaleFactor = 1.8;
            FaceRectangle biggerRectangle = new FaceRectangle
            {
                Height = Math.Min((int)(rect.Height * heightScaleFactor), img.DecodedImageHeight),
                Width = Math.Min((int)(rect.Width * widthScaleFactor), img.DecodedImageWidth)
            };
            biggerRectangle.Left = Math.Max(0, rect.Left - (int)(rect.Width * ((widthScaleFactor - 1) / 2)));
            biggerRectangle.Top = Math.Max(0, rect.Top - (int)(rect.Height * ((heightScaleFactor - 1) / 1.4)));

            StorageFile tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(
                                                    "FaceRecoCameraCapture.jpg",
                                                    CreationCollisionOption.GenerateUniqueName);

            await Util.CropBitmapAsync(img.GetImageStreamCallback, biggerRectangle.ToRect(), tempFile);

            return new ImageAnalyzer(tempFile.OpenStreamForReadAsync, tempFile.Path);
        }

        private async void OnCameraFlyoutOpened(object sender, object e)
        {
            await this.cameraControl.StartStreamAsync();
        }

        private async void OnCameraFlyoutClosed(object sender, object e)
        {
            await this.cameraControl.StopStreamAsync();
            this.isProcessingLoopInProgress = false;
            this.autoCaptureToggle.IsOn = false;
        }

        private void StartAutoCaptureProcessingLoop()
        {
            this.isProcessingLoopInProgress = true;

            if (this.processingLoopTask == null || this.processingLoopTask.Status != TaskStatus.Running)
            {
                this.processingLoopTask = Task.Run(() => this.AutoCaptureProcessingLoop());
            }
        }

        private async void AutoCaptureProcessingLoop()
        {
            while (this.isProcessingLoopInProgress)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (!this.isProcessingPhoto)
                    {
                        this.isProcessingPhoto = true;
                        await this.HandleTrainingImageCapture(await this.cameraControl.TakeAutoCapturePhoto(), dismissImageSearchFlyout: false);
                        this.isProcessingPhoto = false;
                    }
                });

                await Task.Delay(1000);
            }
        }

        private void OnCameraAutoCaptureToggleChanged(object sender, RoutedEventArgs e)
        {
            if (this.autoCaptureToggle.IsOn)
            {
                this.StartAutoCaptureProcessingLoop();
            }
            else
            {
                this.isProcessingLoopInProgress = false;
            }
        }
    }
}
