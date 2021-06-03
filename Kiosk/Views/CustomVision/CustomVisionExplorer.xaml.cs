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

using IntelligentKioskSample.Controls.Overlays;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using ServiceHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "CustomVisionExplorer",
        DisplayName = "Custom Vision Explorer",
        Description = "Analyze images using your own models",
        ImagePath = "ms-appx:/Assets/DemoGallery/Custom Vision Explorer.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.BingAutoSuggest | TechnologyType.BingImages | TechnologyType.CustomVision,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2017/05/11")]
    public sealed partial class CustomVisionExplorer : Page
    {
        private CustomVisionTrainingClient userProvidedTrainingApi;
        private CustomVisionPredictionClient userProvidedPredictionApi;

        public ObservableCollection<ProjectViewModel> Projects { get; set; } = new ObservableCollection<ProjectViewModel>();

        public ObservableCollection<ActiveLearningTagViewModel> PredictionDataForRetraining { get; set; } = new ObservableCollection<ActiveLearningTagViewModel>();

        public CustomVisionExplorer()
        {
            this.InitializeComponent();
        }

        private void DisplayProcessingUI()
        {
            //clear overlays
            OverlayPresenter.ObjectInfo = null;
            OverlayPresenter.MatchInfo = null;

            this.progressRing.IsActive = true;
        }

        private async void UpdateResults(ImageAnalyzer img)
        {
            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction result = null;
            var currentProjectViewModel = (ProjectViewModel)this.projectsComboBox.SelectedValue;
            var currentProject = ((ProjectViewModel)this.projectsComboBox.SelectedValue).Model;

            CustomVisionTrainingClient trainingApi = this.userProvidedTrainingApi;
            CustomVisionPredictionClient predictionApi = this.userProvidedPredictionApi;

            try
            {
                IList<Iteration> iteractions = await trainingApi.GetIterationsAsync(currentProject.Id);

                Iteration latestTrainedIteraction = iteractions.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();

                if (latestTrainedIteraction == null)
                {
                    throw new Exception("This project doesn't have any trained models yet. Please train it, or wait until training completes if one is in progress.");
                }
                
                if (string.IsNullOrEmpty(latestTrainedIteraction.PublishName))
                {
                    await trainingApi.PublishIterationAsync(currentProject.Id, latestTrainedIteraction.Id, latestTrainedIteraction.Id.ToString(), SettingsHelper.Instance.CustomVisionPredictionResourceId);
                    latestTrainedIteraction = await trainingApi.GetIterationAsync(currentProject.Id, latestTrainedIteraction.Id);
                }

                if (img.ImageUrl != null)
                {
                    result = await CustomVisionServiceHelper.ClassifyImageUrlWithRetryAsync(predictionApi, currentProject.Id, new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl(img.ImageUrl), latestTrainedIteraction.PublishName);
                }
                else
                {
                    result = await CustomVisionServiceHelper.ClassifyImageWithRetryAsync(predictionApi, currentProject.Id, img.GetImageStreamCallback, latestTrainedIteraction.PublishName);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error");
            }

            this.progressRing.IsActive = false;

            var matches = result?.Predictions?.Where(r => Math.Round(r.Probability * 100) > 0);

            if (!currentProjectViewModel.IsObjectDetection)
            {
                //show image classification matches
                OverlayPresenter.MatchInfo = new MatchOverlayInfo(matches);
            }
            else
            {
                //show detected objects
                OverlayPresenter.ObjectInfo = matches.Where(m => m.Probability >= 0.6).Select(i => new PredictedObjectOverlayInfo(i)).ToList();
            }

            if (result?.Predictions != null && !currentProjectViewModel.IsObjectDetection)
            {
                this.activeLearningButton.Opacity = 1;

                this.PredictionDataForRetraining.Clear();
                this.PredictionDataForRetraining.AddRange(result.Predictions.Select(t => new ActiveLearningTagViewModel
                {
                    PredictionResultId = result.Id,
                    TagId = t.TagId,
                    TagName = t.TagName,
                    HasTag = Math.Round(t.Probability * 100) > 0
                }));
            }
            else
            {
                this.activeLearningButton.Opacity = 0;
            }
        }

        private async Task UpdateActivePhoto(ImageAnalyzer img)
        {
            //set image source
            OverlayPresenter.Source = await img.GetImageSource();

            this.DisplayProcessingUI();
            this.UpdateResults(img);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey) &&
                !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionApiKey) &&
                !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionResourceId))
            {
                userProvidedTrainingApi = new CustomVisionTrainingClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(SettingsHelper.Instance.CustomVisionTrainingApiKey))
                {
                    Endpoint = SettingsHelper.Instance.CustomVisionTrainingApiKeyEndpoint
                };
                userProvidedPredictionApi = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(SettingsHelper.Instance.CustomVisionPredictionApiKey))
                {
                    Endpoint = SettingsHelper.Instance.CustomVisionPredictionApiKeyEndpoint
                };
            }

            this.DataContext = this;
            await this.LoadProjectsFromService();

            base.OnNavigatedTo(e);
        }

        private async Task LoadProjectsFromService()
        {
            this.progressRing.IsActive = true;

            try
            {
                this.Projects.Clear();

                // Add projects from API Keys provided by user
                if (this.userProvidedTrainingApi != null && this.userProvidedPredictionApi != null)
                {
                    IEnumerable<Project> projects = await this.userProvidedTrainingApi.GetProjectsAsync();
                    foreach (var project in projects.OrderBy(p => p.Name))
                    {
                        this.Projects.Add(
                            new ProjectViewModel
                            {
                                Model = project,
                                TagSamples = new ObservableCollection<TagSampleViewModel>(),
                                IsObjectDetection = CustomVisionServiceHelper.ObjectDetectionDomainGuidList.Contains(project.Settings.DomainId)
                            });
                    }
                }

                if (this.projectsComboBox.Items.Any())
                {
                    this.projectsComboBox.SelectedIndex = 0;
                }

                // Trigger loading of the tags associated with each project
                foreach (var project in this.Projects)
                {
                    this.PopulateTagSamplesAsync(project.Model.Id,
                                            this.userProvidedTrainingApi,
                                            project.TagSamples);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading projects");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private async void PopulateTagSamplesAsync(Guid projectId, CustomVisionTrainingClient trainingEndPoint, ObservableCollection<TagSampleViewModel> collection)
        {
            //take samples images rotating through each tag
            var maxSamples = 8;
            var tags = (await trainingEndPoint.GetTagsAsync(projectId)).OrderBy(i => i.Name).ToArray();
            //extend sample count to atleast match tag count
            if (tags.Length > maxSamples)
            {
                maxSamples = (int)Math.Ceiling(tags.Length / 4d) * 4;
            }
            var sampleTasks = tags.Select(i => trainingEndPoint.GetTaggedImagesAsync(projectId, null, new List<Guid>() { i.Id }, null, maxSamples)).ToArray(); //request sample images for each tag
            await Task.WhenAll(sampleTasks); //wait for request to finish
            //round-robin out sample images
            var roundRobin = new RoundRobinIterator<Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image>(sampleTasks.Select(i => i.Result));
            collection.AddRange(roundRobin.Distinct(new TaggedImageComparer()).Take(maxSamples).Select(i => new TagSampleViewModel { TagSampleImage = new BitmapImage(new Uri(i.OriginalImageUri)) }));

            UpdateSuggestedPhotoList();
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            ImageAnalyzer image = args.First();
            image.ShowDialogOnFaceApiErrors = true;

            await this.UpdateActivePhoto(image);
        }

        private void OnTargetProjectChanged(object sender, SelectionChangedEventArgs e)
        {
            // go back to the initial state, so the user can pick a new appropriate image for the new project
            UpdateSuggestedPhotoList();
            imagePicker.CurrentState = Controls.ImagePickerState.InputTypes;
        }

        void UpdateSuggestedPhotoList()
        {
            //update suggested photo list
            var selectedModel = projectsComboBox.SelectedValue as ProjectViewModel;
            if (selectedModel != null)
            {
                imagePicker.SetSuggestedImageList(selectedModel.TagSamples?.Select(i => i.TagSampleImage));
            }
        }

        private void EditProjectsClicked(object sender, RoutedEventArgs e)
        {
            AppShell.Current.NavigateToPage(typeof(CustomVisionSetup));
        }

        private async void TriggerActiveLearningButtonClicked(object sender, RoutedEventArgs e)
        {
            this.activeLearningFlyout.Hide();

            var currentProject = ((ProjectViewModel)this.projectsComboBox.SelectedValue).Model;

            try
            {
                var tags = this.PredictionDataForRetraining.Where(d => d.HasTag).Select(d => d.TagId).ToList();

                if (tags.Any())
                {
                    var test = await this.userProvidedTrainingApi.CreateImagesFromPredictionsAsync(currentProject.Id,
                        new ImageIdCreateBatch
                        {
                            TagIds = tags,
                            Images = new List<ImageIdCreateEntry>(new ImageIdCreateEntry[] { new ImageIdCreateEntry(this.PredictionDataForRetraining.First().PredictionResultId) })
                        });
                }
                else
                {
                    await new MessageDialog("You need to select at least one Tag in order to save and re-train.").ShowAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure adding image to the training set");
                return;
            }

            this.progressRing.IsActive = true;

            bool trainingSucceeded = true;

            try
            {
                Iteration iterationModel = await userProvidedTrainingApi.TrainProjectAsync(currentProject.Id);

                while (true)
                {
                    iterationModel = await userProvidedTrainingApi.GetIterationAsync(currentProject.Id, iterationModel.Id);

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
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "The image was added to the training set, but re-training failed. You can try re-training later via the Custom Vision Setup page.");
            }

            if (!trainingSucceeded)
            {
                await new MessageDialog("The image was added to the training set, but re-training failed. You can try re-training later via the Custom Vision Setup page.").ShowAsync();
            }

            this.progressRing.IsActive = false;
        }
    }

    class RoundRobinIterator<T> : IEnumerable<T>
    {
        IEnumerable<IEnumerable<T>> _iterators;

        public RoundRobinIterator(IEnumerable<IEnumerable<T>> iterators)
        {
            //set fields
            _iterators = iterators;
        }

        public IEnumerator<T> GetEnumerator()
        {
            //validate
            if (_iterators == null)
            {
                yield break;
            }

            //get enumerators
            var enumerators = _iterators.Select(i => i.GetEnumerator()).ToArray();

            //round robin until no results are left
            var hasResult = false;
            do
            {
                foreach (var enumerator in enumerators)
                {
                    hasResult = enumerator.MoveNext();
                    if (hasResult)
                    {
                        yield return enumerator.Current;
                    }
                }
            } while (hasResult);
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class TaggedImageComparer : IEqualityComparer<Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image>
    {
        public bool Equals(Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image x, Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image y)
        {
            return x.OriginalImageUri == y.OriginalImageUri;
        }

        public int GetHashCode(Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Image obj)
        {
            return obj.OriginalImageUri.GetHashCode();
        }
    }

    public class ProjectViewModel
    {
        public bool IsObjectDetection { get; set; }
        public Project Model { get; set; }
        public ObservableCollection<TagSampleViewModel> TagSamples { get; set; }
    }

    public class TagSampleViewModel
    {
        public string TagName { get; set; }
        public ImageSource TagSampleImage { get; set; }
    }

    public class ActiveLearningTagViewModel
    {
        public Guid PredictionResultId { get; set; }
        public Guid TagId { get; set; }
        public string TagName { get; set; }
        public bool HasTag { get; set; }
    }
}
