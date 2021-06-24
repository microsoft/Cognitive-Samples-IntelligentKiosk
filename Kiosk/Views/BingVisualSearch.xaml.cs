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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "BingVisualSearch",
        DisplayName = "Bing Visual Search",
        Description = "See photos and products similar to your image",
        ImagePath = "ms-appx:/Assets/DemoGallery/Bing Visual Search.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologyArea = TechnologyAreaType.Search | TechnologyAreaType.Vision,
        TechnologiesUsed = TechnologyType.BingAutoSuggest | TechnologyType.BingImages,
        DateAdded = "2017/03/01")]
    public sealed partial class BingVisualSearch : Page
    {
        private ImageAnalyzer currentPhoto;

        public BingVisualSearch()
        {
            this.InitializeComponent();
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
                ShowErrorResult();
            }
        }

        private void UpdateActivePhoto(ImageAnalyzer img)
        {
            this.currentPhoto = img;

            this.resultsDetails.Visibility = Visibility.Visible;

            this.DisplayProcessingUI();
            this.UpdateResults(img);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await this.imagePicker.SetSuggestedImageList(
                new Uri("ms-appx:///Assets/DemoSamples/BingVisualSearch/1.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/BingVisualSearch/3.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/BingVisualSearch/4.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/BingVisualSearch/11.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/BingVisualSearch/12.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/BingVisualSearch/13.jpg"));

            if (string.IsNullOrEmpty(SettingsHelper.Instance.BingSearchApiKey))
            {
                await new MessageDialog("Missing Bing Search API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            ImageAnalyzer image = args.First();
            image.ShowDialogOnFaceApiErrors = true;

            this.UpdateActivePhoto(image);

            DisplayImage.Source = await image.GetImageSource();
        }

        private void OnResultTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.currentPhoto != null)
            {
                this.UpdateActivePhoto(this.currentPhoto);
            }
        }

        private void ShowErrorResult()
        {
            string message = string.Empty;
            if (this.similarImagesResultType.IsSelected)
            {
                message = "No similar images found.\r\nPlease try another image.";
            }
            else if (this.similarProductsResultType.IsSelected)
            {
                message = "No similar products found.\r\nPlease try a close-up image of some product.";
            }
            this.searchErrorTextBlock.Text = message;
            this.searchErrorTextBlock.Visibility = Visibility.Visible;
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
