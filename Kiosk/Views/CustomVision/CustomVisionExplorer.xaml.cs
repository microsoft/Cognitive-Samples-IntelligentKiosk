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
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Title = "Custom Vision Explorer", ImagePath = "ms-appx:/Assets/CustomVisionExplorer.png")]
    public sealed partial class CustomVisionExplorer : Page
    {
        private ImageAnalyzer currentPhoto;

        private TrainingApi userProvidedTrainingApi;
        private PredictionEndpoint userProvidedPredictionApi;

        public ObservableCollection<ProjectViewModel> Projects { get; set; } = new ObservableCollection<ProjectViewModel>();

        public ObservableCollection<ActiveLearningTagViewModel> PredictionDataForRetraining { get; set; } = new ObservableCollection<ActiveLearningTagViewModel>();

        private IEnumerable<PredictionModel> currentDetectedObjects;

        public CustomVisionExplorer()
        {
            this.InitializeComponent();

            this.cameraControl.ImageCaptured += CameraControl_ImageCaptured;
            this.cameraControl.CameraRestarted += CameraControl_CameraRestarted;
        }

        private async void CameraControl_CameraRestarted(object sender, EventArgs e)
        {
            // We induce a delay here to give the camera some time to start rendering before we hide the last captured photo.
            // This avoids a black flash.
            await Task.Delay(500);

            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;
            this.resultsDetails.Visibility = Visibility.Collapsed;
            this.objectDetectionVisualizationCanvas.Children.Clear();
            this.currentDetectedObjects = null;
        }

        private void DisplayProcessingUI()
        {
            this.resultsDetails.Visibility = Visibility.Collapsed;
            this.resultsGridView.ItemsSource = null;

            this.objectDetectionVisualizationCanvas.Children.Clear();
            this.currentDetectedObjects = null;

            this.progressRing.IsActive = true;
        }

        private async void UpdateResults(ImageAnalyzer img)
        {
            this.searchErrorTextBlock.Visibility = Visibility.Collapsed;

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction result = null;
            var currentProjectViewModel = (ProjectViewModel)this.projectsComboBox.SelectedValue;
            var currentProject = ((ProjectViewModel)this.projectsComboBox.SelectedValue).Model;

            var trainingApi = this.userProvidedTrainingApi;
            var predictionApi = this.userProvidedPredictionApi;

            try
            {
                var iteractions = await trainingApi.GetIterationsAsync(currentProject.Id);

                var latestTrainedIteraction = iteractions.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();

                if (latestTrainedIteraction == null)
                {
                    throw new Exception("This project doesn't have any trained models yet. Please train it, or wait until training completes if one is in progress.");
                }

                if (img.ImageUrl != null)
                {
                    result = await CustomVisionServiceHelper.PredictImageUrlWithRetryAsync(predictionApi, currentProject.Id, new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl(img.ImageUrl), latestTrainedIteraction.Id);
                }
                else
                {
                    result = await CustomVisionServiceHelper.PredictImageWithRetryAsync(predictionApi, currentProject.Id, img.GetImageStreamCallback, latestTrainedIteraction.Id);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error");
            }

            this.progressRing.IsActive = false;
            this.resultsDetails.Visibility = Visibility.Visible;

            var matches = result?.Predictions?.Where(r => Math.Round(r.Probability * 100) > 0);

            if (matches == null || !matches.Any())
            {
                this.searchErrorTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                if (!currentProjectViewModel.IsObjectDetection)
                {
                    this.resultsGridView.ItemsSource = matches.Select(t => new { Tag = t.TagName, Probability = string.Format("{0}%", Math.Round(t.Probability * 100)) });
                }
                else
                {
                    this.resultsDetails.Visibility = Visibility.Collapsed;
                    this.currentDetectedObjects = matches.Where(m => m.Probability >= 0.6);
                    ShowObjectDetectionBoxes(this.currentDetectedObjects);
                }
            }

            if (result?.Predictions != null && !currentProjectViewModel.IsObjectDetection)
            {
                this.activeLearningButton.Opacity = 1;

                this.PredictionDataForRetraining.Clear();
                this.PredictionDataForRetraining.AddRange(result.Predictions.Select(
                    t => new ActiveLearningTagViewModel
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

        private void ShowObjectDetectionBoxes(IEnumerable<PredictionModel> detectedObjects)
        {
            this.objectDetectionVisualizationCanvas.Children.Clear();

            double canvasWidth = objectDetectionVisualizationCanvas.ActualWidth;
            double canvasHeight = objectDetectionVisualizationCanvas.ActualHeight;

            foreach (PredictionModel prediction in detectedObjects)
            {
                objectDetectionVisualizationCanvas.Children.Add(
                    new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Lime),
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(prediction.BoundingBox.Left * canvasWidth,
                                               prediction.BoundingBox.Top * canvasHeight, 0, 0),
                        Width = prediction.BoundingBox.Width * canvasWidth,
                        Height = prediction.BoundingBox.Height * canvasHeight,
                    });

                objectDetectionVisualizationCanvas.Children.Add(
                    new Border
                    {
                        Height = 40,
                        FlowDirection = FlowDirection.LeftToRight,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(prediction.BoundingBox.Left * canvasWidth,
                                               prediction.BoundingBox.Top * canvasHeight - 40, 0, 0),

                        Child = new Border
                        {
                            Background = new SolidColorBrush(Colors.Lime),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Child =
                                new TextBlock
                                {
                                    Foreground = new SolidColorBrush(Colors.Black),
                                    Text = $"{prediction.TagName} ({Math.Round(prediction.Probability * 100)}%)",
                                    FontSize = 16,
                                    Margin = new Thickness(6, 0, 6, 0)
                                }
                        }
                    });
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
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey) && 
                !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionApiKey))
            {
                userProvidedTrainingApi = new TrainingApi { BaseUri = new Uri("https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Training"), ApiKey = SettingsHelper.Instance.CustomVisionTrainingApiKey };
                userProvidedPredictionApi = new PredictionEndpoint { BaseUri = new Uri("https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction"), ApiKey = SettingsHelper.Instance.CustomVisionPredictionApiKey };
            }

            this.DataContext = this;
            await this.LoadProjectsFromService();

            if (!this.Projects.Any())
            {
                await new MessageDialog("It looks like you don't have any projects yet. Please create a project via the '+' button near the Target Project list in this page.", "No projects found").ShowAsync();
                this.webCamButton.IsEnabled = false;
                this.PicturesAppBarButton.IsEnabled = false;
            }

            base.OnNavigatedTo(e);
        }

        private async Task LoadProjectsFromService()
        {
            this.progressRing.IsActive = true;

            try
            {
                this.Projects.Clear();
                Guid objectDetectionDomainGuid = new Guid("da2e3a8a-40a5-4171-82f4-58522f70fbc1");

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
                                IsObjectDetection = project.Settings.DomainId == objectDetectionDomainGuid
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

        private async void PopulateTagSamplesAsync(Guid projectId, TrainingApi trainingEndPoint, ObservableCollection<TagSampleViewModel> collection)
        {
            foreach (var tag in (await trainingEndPoint.GetTagsAsync(projectId)).OrderBy(t => t.Name))
            {
                if (tag.ImageCount > 0)
                {
                    var imageModelSample = (await trainingEndPoint.GetTaggedImagesAsync(projectId, null, new string[] { tag.Id.ToString() }, null, 1)).First();

                    var tagRegion = imageModelSample.Regions?.FirstOrDefault(r => r.TagId == tag.Id);
                    if (tagRegion == null || (tagRegion.Width == 0 && tagRegion.Height == 0))
                    {
                        collection.Add(new TagSampleViewModel { TagName = tag.Name, TagSampleImage = new BitmapImage(new Uri(imageModelSample.ThumbnailUri)) });
                    }
                    else
                    {
                        // Crop a region from the image that is associated with the tag, so we show something more 
                        // relevant than the whole image. 
                        ImageSource croppedImage = await Util.DownloadAndCropBitmapAsync(
                            imageModelSample.ImageUri,
                            new Microsoft.ProjectOxford.Face.Contract.FaceRectangle
                            {
                                Left = (int)(tagRegion.Left * imageModelSample.Width),
                                Top = (int)(tagRegion.Top * imageModelSample.Height),
                                Width = (int)(tagRegion.Width * imageModelSample.Width),
                                Height = (int)(tagRegion.Height * imageModelSample.Height)
                            });

                        collection.Add(new TagSampleViewModel { TagName = tag.Name, TagSampleImage = croppedImage });
                    }
                }
            }
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
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
            this.webCamHostGrid.Width = this.webCamHostGrid.ActualHeight * (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }

        private void OnResultTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.currentPhoto != null)
            {
                this.UpdateActivePhoto(this.currentPhoto);
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

        private void OnObjectDetectionVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentDetectedObjects != null)
            {
                this.ShowObjectDetectionBoxes(this.currentDetectedObjects);
            }
        }
    }

    public class ProjectViewModel
    {
        public Project Model { get; set; }
        public bool IsObjectDetection { get; set; }
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
