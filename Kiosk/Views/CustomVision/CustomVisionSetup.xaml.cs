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
using IntelligentKioskSample.Views.CustomVision;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    public class ProjectDomainViewModel
    {
        public string DisplayName { get; set; }
        public Guid DomainId { get; set; }
    }

    public class ExportableProjectViewModel
    {
        public string Name { get; set; }
        public string[] Labels { get; set; }
        public string CoremlDownloadUri { get; set; }
        public string TensorflowDownloadUri { get; set; }
        public string OnnxDownloadUri { get; set; }
    }

    public class ImageViewModel
    {
        public event EventHandler ImageDataChanged;
        public Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image Image { get; set; }
        public bool SupportsImageRegions { get; set; }
        public IEnumerable<Tag> AvailableTags { get; set; }
        public Tag TagHintForNewRegions { get; set; }

        public IList<ImageRegion> AddedImageRegions { get; set; } = new List<ImageRegion>();
        public IList<ImageRegion> DeletedImageRegions { get; set; } = new List<ImageRegion>();

        public void UpdateImageData(Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image image)
        {
            this.Image = image;
            this.ImageDataChanged?.Invoke(this, EventArgs.Empty);
            this.AddedImageRegions.Clear();
            this.DeletedImageRegions.Clear();
        }
    }

    public sealed partial class CustomVisionSetup : Page
    {
        private bool needsTraining = false;
        private CancellationTokenSource downloadCancellationTokenSource;
        private CustomVisionModelData newExportedCustomVisionModel;
        private CustomVisionTrainingClient trainingApi;

        public ObservableCollection<Project> Projects { get; set; } = new ObservableCollection<Project>();
        public Project CurrentProject { get; set; }
        public ObservableCollection<Tag> TagsInCurrentGroup { get; set; } = new ObservableCollection<Tag>();
        public ObservableCollection<ImageViewModel> SelectedTagImages { get; set; } = new ObservableCollection<ImageViewModel>();
        public Tag SelectedTag { get; set; }
        public Iteration LatestTrainedIteration { get; set; }

        public ObservableCollection<Platform> Platforms { get; set; } = new ObservableCollection<Platform>(
            new List<Platform>()
            {
                new Platform { DisplayName = "ONNX (Windows)", PlatformType = PlatformType.Windows }
            });

        public IEnumerable<ProjectDomainViewModel> ProjectDomainViewModelCollection = new ProjectDomainViewModel[]
        {
            new ProjectDomainViewModel { DomainId = new Guid("ee85a74c-405e-4adc-bb47-ffa8ca0c9f31"), DisplayName = "Image Classification, General" },
            new ProjectDomainViewModel { DomainId = new Guid("c151d5b5-dd07-472a-acc8-15d29dea8518"), DisplayName = "Image Classification, Food" },
            new ProjectDomainViewModel { DomainId = new Guid("ca455789-012d-4b50-9fec-5bb63841c793"), DisplayName = "Image Classification, Landmarks" },
            new ProjectDomainViewModel { DomainId = new Guid("b30a91ae-e3c1-4f73-a81e-c270bff27c39"), DisplayName = "Image Classification, Retail" },
            new ProjectDomainViewModel { DomainId = new Guid("45badf75-3591-4f26-a705-45678d3e9f5f"), DisplayName = "Image Classification, Adult" },
            new ProjectDomainViewModel { DomainId = new Guid("0732100f-1a38-4e49-a514-c9b44c697ab5"), DisplayName = "Image Classification, General (exportable)" },
            new ProjectDomainViewModel { DomainId = new Guid("b5cfd229-2ac7-4b2b-8d0a-2b0661344894"), DisplayName = "Image Classification, Landmarks (exportable)" },
            new ProjectDomainViewModel { DomainId = new Guid("6b4faeda-8396-481b-9f8b-177b9fa3097f"), DisplayName = "Image Classification, Retail (exportable)" },
            new ProjectDomainViewModel { DomainId = new Guid("da2e3a8a-40a5-4171-82f4-58522f70fbc1"), DisplayName = "Object Detection, General" },
            new ProjectDomainViewModel { DomainId = new Guid("1d8ffafe-ec40-4fb2-8f90-72b3b6cecea4"), DisplayName = "Object Detection, Logo" },
            new ProjectDomainViewModel { DomainId = new Guid("a27d5ca5-bb19-49d8-a70a-fec086c47f5b"), DisplayName = "Object Detection, General (exportable)" }
        };

        public CustomVisionSetup()
        {
            this.InitializeComponent();

            this.projectTypeComboBox.ItemsSource = this.ProjectDomainViewModelCollection;
            this.projectTypeComboBox.SelectedIndex = 0;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = this;

            if (string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey) ||
                string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionApiKey) ||
                string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionResourceId))
            {
                await new MessageDialog("Please enter Custom Vision API Keys in the Settings Page.", "Missing API Keys").ShowAsync();
                this.addProjectButton.IsEnabled = false;
                this.trainButton.IsEnabled = false;
                this.exportButton.IsEnabled = false;
            }
            else
            {
                await InitializeTrainingApi();
            }

            base.OnNavigatedTo(e);
        }

        private async Task InitializeTrainingApi()
        {
            trainingApi = new CustomVisionTrainingClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(SettingsHelper.Instance.CustomVisionTrainingApiKey))
            {
                Endpoint = SettingsHelper.Instance.CustomVisionTrainingApiKeyEndpoint
            };
            this.addProjectButton.IsEnabled = true;
            this.trainButton.IsEnabled = true;
            this.exportButton.IsEnabled = true;
            await this.LoadProjectsFromService();
        }

        protected async override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (this.needsTraining)
            {
                e.Cancel = true;
                await Util.ConfirmActionAndExecute("It looks like you made modifications but didn't train the model afterwards. Would you like to train it now?", async () => await this.TrainProjectsAsync());
            }

            base.OnNavigatingFrom(e);
        }

        #region Project management

        private async Task LoadProjectsFromService()
        {
            try
            {
                this.progressControl.IsActive = true;
                this.Projects.Clear();

                IEnumerable<Project> projects = await trainingApi.GetProjectsAsync();
                this.Projects.AddRange(projects.OrderBy(p => p.Name));

                if (this.projectsListView.Items.Any())
                {
                    this.projectsListView.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading projects");
            }
            finally
            {
                this.progressControl.IsActive = false;
            }
        }

        private async void OnAddProjectButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Project model = await trainingApi.CreateProjectAsync(this.projectNameTextBox.Text, this.projectNameTextBox.Text, (Guid)this.projectTypeComboBox.SelectedValue);

                this.Projects.Add(model);
                this.projectsListView.SelectedValue = model;

                this.projectNameTextBox.Text = "";
                this.addProjectFlyout.Hide();

                this.needsTraining = true;
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure creating project");
            }
        }

        private void OnCancelAddProjectButtonClicked(object sender, RoutedEventArgs e)
        {
            this.projectNameTextBox.Text = "";
            this.addProjectFlyout.Hide();
        }

        private async void OnDeleteProjectClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete project?", async () => 
            {
                await UnpublishIterations();
                await DeleteProjectAsync();
            });
        }

        private async Task UnpublishIterations()
        {
            try
            {
                IList<Iteration> publishedIterations = (await trainingApi.GetIterationsAsync(this.CurrentProject.Id)).Where(i => !string.IsNullOrEmpty(i.PublishName)).ToList();
                foreach (Iteration iteration in publishedIterations)
                {
                    await trainingApi.UnpublishIterationAsync(this.CurrentProject.Id, iteration.Id);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure unpublish iterations");
            }
        }

        private async Task DeleteProjectAsync()
        {
            try
            {
                await trainingApi.DeleteProjectAsync(this.CurrentProject.Id);
                this.Projects.Remove(this.CurrentProject);

                this.TagsInCurrentGroup.Clear();
                this.SelectedTagImages.Clear();

                this.needsTraining = false;

                if (this.projectsListView.Items.Any())
                {
                    this.projectsListView.SelectedIndex = 0;
                }
                else
                {
                    this.exportButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting project");
            }
        }

        private async void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.needsTraining && (Project)this.projectsListView.SelectedValue != this.CurrentProject)
            {
                // revert
                this.projectsListView.SelectedValue = this.CurrentProject;

                await Util.ConfirmActionAndExecute("It looks like you made modifications but didn't train the model afterwards. Would you like to train it now?", async () => await this.TrainProjectsAsync());
            }
            else
            {
                this.CurrentProject = (Project)this.projectsListView.SelectedValue;

                if (this.CurrentProject != null)
                {
                    await this.LoadTagsInCurrentProject();
                    await this.InitProjectExportFeatureAsync();
                }
            }
        }

        #endregion

        #region Tag management

        private async void OnTagSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.projectTagsListView.SelectedValue != null)
            {
                this.SelectedTag = this.projectTagsListView.SelectedValue as Tag;
                await this.LoadTagImagesFromService();
            }
            else
            {
                this.SelectedTagImages.Clear();
            }
        }

        private async Task LoadTagsInCurrentProject()
        {
            this.TagsInCurrentGroup.Clear();

            try
            {
                this.TagsInCurrentGroup.AddRange((await trainingApi.GetTagsAsync(this.CurrentProject.Id)).OrderBy(t => t.Name));
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure loading tags in the project");
            }
        }

        private async Task LoadLatestIterationInCurrentProject()
        {
            try
            {
                IList<Iteration> iterations = await trainingApi.GetIterationsAsync(this.CurrentProject.Id);
                this.LatestTrainedIteration = iterations.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure loading latest iteration in the project");
            }
        }

        private async Task InitProjectExportFeatureAsync()
        {
            await this.LoadLatestIterationInCurrentProject();
            bool isValidDomain = this.ProjectDomainViewModelCollection.Any(item => item.DomainId.Equals(this.CurrentProject.Settings.DomainId));
            this.exportButton.IsEnabled = this.LatestTrainedIteration != null && this.LatestTrainedIteration.Exportable && isValidDomain;
        }

        private async void OnAddTagButtonClicked(object sender, RoutedEventArgs e)
        {
            await this.AddTag(this.tagNameTextBox.Text);
        }

        private async void OnTagNameQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await this.AddTag(args.ChosenSuggestion != null ? args.ChosenSuggestion.ToString() : args.QueryText);
        }

        private async Task AddTag(string name)
        {
            name = Util.CapitalizeString(name);

            await this.CreateTagAsync(name);
            this.projectTagsListView.SelectedValue = this.TagsInCurrentGroup.FirstOrDefault(p => p.Name == name);
            trainingImageCollectorFlyout.ShowAt(this.addImagesButton);
        }

        private void DismissFlyout()
        {
            this.addTagFlyout.Hide();
            this.tagNameTextBox.Text = "";
        }

        private void OnCancelAddTagButtonClicked(object sender, RoutedEventArgs e)
        {
            this.DismissFlyout();
        }

        private async void OnTagNameTextBoxChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try
                {
                    this.tagNameTextBox.ItemsSource = await BingSearchHelper.GetAutoSuggestResults(this.tagNameTextBox.Text);
                }
                catch (HttpRequestException)
                {
                    // default to no suggestions
                    this.tagNameTextBox.ItemsSource = null;
                }
            }
        }

        private async Task CreateTagAsync(string name)
        {
            try
            {
                Tag result = await trainingApi.CreateTagAsync(this.CurrentProject.Id, name);
                this.TagsInCurrentGroup.Add(result);
                this.needsTraining = true;
                this.DismissFlyout();
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure creating tag");
            }
        }

        private async void OnDeleteTagClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete tag?", async () => { await DeleteTagAsync(); });
        }

        private async Task DeleteTagAsync()
        {
            try
            {
                await trainingApi.DeleteTagAsync(this.CurrentProject.Id, this.SelectedTag.Id);
                this.TagsInCurrentGroup.Remove(this.SelectedTag);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting tag");
            }
        }

        #endregion

        #region Image management

        private async Task LoadTagImagesFromService()
        {
            this.progressControl.IsActive = true;

            this.SelectedTagImages.Clear();

            try
            {
                IEnumerable<Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image> images = await trainingApi.GetTaggedImagesAsync(this.CurrentProject.Id, null, new List<Guid>() { this.SelectedTag.Id }, null, 200);
                this.SelectedTagImages.AddRange(images.Select(img =>
                    new ImageViewModel
                    {
                        Image = img,
                        SupportsImageRegions = CustomVisionServiceHelper.ObjectDetectionDomainGuidList.Contains(this.CurrentProject.Settings.DomainId),
                        AvailableTags = this.TagsInCurrentGroup,
                        TagHintForNewRegions = SelectedTag
                    }));
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure loading images for this tag");
            }

            this.progressControl.IsActive = false;
        }

        private void OnImageSearchCanceled(object sender, EventArgs e)
        {
            this.trainingImageCollectorFlyout.Hide();
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            await AddTrainingImages(args);
        }

        private async void OnCameraFrameCaptured(object sender, IEnumerable<ImageAnalyzer> e)
        {
            await this.AddTrainingImages(e, dismissImageCollectorFlyout: false);
        }

        private async Task AddTrainingImages(IEnumerable<ImageAnalyzer> args, bool dismissImageCollectorFlyout = true)
        {
            this.progressControl.IsActive = true;

            if (dismissImageCollectorFlyout)
            {
                this.trainingImageCollectorFlyout.Hide();
            }

            bool foundError = false;
            Exception lastError = null;
            foreach (var item in args)
            {
                try
                {
                    ImageCreateSummary addResult;
                    if (item.GetImageStreamCallback != null)
                    {
                        addResult = await trainingApi.CreateImagesFromDataAsync(
                            this.CurrentProject.Id,
                            await item.GetImageStreamCallback(), new List<Guid> { this.SelectedTag.Id });
                    }
                    else
                    {
                        addResult = await trainingApi.CreateImagesFromUrlsAsync(
                            this.CurrentProject.Id,
                            new ImageUrlCreateBatch(new ImageUrlCreateEntry[] { new ImageUrlCreateEntry(item.ImageUrl) }, new Guid[] { this.SelectedTag.Id }));
                    }

                    if (addResult != null)
                    {
                        this.SelectedTagImages.AddRange(addResult.Images.Select(r =>
                            new ImageViewModel
                            {
                                Image = r.Image,
                                SupportsImageRegions = CustomVisionServiceHelper.ObjectDetectionDomainGuidList.Contains(this.CurrentProject.Settings.DomainId),
                                AvailableTags = this.TagsInCurrentGroup,
                                TagHintForNewRegions = this.SelectedTag
                            }));

                        this.needsTraining = true;
                    }
                }
                catch (Exception e)
                {
                    foundError = true;
                    lastError = e;
                }
            }

            if (foundError)
            {
                await Util.GenericApiCallExceptionHandler(lastError, "Failure adding one or more of the images");
            }

            this.progressControl.IsActive = false;
        }

        private void OnImageSearchFlyoutOpened(object sender, object e)
        {
            this.bingSearchControl.TriggerSearch(this.SelectedTag.Name);
        }

        private async void OnDeleteImageClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in this.selectedTagImagesGridView.SelectedItems.ToArray())
                {
                    ImageViewModel tagImage = (ImageViewModel)item;
                    await trainingApi.DeleteImagesAsync(this.CurrentProject.Id, new List<Guid>() { tagImage.Image.Id });
                    this.SelectedTagImages.Remove(tagImage);

                    this.needsTraining = true;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting images");
            }
        }

        #endregion

        #region Training processing

        private async void OnStartTrainingClicked(object sender, RoutedEventArgs e)
        {
            await TrainProjectsAsync();
            await InitProjectExportFeatureAsync();
        }

        private async Task TrainProjectsAsync()
        {
            this.progressControl.IsActive = true;

            bool trainingSucceeded = true;
            try
            {
                Iteration iterationModel = await trainingApi.TrainProjectAsync(this.CurrentProject.Id);

                while (true)
                {
                    iterationModel = await trainingApi.GetIterationAsync(this.CurrentProject.Id, iterationModel.Id);

                    if (iterationModel.Status != "Training")
                    {
                        if (iterationModel.Status == "Failed")
                        {
                            trainingSucceeded = false;
                        }
                        break;
                    }
                    await Task.Delay(500);
                }

                this.needsTraining = false;
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure requesting training");
            }

            this.progressControl.IsActive = false;

            if (!trainingSucceeded)
            {
                await new MessageDialog("Something went wrong and we couldn't train this model. Sorry :(", "Training failed").ShowAsync();
            }
        }

        #endregion

        private async void OnImageRegionsChangedByUser(object sender, ImageViewModel imageViewModel)
        {
            if (imageViewModel.Image.Regions != null && imageViewModel.Image.Regions.Any())
            {
                // remove the current regions from the image
                await this.trainingApi.DeleteImageRegionsAsync(this.CurrentProject.Id, imageViewModel.Image.Regions.Select(r => r.RegionId).ToList());

                // re-add the regions to the image based on the possibly new locations, taking into account the ones that might haven been deleted in the UI
                var regionsToReAdd = imageViewModel.Image.Regions.Where(r => !imageViewModel.DeletedImageRegions.Contains(r));
                if (regionsToReAdd.Any())
                {
                    await this.trainingApi.CreateImageRegionsAsync(
                        this.CurrentProject.Id,
                        new ImageRegionCreateBatch(regionsToReAdd.Select(r => new ImageRegionCreateEntry(imageViewModel.Image.Id, r.TagId, r.Left, r.Top, r.Width, r.Height)).ToArray())
                        );
                }
            }

            if (imageViewModel.AddedImageRegions.Any())
            {
                // add any new regions to the image 
                await this.trainingApi.CreateImageRegionsAsync(
                this.CurrentProject.Id,
                new ImageRegionCreateBatch(imageViewModel.AddedImageRegions.Select(r => new ImageRegionCreateEntry(imageViewModel.Image.Id, r.TagId, r.Left, r.Top, r.Width, r.Height)).ToArray())
                );
            }

            // update the regions that are shown in the thumbnail UI
            var newImages = await this.trainingApi.GetImagesByIdsAsync(this.CurrentProject.Id, new List<Guid>() { imageViewModel.Image.Id });
            imageViewModel.UpdateImageData(newImages.First());
        }

        private void OnShareProjectFlyoutOpened(object sender, object e)
        {
            this.shareStatusTextBlock.Text = "Please select the target platform";
        }

        private async Task ExportProject(Platform currentPlatform)
        {
            CustomVisionProjectType customVisionProjectType = CustomVisionServiceHelper.ObjectDetectionDomainGuidList.Contains(this.CurrentProject.Settings.DomainId)
                    ? CustomVisionProjectType.ObjectDetection : CustomVisionProjectType.Classification;

            bool success = false;
            try
            {
                this.shareStatusTextBlock.Text = "Exporting model...";
                this.shareStatusPanelDescription.Visibility = Visibility.Collapsed;
                this.closeFlyoutBtn.Visibility = Visibility.Visible;
                this.projectShareProgressRing.IsActive = true;

                // get latest iteration of the project
                if (this.LatestTrainedIteration == null)
                {
                    await this.LoadLatestIterationInCurrentProject();
                }

                if (LatestTrainedIteration != null && LatestTrainedIteration.Exportable)
                {
                    // get project's download Url for the particular platform Windows (ONNX) model
                    Export exportProject = await CustomVisionServiceHelper.ExportIteration(trainingApi, this.CurrentProject.Id, LatestTrainedIteration.Id);
                    success = await ExportOnnxProject(exportProject, customVisionProjectType);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "We couldn't export the model at this time.");
            }
            finally
            {
                this.projectShareProgressRing.IsActive = false;
                this.closeFlyoutBtn.Visibility = Visibility.Collapsed;
                this.shareStatusTextBlock.Text = success
                        ? "The project was exported successfully."
                        : "Something went wrong and we couldn't export the model. Sorry :(";
            }
        }

        private async Task<bool> ExportOnnxProject(Export exportProject, CustomVisionProjectType customVisionProjectType)
        {
            if (string.IsNullOrEmpty(exportProject?.DownloadUri))
            {
                throw new ArgumentNullException("Download Uri");
            }

            var newModelId = Guid.NewGuid();
            this.downloadCancellationTokenSource = new CancellationTokenSource();

            StorageFolder onnxProjectDataFolder = await CustomVisionDataLoader.GetOnnxModelStorageFolderAsync(customVisionProjectType);
            StorageFile file = await onnxProjectDataFolder.CreateFileAsync($"{newModelId}.onnx", CreationCollisionOption.ReplaceExisting);
            bool success = await Util.UnzipModelFileAsync(exportProject.DownloadUri, file, this.downloadCancellationTokenSource.Token);
            
            if (!success)
            {
                await file.DeleteAsync();
                return false;
            }

            string[] classLabels = this.TagsInCurrentGroup?.Select(x => x.Name)?.ToArray() ?? new string[] { };
            newExportedCustomVisionModel = new CustomVisionModelData
            {
                Id = newModelId,
                Name = CurrentProject.Name,
                ClassLabels = classLabels,
                ExportDate = DateTime.UtcNow,
                FileName = file.Name,
                FilePath = file.Path
            };

            List<CustomVisionModelData> customVisionModelList = await CustomVisionDataLoader.GetCustomVisionModelDataAsync(customVisionProjectType);
            CustomVisionModelData customVisionModelWithSameName = customVisionModelList.FirstOrDefault(x => string.Equals(x.Name, CurrentProject.Name));
            if (customVisionModelWithSameName != null)
            {
                string titleMessage = $"There is already a “{CurrentProject.Name}” model in this device. Select “Replace” if you would like to replace it, or “Keep Both” if you would like to keep both.";
                await Util.ConfirmActionAndExecute(titleMessage,
                    async () =>
                    {
                        // if user select Yes, we replace the model with the same name
                        bool modelEntryRemovedFromFile = customVisionModelList.Remove(customVisionModelWithSameName);
                        StorageFile modelFileToRemove = await onnxProjectDataFolder.GetFileAsync(customVisionModelWithSameName.FileName);
                        if (modelEntryRemovedFromFile && modelFileToRemove != null)
                        {
                            await modelFileToRemove.DeleteAsync();
                        }
                        await SaveCustomVisionModelAsync(customVisionModelList, newExportedCustomVisionModel, customVisionProjectType);

                        // re-display flyout window
                        FlyoutBase.ShowAttachedFlyout(this.exportButton);
                        ShowShareStatus(customVisionProjectType);
                    },
                    cancelAction: async () =>
                    {
                        int maxNumberOfModelWithSameName = customVisionModelList
                            .Where(x => x.Name != null && x.Name.StartsWith(newExportedCustomVisionModel.Name, StringComparison.OrdinalIgnoreCase))
                            .Select(x =>
                            {
                                string modelNumberInString = x.Name.Split('_').LastOrDefault();
                                int.TryParse(modelNumberInString, out int number);
                                return number;
                            })
                            .Max();

                        // if user select Cancel we just save the new model with the same name
                        newExportedCustomVisionModel.Name = $"{newExportedCustomVisionModel.Name}_{maxNumberOfModelWithSameName + 1}";
                        await SaveCustomVisionModelAsync(customVisionModelList, newExportedCustomVisionModel, customVisionProjectType);

                        // re-display flyout window
                        FlyoutBase.ShowAttachedFlyout(this.exportButton);
                        ShowShareStatus(customVisionProjectType);
                    },
                    confirmActionLabel: "Replace",
                    cancelActionLabel: "Keep Both");
            }
            else
            {
                await SaveCustomVisionModelAsync(customVisionModelList, newExportedCustomVisionModel, customVisionProjectType);
                ShowShareStatus(customVisionProjectType);
            }

            return success;
        }

        private async Task SaveCustomVisionModelAsync(List<CustomVisionModelData> customVisionModelList, CustomVisionModelData customVisionModelData,
            CustomVisionProjectType customVisionProjectType)
        {
            if (customVisionModelList != null)
            {
                // Update existing model, otherwise add a new one
                int index = customVisionModelList.FindIndex(x => x.Id == customVisionModelData.Id);
                if (index >= 0)
                {
                    customVisionModelList[index] = customVisionModelData;
                }
                else
                {
                    customVisionModelList.Add(customVisionModelData);
                }
                await CustomVisionDataLoader.SaveCustomVisionModelDataAsync(customVisionModelList, customVisionProjectType);
            }
        }

        private void OnNavigateToRealtimeScoringPageButtonClicked(object sender, RoutedEventArgs e)
        {
            bool isObjectDetectionModel = CustomVisionServiceHelper.ObjectDetectionDomainGuidList.Contains(this.CurrentProject.Settings.DomainId);
            Type destPage = isObjectDetectionModel ? typeof(RealtimeObjectDetection) : typeof(RealtimeImageClassification);
            AppShell.Current.NavigateToPage(destPage, newExportedCustomVisionModel);
        }

        private void ShowShareStatus(CustomVisionProjectType customVisionProjectType)
        {
            string scenarioName = customVisionProjectType == CustomVisionProjectType.Classification ? "Realtime Image Classification" : "Realtime Object Detection";
            string shareStatusText = string.Format("Your project was exported. To start using it, open the {0} demo and select this project.", scenarioName);
            string openPageText = string.Format("Click here if you would like us to switch to the {0} experience. We will open it and target the newly exported model.", scenarioName);
            this.shareStatusPanelDescription.Visibility = Visibility.Visible;
            this.shareStatusTextDescription.Text = shareStatusText;
            this.openPageTextBlock.Text = openPageText;
        }

        private void OnShareProjectFlyoutClosed(object sender, object e)
        {
            this.shareStatusTextBlock.Text = string.Empty;
            this.platformsCombo.SelectedItem = null;
            this.shareStatusPanelDescription.Visibility = Visibility.Collapsed;
            if (this.downloadCancellationTokenSource != null && this.downloadCancellationTokenSource.Token.CanBeCanceled)
            {
                this.downloadCancellationTokenSource.Cancel();
            }
        }

        private void OnProjectShareCloseButtonClicked(object sender, RoutedEventArgs e)
        {
            HideProjectSharingFlyout();
        }

        private void HideProjectSharingFlyout()
        {
            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(this.exportButton);
            if (flyout != null)
            {
                flyout.Hide();
            }
        }

        private void OnExportModelClicked(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void OnPlatformSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Platform currentPlatform = this.platformsCombo.SelectedItem as Platform;
            if (currentPlatform != null)
            {
                await ExportProject(currentPlatform);
            }
        }
    }
}