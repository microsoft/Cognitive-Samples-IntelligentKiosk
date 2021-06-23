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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace IntelligentKioskSample.Controls
{
    public enum ImagePickerState
    {
        InputTypes,
        CameraStream,
        BingImageSearch,
        LocalFile,
        ShowingSelectedImage
    }

    public sealed partial class ImagePickerControl : UserControl, INotifyPropertyChanged
    {
        private ImagePickerState stateAtLastPickedImage;

        public static readonly DependencyProperty ImageContentTypeProperty =
            DependencyProperty.Register(
            "ImageContentType",
            typeof(string),
            typeof(ImagePickerControl),
            new PropertyMetadata("Face")
            );

        public static readonly DependencyProperty ImageSelectionModeProperty =
            DependencyProperty.Register(
            "ImageSelectionMode",
            typeof(ListViewSelectionMode),
            typeof(ImagePickerControl),
            new PropertyMetadata(ListViewSelectionMode.Single)
            );

        public static readonly DependencyProperty SubheaderTextProperty =
            DependencyProperty.Register(
            "SubheaderText",
            typeof(string),
            typeof(ImagePickerControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty TryAnotherButtonAlignmentProperty =
            DependencyProperty.Register(
            "TryAnotherButtonAlignment",
            typeof(HorizontalAlignment),
            typeof(ImagePickerControl),
            new PropertyMetadata(HorizontalAlignment.Center)
            );

        public event EventHandler<IEnumerable<ImageAnalyzer>> OnImageSearchCompleted;
        public event PropertyChangedEventHandler PropertyChanged;

        private ImagePickerState currentState;
        public ImagePickerState CurrentState
        {
            get { return currentState; }
            set
            {
                currentState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentState"));
            }
        }

        public string ImageContentType
        {
            get { return (string)GetValue(ImageContentTypeProperty); }
            set { SetValue(ImageContentTypeProperty, value); }
        }

        public ListViewSelectionMode ImageSelectionMode
        {
            get { return (ListViewSelectionMode)GetValue(ImageSelectionModeProperty); }
            set { SetValue(ImageSelectionModeProperty, value); }
        }

        public string SubheaderText
        {
            get { return (string)GetValue(SubheaderTextProperty); }
            set { SetValue(SubheaderTextProperty, value); }
        }

        public HorizontalAlignment TryAnotherButtonAlignment
        {
            get { return (HorizontalAlignment)GetValue(TryAnotherButtonAlignmentProperty); }
            set { SetValue(TryAnotherButtonAlignmentProperty, value); }
        }

        public void SetSuggestedImageList(params string[] imageUrls)
        {
            //validate
            imageUrls = imageUrls ?? new string[] { };

            suggestedImagesGrid.ItemsSource = imageUrls.Select(url => new Tuple<ImageSource, ImageAnalyzer>(new BitmapImage(new Uri(url)), new ImageAnalyzer(url)));

            //reset scrolling
            suggestedImagesScroll.ResetScroll();
        }

        public void SetSuggestedImageList(IEnumerable<ImageSource> images)
        {
            //validate
            images = images ?? new ImageSource[] { };

            suggestedImagesGrid.ItemsSource = images.Select(i => new Tuple<ImageSource, ImageAnalyzer>(i, new ImageAnalyzer((i as BitmapImage).UriSource.AbsoluteUri)));

            //reset scrolling
            suggestedImagesScroll.ResetScroll();
        }

        public async Task SetSuggestedImageList(params Uri[] imageLocalUris)
        {
            //validate
            imageLocalUris = imageLocalUris ?? new Uri[] { };

            var imageList = new List<Tuple<ImageSource, ImageAnalyzer>>();
            foreach (Uri uri in imageLocalUris)
            {
                StorageFile localImageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
                imageList.Add(new Tuple<ImageSource, ImageAnalyzer>(new BitmapImage(uri), new ImageAnalyzer(localImageFile.OpenStreamForReadAsync)));
            }

            suggestedImagesGrid.ItemsSource = imageList;

            //reset scrolling
            suggestedImagesScroll.ResetScroll();
        }

        public ImagePickerControl()
        {
            this.InitializeComponent();

            inputSourcesGridView.ItemsSource = new[]
                {
                    new { Gliph = "\uE721", Label = "From search", Tag = ImagePickerState.BingImageSearch, IsWide = true },
                    new { Gliph = "\uE722", Label = "From camera", Tag = ImagePickerState.CameraStream, IsWide = false },
                    new { Gliph = "\uF12B", Label = "From local file", Tag = ImagePickerState.LocalFile, IsWide = false }
                };

            DataContext = this;
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
                if (!string.IsNullOrEmpty(query))
                {
                    await Util.GenericApiCallExceptionHandler(ex, "Failure searching on Bing Images");
                }
            }

            this.progressRing.IsActive = false;
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

        private async Task OpenFilePickerDialogAsync()
        {
            try
            {
                FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary, ViewMode = PickerViewMode.Thumbnail };
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".jpeg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.FileTypeFilter.Add(".bmp");

                IReadOnlyList<StorageFile> selectedFiles = null;
                if (ImageSelectionMode == ListViewSelectionMode.Multiple)
                {
                    selectedFiles = await fileOpenPicker.PickMultipleFilesAsync();
                }
                else
                {
                    var singleFile = await fileOpenPicker.PickSingleFileAsync();
                    if (singleFile != null)
                    {
                        selectedFiles = new StorageFile[] { singleFile };
                    }
                }

                if (selectedFiles != null && selectedFiles.Any())
                {
                    ProcessImageSelection(selectedFiles.Select(file => new ImageAnalyzer(file.OpenStreamForReadAsync, file.Path)));
                }
            }
            catch (Exception ex)
            {
                this.imageResultsGrid.ItemsSource = null;
                await Util.GenericApiCallExceptionHandler(ex, "Failure processing local images");
            }
        }

        private async void OnInputTypeItemClicked(object sender, ItemClickEventArgs e)
        {
            dynamic targetMode = ((dynamic)e.ClickedItem).Tag;

            if (targetMode == ImagePickerState.LocalFile)
            {
                await OpenFilePickerDialogAsync();
            }
            else if (targetMode == ImagePickerState.CameraStream)
            {
                CurrentState = ImagePickerState.CameraStream;
                await cameraControl.StartStreamAsync();
            }
            else
            {
                CurrentState = (ImagePickerState)targetMode;
            }
        }

        private async void OnBackToInputSelectionClicked(object sender, RoutedEventArgs e)
        {
            CurrentState = ImagePickerState.InputTypes;

            // make sure camera stops in case it was up
            await cameraControl.StopStreamAsync();
        }

        private async void OnTryAnotherImageClicked(object sender, RoutedEventArgs e)
        {
            CurrentState = stateAtLastPickedImage;

            if (CurrentState == ImagePickerState.CameraStream)
            {
                await cameraControl.StartStreamAsync();
            }
        }

        private void ProcessImageSelection(IEnumerable<ImageAnalyzer> imgs)
        {
            this.OnImageSearchCompleted?.Invoke(this, imgs);

            stateAtLastPickedImage = CurrentState;
            CurrentState = ImagePickerState.ShowingSelectedImage;

        }

        private async void OnCameraPhotoCaptured(object sender, ImageAnalyzer e)
        {
            ProcessImageSelection(new ImageAnalyzer[] { e });

            await this.cameraControl.StopStreamAsync();
        }

        private void OnCameraAvailableSpaceChanged(object sender, SizeChangedEventArgs e)
        {
            double aspectRatio = (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);

            double desiredHeight = this.webCamHostGridParent.ActualWidth / aspectRatio;

            if (desiredHeight > this.webCamHostGridParent.ActualHeight)
            {
                // optimize for height
                this.webCamHostGrid.Height = this.webCamHostGridParent.ActualHeight;
                this.webCamHostGrid.Width = this.webCamHostGridParent.ActualHeight * aspectRatio;
            }
            else
            {
                // optimize for width
                this.webCamHostGrid.Height = desiredHeight;
                this.webCamHostGrid.Width = this.webCamHostGridParent.ActualWidth;
            }
        }

        private void OnImageItemClicked(object sender, ItemClickEventArgs e)
        {
            ProcessImageSelection(new ImageAnalyzer[] { e.ClickedItem as ImageAnalyzer });
        }

        private void OnSuggestedImageItemClicked(object sender, ItemClickEventArgs e)
        {
            var imgAnalyzer = e.ClickedItem as Tuple<ImageSource, ImageAnalyzer>;
            ProcessImageSelection(new ImageAnalyzer[] { imgAnalyzer.Item2 });
        }
    }

    public class WideStyleSelector : StyleSelector
    {
        public Style WideStyle { get; set; }
        public Style DefaultStyle { get; set; }
        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            if (((dynamic)item).IsWide)
            {
                return WideStyle;
            }
            return DefaultStyle;
        }
    }
}
