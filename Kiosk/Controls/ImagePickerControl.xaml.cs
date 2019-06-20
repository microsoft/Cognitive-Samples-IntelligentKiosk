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

namespace IntelligentKioskSample.Controls
{
    public enum ImagePickerState
    {
        InputTypes,
        CameraStream,
        BingImageSearch,
        ImageSuggestions,
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
            suggestedImagesGrid.ItemsSource = imageUrls.Select(url => new ImageAnalyzer(url));
        }

        public ImagePickerControl()
        {
            this.InitializeComponent();

            inputSourcesGridView.ItemsSource = new[]
                {
                    new { Gliph = "\uEB9F", Label = "From suggestions", Tag = ImagePickerState.ImageSuggestions },
                    new { Gliph = "\uE721", Label = "From search", Tag = ImagePickerState.BingImageSearch },
                    new { Gliph = "\uE722", Label = "From camera", Tag = ImagePickerState.CameraStream },
                    new { Gliph = "\uE8B7", Label = "From local file", Tag = ImagePickerState.LocalFile }
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
    }
}
