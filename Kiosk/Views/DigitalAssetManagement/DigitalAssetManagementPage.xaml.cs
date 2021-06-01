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

using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.DigitalAssetManagement
{
    [KioskExperience(Id = "DigitalAssetManagement",
        DisplayName = "Digital Asset Management",
        Description = "See how Computer Vision can add a layer of insights to image collections",
        ImagePath = "ms-appx:/Assets/DemoGallery/Image Collection Insights.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.Face | TechnologyType.Emotion | TechnologyType.Vision,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2019/10/30")]
    public sealed partial class DigitalAssetManagementPage : Page, INotifyPropertyChanged
    {
        const int _maxImageCountPerProcessingCycle = 50;

        ImageProcessor _imageProcessor = new ImageProcessor();
        DigitalAssetData _currentData;
        CustomVisionTrainingClient _customVisionTraining;
        CustomVisionPredictionClient _customVisionPrediction;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ProjectViewModel> CustomVisionProjects { get; set; } = new ObservableCollection<ProjectViewModel>();
        public FilesViewModel FileManager { get; } = new FilesViewModel();
        public ImageFiltersViewModel ImageFilters { get; } = new ImageFiltersViewModel();

        public DigitalAssetManagementPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey) ||
                string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey))
            {
                await new MessageDialog("Missing Face or Vision API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }

            FaceListManager.FaceListsUserDataFilter = SettingsHelper.Instance.WorkspaceKey + "_DigitalAssetManagement";
            await FaceListManager.Initialize();

            //load files
            await FileManager.LoadFilesAsync();

            //setup Custom Vision
            await InitCustomVision();

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await FaceListManager.ResetFaceLists();

            base.OnNavigatingFrom(e);
        }

        async Task InitCustomVision()
        {
            //setup custom vision clients
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey) &&
                !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionApiKey) &&
                !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionResourceId))
            {
                _customVisionTraining = new CustomVisionTrainingClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(SettingsHelper.Instance.CustomVisionTrainingApiKey))
                {
                    Endpoint = SettingsHelper.Instance.CustomVisionTrainingApiKeyEndpoint
                };
                _customVisionPrediction = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(SettingsHelper.Instance.CustomVisionPredictionApiKey))
                {
                    Endpoint = SettingsHelper.Instance.CustomVisionPredictionApiKeyEndpoint
                };
            }

            //get custom vision projects
            CustomVisionProjects.Clear();
            if (_customVisionTraining != null)
            {
                var projects = await _customVisionTraining.GetProjectsAsync();
                CustomVisionProjects.AddRange(projects.OrderBy(i => i.Name).Select(i => new ProjectViewModel { Project = i }));
            }

            //enable UI
            CustomVisionApi.IsEnabled = _customVisionTraining != null;
        }

        public DigitalAssetData CurrentData
        {
            get => _currentData;
            set
            {
                _currentData = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentData)));
            }
        }

        private void StartOverClicked(object sender, RoutedEventArgs e)
        {
            StartOver();
        }

        void StartOver()
        {
            //reset back to loading screen
            this.landingMessage.Visibility = Visibility.Visible;
            this.filterTab.Visibility = Visibility.Collapsed;
            this.ActiveFilters.Visibility = Visibility.Collapsed;
            this.ImagesContainer.Visibility = Visibility.Collapsed;
            ImageFilters.Clear();
            CurrentData = null;
            progressRing.IsActive = false;
        }

        async void LoadTypeClicked(object sender, ItemClickEventArgs e)
        {
            var tag = (e.ClickedItem as FrameworkElement).Tag;
            switch (tag)
            {
                case "file":
                    await LoadFromFile();
                    break;
                case "storage":
                    await LoadFromStorage();
                    break;
            }
        }

        async Task LoadFromFile()
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    StorageApplicationPermissions.FutureAccessList.Add(folder);
                    await LoadImages(new FileSource(new Uri(folder.Path)), _maxImageCountPerProcessingCycle, null);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error loading from the target folder.");
            }
        }

        async Task LoadFromStorage()
        {
            try
            {
                var dialog = new StorageDialog();
                if ((await dialog.ShowAsync()) == ContentDialogResult.Primary)
                {
                    await LoadImages(new StorageSource(new Uri(dialog.SasUri)), _maxImageCountPerProcessingCycle, null);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error loading from the target storage.");
            }
        }

        async Task LoadImages(ImageProcessorSource source, int? fileLimit, int? startingFileIndex)
        {
            //prepare UI
            this.progressRing.IsActive = true;
            this.landingMessage.Visibility = Visibility.Collapsed;
            this.filterTab.Visibility = Visibility.Visible;
            this.ActiveFilters.Visibility = Visibility.Visible;
            this.ImagesContainer.Visibility = Visibility.Visible;
            ImageFilters.Clear();
            await FaceListManager.ResetFaceLists();

            try
            {
                //convert to view model
                var serviceTypes = (FaceApi.IsChecked.GetValueOrDefault() ? ImageProcessorServiceType.Face : 0) |
                    (ComputerVisionApi.IsChecked.GetValueOrDefault() ? ImageProcessorServiceType.ComputerVision : 0) |
                    (CustomVisionApi.IsChecked.GetValueOrDefault() ? ImageProcessorServiceType.CustomVision : 0);
                var customVisionProjects = CustomVisionProjects.Where(i => i.IsSelected).Select(i => i.Project.Id).ToArray();
                var data = await _imageProcessor.ProcessImagesAsync(source, await GetServices(serviceTypes, customVisionProjects), fileLimit, startingFileIndex, async insight =>
                {
                    await ImageFilters.AddImage(insight);
                });
                CurrentData = data;

                ImageFilters.AddImagesCompleted();

                //save data
                await FileManager.SaveFileAsync(CurrentData);
            }
            catch
            {
                StartOver();
                throw;
            }
            finally
            {
                //finished
                this.progressRing.IsActive = false;
            }
        }

        async Task LoadImages(DigitalAssetData data)
        {
            //prepare UI
            this.progressRing.IsActive = true;
            this.landingMessage.Visibility = Visibility.Collapsed;
            this.filterTab.Visibility = Visibility.Visible;
            this.ActiveFilters.Visibility = Visibility.Visible;
            this.ImagesContainer.Visibility = Visibility.Visible;
            ImageFilters.Clear();
            CurrentData = null;

            try
            {
                foreach (var insight in data.Insights)
                {
                    await ImageFilters.AddImage(insight);
                }
                CurrentData = data;

                ImageFilters.AddImagesCompleted();
            }
            catch (Exception ex)
            {
                StartOver();
                await Util.GenericApiCallExceptionHandler(ex, "Error loading from history.");
            }
            finally
            {
                //finished
                this.progressRing.IsActive = false;
            }
        }

        async Task LoadMoreImages(int? fileLimit)
        {
            //validate
            var data = CurrentData;
            if (data == null)
            {
                return;
            }

            //prepare UI
            this.progressRing.IsActive = true;

            try
            {
                //create source
                var source = data.Info.Source == "StorageSource" ? (ImageProcessorSource)new StorageSource(data.Info.Path) : new FileSource(data.Info.Path);

                //convert to view model
                var newData = await _imageProcessor.ProcessImagesAsync(source, await GetServices(data.Info.Services, data.Info.CustomVisionProjects), fileLimit, data.Info.LastFileIndex, async insight =>
                {
                    await ImageFilters.AddImage(insight);
                });
                newData.Insights = data.Insights.Concat(newData.Insights).ToArray();
                CurrentData = newData;

                ImageFilters.AddImagesCompleted();

                //save data
                await FileManager.SaveFileAsync(CurrentData);
            }
            catch
            {
                StartOver();
                throw;
            }
            finally
            {
                //finished
                this.progressRing.IsActive = false;
            }
        }

        async void History_ItemClick(object sender, ItemClickEventArgs e)
        {
            (sender as ListViewBase).SelectedItem = null;

            //load the data
            var data = await FileManager.GetFileData((e.ClickedItem as FileViewModel).File);
            if (data == null)
            {
                await Util.GenericApiCallExceptionHandler(new Exception("failed to load json file"), "Error loading from history.");
            }

            HistoryFlyout.Hide();

            //load the images
            await LoadImages(data);
        }

        async void Reprocess_Click(object sender, RoutedEventArgs e)
        {
            await LoadMoreImages(_maxImageCountPerProcessingCycle);
        }

        async void Download_Click(object sender, RoutedEventArgs e)
        {
            await FileManager.DownloadFileAsync((sender as FrameworkElement).DataContext as FileViewModel);
        }

        async void Delete_Click(object sender, RoutedEventArgs e)
        {
            await FileManager.DeleteFileAsync((sender as FrameworkElement).DataContext as FileViewModel);
        }

        async Task<IImageProcessorService[]> GetServices(ImageProcessorServiceType serviceTypes, Guid[] customVisionProjects)
        {
            var result = new List<IImageProcessorService>();
            if (serviceTypes.HasFlag(ImageProcessorServiceType.Face))
            {
                result.Add(new FaceProcessorService());
            }
            if (serviceTypes.HasFlag(ImageProcessorServiceType.ComputerVision))
            {
                result.Add(new ComputerVisionProcessorService());
            }
            if (serviceTypes.HasFlag(ImageProcessorServiceType.CustomVision))
            {
                result.Add(new CustomVisionProcessorService(_customVisionPrediction, await CustomVisionProcessorService.GetProjectIterations(_customVisionTraining, SettingsHelper.Instance.CustomVisionPredictionResourceId, customVisionProjects)));
            }
            return result.ToArray();
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            ImageFilters.ApplyFilters();
        }

        private void WordSearch(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Tag == null) //flag to ignore updating the autosuggest list
                {
                    System.Diagnostics.Debug.WriteLine(args.CheckCurrent());
                    //find suggestion for last word
                    var lastWord = sender.Text.Trim().Split(' ').LastOrDefault() ?? string.Empty;
                    //pick top 5 words sorting words starting with on top
                    sender.ItemsSource = ImageFilters.WordFilters
                        .Where(i => ((string)i.Key).Contains(lastWord, StringComparison.OrdinalIgnoreCase)) //contains word
                        .OrderBy(i => ((string)i.Key).StartsWith(lastWord, StringComparison.OrdinalIgnoreCase) ? 1 : 2) //put terms starting with word on top
                        .Select(i => i.Key).Take(5); //top 5
                }
                else
                {
                    sender.Tag = null; //reset the flag
                }
            }
        }

        private void WordSearchQuery(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ImageFilters.ApplyWordsFilter(sender.Text);
        }

        private void WordSearchChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            //replace last word only
            var words = sender.Text.Trim().Split(' ');
            words[words.Length - 1] = args.SelectedItem as string;
            sender.Text = string.Join(' ', words);
            sender.Tag = true; //flag to ignore updating the autosuggest list
        }

        private void ShowAllToggle(object sender, RoutedEventArgs e)
        {
            var filter = (sender as FrameworkElement)?.DataContext as FilterCollection;
            if (filter != null)
            {
                filter.IsShowingAll = !filter.IsShowingAll;
            }
        }

        private void ImagesContainer_ItemClick(object sender, ItemClickEventArgs e)
        {
            DetailView.DataContext = e.ClickedItem;
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void NavigateCustomVisionSetup(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            Frame.Navigate(typeof(CustomVisionSetup));
        }

        public class ProjectViewModel : INotifyPropertyChanged
        {
            bool _isSelected;

            public event PropertyChangedEventHandler PropertyChanged;

            public Project Project { get; set; }
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
    }
}
