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
    [KioskExperience(Title = "Bing Visual Search", ImagePath = "ms-appx:/Assets/BingVisualSearch.jpg")]
    public sealed partial class BingVisualSearch : Page
    {
        private ImageAnalyzer currentPhoto;

        public BingVisualSearch()
        {
            this.InitializeComponent();

            this.cameraControl.ImageCaptured += CameraControl_ImageCaptured;
            this.cameraControl.CameraRestarted += CameraControl_CameraRestarted;

            this.favoritePhotosGridView.ItemsSource = new string[] 
                {
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/1.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/3.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/4.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/11.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/12.jpg",
                    "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/13.jpg",
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
            this.resultsGridView.ItemsSource = null;
            this.progressRing.IsActive = true;
        }

        private async void UpdateResults(ImageAnalyzer img)
        {
            this.searchErrorTextBlock.Visibility = Visibility.Collapsed;

            IEnumerable<VisualSearchResult> result = null;

            try
            {
                if (this.similarImagesResultType.IsSelected)
                {
                    if (img.ImageUrl != null)
                    {
                        result = await BingSearchHelper.GetVisuallySimilarImages(img.ImageUrl);
                    }
                    else
                    {
                        result = await BingSearchHelper.GetVisuallySimilarImages(await Util.ResizePhoto(await img.GetImageStreamCallback(), 360));
                    }
                }
                else if (this.celebrityResultType.IsSelected)
                {
                    if (img.ImageUrl != null)
                    {
                        result = (await BingSearchHelper.GetVisuallySimilarCelebrities(img.ImageUrl)).OrderByDescending(r => r.SimilarityScore);
                    }
                    else
                    {
                        result = (await BingSearchHelper.GetVisuallySimilarCelebrities(await Util.ResizePhoto(await img.GetImageStreamCallback(), 360))).OrderByDescending(r => r.SimilarityScore);
                    }
                }
                else if (this.similarProductsResultType.IsSelected)
                {
                    if (img.ImageUrl != null)
                    {
                        result = await BingSearchHelper.GetVisuallySimilarProducts(img.ImageUrl);
                    }
                    else
                    {
                        result = await BingSearchHelper.GetVisuallySimilarProducts(await Util.ResizePhoto(await img.GetImageStreamCallback(), 360));
                    }
                }
            }
            catch (Exception)
            {
                // We just ignore errors for now and default to a generic error message
            }

            this.resultsGridView.ItemsSource = result;
            this.progressRing.IsActive = false;

            if (result == null || !result.Any())
            {
                this.searchErrorTextBlock.Visibility = Visibility.Visible;
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
            this.currentPhoto = img;

            this.landingMessage.Visibility = Visibility.Collapsed;
            this.resultsDetails.Visibility = Visibility.Visible;

            this.DisplayProcessingUI();
            this.UpdateResults(img);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.BingSearchApiKey))
            {
                await new MessageDialog("Missing Bing Search API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
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

        private void OnResultTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.currentPhoto != null)
            {
                this.UpdateActivePhoto(this.currentPhoto);
            }
        }
    }

    public class ResultItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PhotoTemplate { get; set; }
        public DataTemplate ProductTemplate { get; set; }
        public DataTemplate CelebrityTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is VisualSearchProductResult)
            { 
                return ProductTemplate;
            }
            else if (item is VisualSearchCelebrityResult)
            {
                return CelebrityTemplate;
            }
            else
            {
                return PhotoTemplate;
            }
        }
    }
}
