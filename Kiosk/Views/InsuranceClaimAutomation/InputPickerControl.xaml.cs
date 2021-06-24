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

using IntelligentKioskSample.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Views.InsuranceClaimAutomation
{
    public enum InputType
    {
        Image,
        Form
    }

    public sealed partial class InputPickerControl : UserControl, INotifyPropertyChanged
    {
        public static readonly string[] PdfExtensions = new string[] { ".pdf" };
        public static readonly string[] ImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };

        public static readonly DependencyProperty InputTypeProperty =
            DependencyProperty.Register("InputType",
                typeof(InputType),
                typeof(InputPickerControl),
                new PropertyMetadata(InputType.Image, InputTypePropertyChangedCallback));

        public static readonly DependencyProperty SampleCollectionProperty =
            DependencyProperty.Register(
                "SampleCollection",
                typeof(ObservableCollection<InputSampleViewModel>),
                typeof(InputPickerControl),
                new PropertyMetadata(null));

        public InputType InputType
        {
            get { return (InputType)GetValue(InputTypeProperty); }
            set { SetValue(InputTypeProperty, value); }
        }

        public ObservableCollection<InputSampleViewModel> SampleCollection
        {
            get { return (ObservableCollection<InputSampleViewModel>)GetValue(SampleCollectionProperty); }
            set { SetValue(SampleCollectionProperty, value); }
        }

        static void InputTypePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((InputPickerControl)d).InputTypeChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public TabHeader ExamplesTab = new TabHeader() { Name = "Examples" };
        public TabHeader CameraTab = new TabHeader() { Name = "Camera" };
        public TabHeader FileTab = new TabHeader() { Name = "From File" };
        public TabHeader SearchTab = new TabHeader() { Name = "Search" };

        private PivotItem selectedTab;
        public PivotItem SelectedTab
        {
            get { return selectedTab; }
            set
            {
                if (SetProperty(ref selectedTab, value))
                {
                    TabChanged(SelectedTab);
                }
            }
        }

        private bool enableListView = false;
        public bool EnableListView
        {
            get { return enableListView; }
            set
            {
                enableListView = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EnableListView"));
            }
        }

        public event EventHandler<ImageAnalyzer> OnItemSearchCompleted;
        public event EventHandler<StorageFile> OnFileSearchCompleted;

        public InputPickerControl()
        {
            this.InitializeComponent();
        }

        public void RestartPicker()
        {
            if (SelectedTab != null)
            {
                TabChanged(SelectedTab);
            }
            else
            {
                this.pivot.SelectedIndex = 0;
            }
        }

        private void InputTypeChanged()
        {
            SearchTab.IsVisible = InputType == InputType.Image;
            EnableListView = InputType == InputType.Form;
        }

        private async void TabChanged(PivotItem selectedPivot)
        {
            await cameraControl.StopStreamAsync();

            if (selectedPivot.Header is TabHeader selectedTab)
            {
                if (selectedTab == CameraTab)
                {
                    await cameraControl.StartStreamAsync();
                }
            }
        }

        private async void OnCameraPhotoCaptured(object sender, ImageAnalyzer img)
        {
            this.OnItemSearchCompleted?.Invoke(this, img);

            await this.cameraControl.StopStreamAsync();
        }

        private async void OnSampleItemClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is InputSampleViewModel sampleImage)
            {
                StorageFile localImageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(sampleImage.InputUrl));
                this.OnItemSearchCompleted?.Invoke(this, new ImageAnalyzer(localImageFile.OpenStreamForReadAsync));
            }
        }

        private async void OnBrowseFileButtonClicked(object sender, RoutedEventArgs e)
        {
            await GetImageFromFileAsync();
        }

        private void OnImageItemClicked(object sender, ItemClickEventArgs e)
        {
            this.OnItemSearchCompleted?.Invoke(this, e.ClickedItem as ImageAnalyzer);
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

        private async void onQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await QueryBingImages(args.QueryText);
        }

        private async Task QueryBingImages(string query)
        {
            this.progressRing.IsActive = true;

            try
            {
                IEnumerable<string> imageUrls = await BingSearchHelper.GetImageSearchResults(query, imageContent: string.Empty, count: 30);
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

        private async Task GetImageFromFileAsync()
        {
            var fileTypeFilter = InputType == InputType.Image ? ImageExtensions : ImageExtensions.Concat(PdfExtensions).ToArray();
            StorageFile file = await Util.PickSingleFileAsync(fileTypeFilter);
            if (file != null)
            {
                if (ImageExtensions.Contains(file.FileType))
                {
                    this.OnItemSearchCompleted?.Invoke(this, new ImageAnalyzer(file.OpenStreamForReadAsync, file.Path));
                }
                else
                {
                    this.OnFileSearchCompleted?.Invoke(this, file);
                }
            }
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
    }

    public class InputResult
    {
        public ImageAnalyzer Image { get; set; }
        public StorageFile File { get; set; }
    }

    public class InputSampleViewModel
    {
        public string InputUrl { get; private set; }
        public string FileName { get; private set; }
        public InputType Type { get; private set; }

        public InputSampleViewModel(string imageUrl)
        {
            InputUrl = imageUrl;
            Type = InputType.Image;
        }

        public InputSampleViewModel(string fileName, string fileUrl)
        {
            FileName = fileName;
            InputUrl = fileUrl;
            Type = InputType.Form;
        }
    }
}
