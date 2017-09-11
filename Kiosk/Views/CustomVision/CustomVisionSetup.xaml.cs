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

using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    public sealed partial class CustomVisionSetup : Page
    {
        private bool needsTraining = false;

        public ObservableCollection<ProjectModel> Projects { get; set; } = new ObservableCollection<ProjectModel>();
        public ProjectModel CurrentProject { get; set; }
        public ObservableCollection<ImageTagModel> TagsInCurrentGroup { get; set; } = new ObservableCollection<ImageTagModel>();
        public ObservableCollection<ImageModel> SelectedTagImages { get; set; } = new ObservableCollection<ImageModel>();
        public ImageTagModel SelectedTag { get; set; }

        private TrainingApi trainingApi;

        public CustomVisionSetup()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = this;
            this.settingsButton.DataContext = SettingsHelper.Instance;

            if (string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey))
            {
                await new MessageDialog("Please enter keys under the Settings button in this page.", "Missing API Keys").ShowAsync();
                this.addProjectButton.IsEnabled = false;
                this.trainButton.IsEnabled = false;
            }
            else
            {
                await InitializeTrainingApi();
            }

            base.OnNavigatedTo(e);
        }

        private async Task InitializeTrainingApi()
        {
            trainingApi = new TrainingApi(new TrainingApiCredentials(SettingsHelper.Instance.CustomVisionTrainingApiKey));
            this.addProjectButton.IsEnabled = true;
            this.trainButton.IsEnabled = true;

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
            this.progressControl.IsActive = true;

            try
            {
                this.Projects.Clear();
                IEnumerable<ProjectModel> projects = await trainingApi.GetProjectsAsync();
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

            this.progressControl.IsActive = false;
        }

        private async void OnAddProjectButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = this.projectNameTextBox.Text;
                ProjectModel model = await trainingApi.CreateProjectAsync(name);

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
            await Util.ConfirmActionAndExecute("Delete project?", async () => { await DeleteProjectAsync(); });
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
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting project");
            }
        }

        private async void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.needsTraining && (ProjectModel)this.projectsListView.SelectedValue != this.CurrentProject)
            {
                // revert
                this.projectsListView.SelectedValue = this.CurrentProject;

                await Util.ConfirmActionAndExecute("It looks like you made modifications but didn't train the model afterwards. Would you like to train it now?", async () => await this.TrainProjectsAsync());
            }
            else
            {
                this.CurrentProject = (ProjectModel)this.projectsListView.SelectedValue;

                if (this.CurrentProject != null)
                {
                    await this.LoadTagsInCurrentProject();
                }
            }
        }

        #endregion

        #region Tag management

        private async void OnTagSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.projectTagsListView.SelectedValue != null)
            {
                this.SelectedTag = this.projectTagsListView.SelectedValue as ImageTagModel;
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
                ImageTagListModel tagListModel = await trainingApi.GetTagsAsync(this.CurrentProject.Id);
                foreach (ImageTagModel tag in tagListModel.Tags.OrderBy(t => t.Name))
                {
                    this.TagsInCurrentGroup.Add(tag);
                }
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure loading people in the group");
            }
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
                ImageTagModel result = await trainingApi.CreateTagAsync(this.CurrentProject.Id, name);
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
                IEnumerable<ImageModel> images = await trainingApi.GetImagesByTagsAsync(this.CurrentProject.Id, null, new string[] { this.SelectedTag.Id.ToString() }, null, 200);
                this.SelectedTagImages.AddRange(images);
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
            this.progressControl.IsActive = true;

            this.trainingImageCollectorFlyout.Hide();

            bool foundError = false;
            Exception lastError = null;
            foreach (var item in args)
            {
                try
                {
                    CreateImageSummaryModel addResult;
                    if (item.GetImageStreamCallback != null)
                    {
                        addResult = await trainingApi.CreateImagesFromDataAsync(
                            this.CurrentProject.Id, 
                            await item.GetImageStreamCallback(), new string[] { this.SelectedTag.Id.ToString() });
                    }
                    else
                    {
                        addResult = await trainingApi.CreateImagesFromUrlsAsync(
                            this.CurrentProject.Id,
                            new ImageUrlCreateBatch(new Guid[] { this.SelectedTag.Id }, new string[] { item.ImageUrl }));
                    }

                    if (addResult != null)
                    {
                        this.SelectedTagImages.AddRange(addResult.Images.Select(r => r.Image));
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
                    ImageModel tagImage = (ImageModel)item;
                    await trainingApi.DeleteImagesAsync(this.CurrentProject.Id, new string[] { tagImage.Id.ToString() });
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
        }

        private async Task TrainProjectsAsync()
        {
            this.progressControl.IsActive = true;

            bool trainingSucceeded = true;
            try
            {
                IterationModel iterationModel = await trainingApi.TrainProjectAsync(this.CurrentProject.Id);

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
                await new MessageDialog("Training failed.").ShowAsync();
            }
        }

        #endregion

        private async void OnSettingsFlyoutClosed(object sender, object e)
        {
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey))
            {
                await InitializeTrainingApi();
            }
        }
    }
}