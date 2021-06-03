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

using IntelligentKioskSample.Controls;
using IntelligentKioskSample.Views.CustomVision;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.VisualAlert
{
    [KioskExperience(Id = "VisualAlertBuilder",
        DisplayName = "Visual Alert Builder",
        Description = "See how Custom Vision enables you to train and trigger visual alerts",
        ImagePath = "ms-appx:/Assets/DemoGallery/VisualAlertBuilder.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologyArea = TechnologyAreaType.Vision,
        TechnologiesUsed = TechnologyType.CustomVision,
        DateAdded = "2019/10/03")]
    public sealed partial class VisualAlertBuilderPage : Page, ICameraFrameProcessor
    {
        private const float MinProbabilityValue = 0.3f;
        private CustomVisionModel customVisionONNXModel;
        private CustomVisionServiceWrapper customVisionServiceWrapper;
        private VisualAlertScenarioData prevScenario;

        public ObservableCollection<VisualAlertScenarioData> ScenarioCollection { get; set; } = new ObservableCollection<VisualAlertScenarioData>();
        public ObservableCollection<LifecycleStepViewModel> LifecycleStepCollection { get; set; } = new ObservableCollection<LifecycleStepViewModel>(new List<LifecycleStepViewModel>()
        {
            new LifecycleStepViewModel()
            {
                Id = VisualAlertBuilderStepType.AddPositiveImages.ToString(),
                Title = "Add images with subject",
                State = LifecycleStepState.Mute
            },
            new LifecycleStepViewModel()
            {
                Id = VisualAlertBuilderStepType.AddNegativeImages.ToString(),
                Title = "Add images without subject",
                State = LifecycleStepState.Mute
            },
            new LifecycleStepViewModel()
            {
                Id = VisualAlertBuilderStepType.TrainModel.ToString(),
                Title = "Train model",
                State = LifecycleStepState.Mute,
                IsLast = true
            }
        });

        public VisualAlertBuilderPage()
        {
            this.InitializeComponent();
            this.DataContext = this;

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.CameraFrameProcessor = this;
            this.cameraControl.PerformFaceTracking = false;
            this.cameraControl.ShowFaceTracking = false;
            this.cameraControl.EnableCameraControls = false;
            this.cameraControl.CameraAspectRatioChanged += CameraAspectRatioChanged;
        }

        private void CameraAspectRatioChanged(object sender, EventArgs e)
        {
            this.UpdateCameraHostSize();
        }

        private async void CurrentWindowActivationStateChanged(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if ((e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated ||
                e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.PointerActivated) &&
                this.cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Shutdown)
            {
                // When our Window loses focus due to user interaction Windows shuts it down, so we 
                // detect here when the window regains focus and trigger a restart of the camera.
                await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey) || string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionApiKey))
            {
                this.mainPage.IsEnabled = false;
                await new MessageDialog("Please enter Custom Vision API Keys in the Settings Page.", "Missing API Keys").ShowAsync();
            }
            else
            {
                this.mainPage.IsEnabled = true;

                customVisionServiceWrapper = new CustomVisionServiceWrapper(SettingsHelper.Instance.CustomVisionTrainingApiKey, SettingsHelper.Instance.CustomVisionTrainingApiKeyEndpoint);

                await LoadScenariosAsync();
            }

            UpdateScenarioListPanel(BuilderMode.ScenarioList);
            await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
            this.cameraControl.CameraAspectRatioChanged -= CameraAspectRatioChanged;

            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        private async Task LoadScenariosAsync()
        {
            try
            {
                IList<VisualAlertScenarioData> scenarioList = await VisualAlertDataLoader.GetScenarioCollectionAsync();
                ScenarioCollection.Clear();
                ScenarioCollection.AddRange(scenarioList);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading alerts");
            }
        }

        private void UpdateScenarioListPanel(BuilderMode mode, VisualAlertScenarioData defaultScenario = null)
        {
            bool isAnyScenario = this.scenarioListView.Items.Any();
            switch (mode)
            {
                case BuilderMode.ScenarioList:
                    this.scenarioListView.SelectionMode = ListViewSelectionMode.Single;
                    if (isAnyScenario)
                    {
                        var selectedScenario = ScenarioCollection.FirstOrDefault(x => x.Id == defaultScenario?.Id);
                        if (selectedScenario != null)
                        {
                            this.scenarioListView.SelectedItem = selectedScenario;
                        }
                        else
                        {
                            this.scenarioListView.SelectedIndex = 0;
                        }
                    }

                    this.newAlertGrid.Visibility = isAnyScenario ? Visibility.Collapsed : Visibility.Visible;
                    this.visualAlertBuilderWizardControl.Visibility = isAnyScenario ? Visibility.Collapsed : Visibility.Visible;

                    this.scenarioListPanel.Visibility = isAnyScenario ? Visibility.Visible : Visibility.Collapsed;
                    this.deleteButton.Visibility = Visibility.Collapsed;

                    this.newAlertStatusGrid.Visibility = Visibility.Collapsed;
                    this.newAlertButton.IsEnabled = true;
                    break;

                case BuilderMode.NewAlert:
                    this.newAlertGrid.Visibility = Visibility.Visible;
                    this.visualAlertBuilderWizardControl.Visibility = Visibility.Visible;

                    this.scenarioListPanel.Visibility = Visibility.Collapsed;
                    break;

                case BuilderMode.Processing:
                    this.newAlertGrid.Visibility = Visibility.Collapsed;
                    this.visualAlertBuilderWizardControl.Visibility = Visibility.Collapsed;

                    this.scenarioListPanel.Visibility = Visibility.Visible;
                    this.newAlertStatusGrid.Visibility = Visibility.Visible;
                    this.newAlertButton.IsEnabled = false;
                    break;
            }

            if (mode == BuilderMode.NewAlert || !isAnyScenario)
            {
                this.scenarioListView.SelectedItem = null;

                this.lifecycleControl.ResetState(clearAll: true);
                LifecycleStepCollection.First().State = LifecycleStepState.Active;
                this.visualAlertBuilderWizardControl.StartWizard();
            }
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCameraHostSize();
        }

        private void UpdateCameraHostSize()
        {
            double aspectRatio = this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777;

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

            // update wizard / result-grid width
            this.resultGrid.Width = this.webCamHostGrid.Width;
            this.visualAlertBuilderWizardControl.Width = this.webCamHostGrid.Width;
        }

        private void OnCameraPhotoCaptured(object sender, ImageAnalyzer img)
        {
            this.visualAlertBuilderWizardControl.AddNewImage(img);
        }

        private void OnCameraContinuousPhotoCaptured(object sender, ContinuousCaptureData data)
        {
            this.cameraGuideCountdownHost.Visibility = Visibility.Collapsed;
            switch (data.State)
            {
                case ContinuousCaptureState.ShowingCountdownForCapture:
                    this.cameraGuideCountdownHost.Visibility = Visibility.Visible;
                    this.countDownTextBlock.Text = $"{data.CountdownValue}";
                    break;

                case ContinuousCaptureState.Processing:
                    this.visualAlertBuilderWizardControl.AddNewImage(data.Image);
                    break;

                case ContinuousCaptureState.Completed:
                default:
                    break;
            }
        }

        private void OnBuilderWizardControlStepChanged(object sender, Tuple<VisualAlertBuilderStepType, VisualAlertModelData> data)
        {
            if (data == null)
            {
                return;
            }

            LifecycleStepViewModel addPosImagesStep = LifecycleStepCollection.FirstOrDefault(x => x.Id == VisualAlertBuilderStepType.AddPositiveImages.ToString());
            LifecycleStepViewModel addNegImagesStep = LifecycleStepCollection.FirstOrDefault(x => x.Id == VisualAlertBuilderStepType.AddNegativeImages.ToString());
            LifecycleStepViewModel trainModelStep = LifecycleStepCollection.FirstOrDefault(x => x.Id == VisualAlertBuilderStepType.TrainModel.ToString());

            this.lifecycleControl.ResetState();
            ToggleCameraControls(enable: false);
            switch (data.Item1)
            {
                case VisualAlertBuilderStepType.NewSubject:
                    addPosImagesStep.State = LifecycleStepState.Active;
                    break;

                case VisualAlertBuilderStepType.AddPositiveImages:
                    addPosImagesStep.State = LifecycleStepState.Active;
                    ToggleCameraControls(enable: true);
                    break;

                case VisualAlertBuilderStepType.AddNegativeImages:
                    addPosImagesStep.Subtitle = $"{data.Item2.Name} - {data.Item2.PositiveImages.Count()} images";
                    addPosImagesStep.State = LifecycleStepState.Completed;
                    addNegImagesStep.State = LifecycleStepState.Active;
                    ToggleCameraControls(enable: true);
                    break;

                case VisualAlertBuilderStepType.TrainModel:
                    addNegImagesStep.Subtitle = $"{data.Item2.NegativeImages.Count()} images";
                    addPosImagesStep.State = LifecycleStepState.Completed;
                    addNegImagesStep.State = LifecycleStepState.Completed;
                    trainModelStep.State = LifecycleStepState.Active;
                    break;
            }
        }

        private async void OnBuilderWizardControlCompleted(object sender, VisualAlertModelData data)
        {
            await TrainAndSaveNewScenarioAsync(data);
        }

        private async Task TrainAndSaveNewScenarioAsync(VisualAlertModelData data)
        {
            Project project = null;
            VisualAlertScenarioData scenario = null;
            try
            {
                this.newAlertProgressBar.IsIndeterminate = true;
                UpdateScenarioListPanel(BuilderMode.Processing);
                await UpdateStatus(string.Empty);

                // create new custom vision project
                UpdateProcessingStatus(data.Name, AlertCreateProcessingStatus.Creating);
                project = await customVisionServiceWrapper.CreateVisualAlertProjectAsync(data.Name, data.PositiveImages, data.NegativeImages);

                // train project
                UpdateProcessingStatus(data.Name, AlertCreateProcessingStatus.Training);
                Iteration iteration = await customVisionServiceWrapper.TrainProjectAsync(project.Id);

                // export project
                UpdateProcessingStatus(data.Name, AlertCreateProcessingStatus.Exporting);
                scenario = await customVisionServiceWrapper.ExportOnnxProject(project);

                // store project
                await VisualAlertDataLoader.StoreScenarioAsync(scenario);

                // update scenario collection
                await LoadScenariosAsync();
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure creating alert");
            }
            finally
            {
                if (project != null)
                {
                    await customVisionServiceWrapper.DeleteProjectAsync(project);
                }

                this.newAlertProgressBar.IsIndeterminate = false;
                UpdateScenarioListPanel(BuilderMode.ScenarioList, scenario);
            }
        }

        private async void OnScenarioListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.scenarioListView.SelectionMode)
            {
                case ListViewSelectionMode.Multiple:
                    var selectedScenarios = this.scenarioListView.SelectedItems.Cast<VisualAlertScenarioData>().ToArray();
                    this.deleteButton.IsEnabled = selectedScenarios != null && selectedScenarios.Any();
                    break;

                case ListViewSelectionMode.Single:
                default:
                    this.customVisionONNXModel = null;
                    if (this.scenarioListView.SelectedValue is VisualAlertScenarioData project)
                    {
                        await LoadCurrentScenarioAsync(project);
                    }
                    break;
            }
        }

        private async Task LoadCurrentScenarioAsync(VisualAlertScenarioData scenario)
        {
            try
            {
                StorageFolder onnxProjectDataFolder = await VisualAlertDataLoader.GetOnnxModelStorageFolderAsync();
                StorageFile scenarioFile = await onnxProjectDataFolder.GetFileAsync(scenario.FileName);
                this.customVisionONNXModel = await CustomVisionModel.CreateONNXModel(scenarioFile);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading current project");
            }
        }

        public async Task ProcessFrame(VideoFrame videoFrame, Canvas visualizationCanvas)
        {
            if (customVisionONNXModel == null || videoFrame == null)
            {
                return;
            }

            try
            {
                using (SoftwareBitmap bitmapBuffer = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                    customVisionONNXModel.InputImageWidth, customVisionONNXModel.InputImageHeight, BitmapAlphaMode.Ignore))
                {
                    using (VideoFrame buffer = VideoFrame.CreateWithSoftwareBitmap(bitmapBuffer))
                    {
                        await videoFrame.CopyToAsync(buffer);

                        var input = new CustomVisionModelInput() { data = buffer };

                        DateTime start = DateTime.Now;

                        // Prediction process with ONNX model
                        CustomVisionModelOutput output = await this.customVisionONNXModel.EvaluateAsync(input);

                        await ShowPredictionResults(output, Math.Round((DateTime.Now - start).TotalMilliseconds));
                    }
                }
            }
            catch (Exception ex)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (SettingsHelper.Instance.ShowDebugInfo)
                    {
                        await Util.GenericApiCallExceptionHandler(ex, "Failure processing frame");
                    }
                });
            }
        }

        private async Task ShowPredictionResults(CustomVisionModelOutput output, double latency)
        {
            string fpsValue = latency > 0 ? $"{Math.Round(1000 / latency)} fps" : string.Empty;

            List<Tuple<string, float>> result = output.GetPredictionResult();
            Tuple<string, float> topMatch = result?.Where(x => x.Item2 > MinProbabilityValue)?.OrderByDescending(x => x.Item2).FirstOrDefault();

            // Update tags in the result panel
            if (topMatch != null && topMatch.Item1 != CustomVisionServiceWrapper.NegativeTag)
            {
                await UpdateStatus(topMatch.Item1, Math.Round(topMatch.Item2 * 100), fpsValue);
            }
            else
            {
                await UpdateStatus("Nothing detected", details: fpsValue);
            }
        }

        private async Task UpdateStatus(string status, double probability = 0, string details = "")
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var highlightColor = new SolidColorBrush(Colors.White);
                var normalColor = new SolidColorBrush(Color.FromArgb(153, 255, 255, 255));

                this.alertTextBlock.Text = status;
                this.alertTextBlock.Foreground = probability > 0 ? highlightColor : normalColor;
                this.alertProbability.Text = probability > 0 ? $"({probability}%)" : string.Empty;

                this.alertIcon.Visibility = probability > 0 ? Visibility.Visible : Visibility.Collapsed;
                this.alertProbability.Visibility = probability > 0 ? Visibility.Visible : Visibility.Collapsed;

                this.fpsTextBlock.Text = details;
            });
        }

        private void UpdateProcessingStatus(string alert, AlertCreateProcessingStatus status)
        {
            this.alertNameTextBlock.Text = alert;
            this.alertStatusTextBlock.Text = $" ({status.ToString()})";
        }

        private void ToggleCameraControls(bool enable)
        {
            this.cameraControl.EnableContinuousMode = enable;
            this.cameraControl.EnableCameraControls = enable;
        }

        private void OnEditScenarioListButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (this.scenarioListView.SelectionMode)
            {
                case ListViewSelectionMode.Single:
                    this.customVisionONNXModel = null;
                    prevScenario = this.scenarioListView.SelectedItem as VisualAlertScenarioData;
                    this.deleteButton.Visibility = Visibility.Visible;
                    this.scenarioListView.SelectionMode = ListViewSelectionMode.Multiple;
                    break;

                case ListViewSelectionMode.Multiple:
                default:
                    this.scenarioListView.SelectionMode = ListViewSelectionMode.Single;
                    this.deleteButton.Visibility = Visibility.Collapsed;
                    if (prevScenario != null)
                    {
                        this.scenarioListView.SelectedItem = prevScenario;
                    }
                    else
                    {
                        this.scenarioListView.SelectedIndex = 0;
                    }
                    break;
            }
        }

        private async void OnDeleteScenariosButtonClicked(object sender, RoutedEventArgs e)
        {
            var selectedScenarios = this.scenarioListView.SelectedItems.Cast<VisualAlertScenarioData>().ToList();
            if (selectedScenarios != null && selectedScenarios.Any())
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Delete selected alert(s) permanently?",
                    Content = "This operation will delete the selected alert(s) permanently.\nAre you sure you want to continue?",
                    PrimaryButtonText = "Delete",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Secondary
                };

                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await DeleteScenariosAsync(selectedScenarios);
                }
            }
        }

        private async Task DeleteScenariosAsync(IList<VisualAlertScenarioData> scenarios)
        {
            try
            {
                this.progressRing.IsActive = true;

                this.customVisionONNXModel = null;
                await VisualAlertDataLoader.DeleteScenariosAsync(scenarios);

                // update scenario collection
                await LoadScenariosAsync();
                UpdateScenarioListPanel(BuilderMode.ScenarioList);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting scenario(s)");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private void OnCancelNewAlert(object sender, RoutedEventArgs e)
        {
            ToggleCameraControls(enable: false);
            UpdateScenarioListPanel(BuilderMode.ScenarioList);
        }

        private void OnNewAlertButtonClicked(object sender, RoutedEventArgs e)
        {
            UpdateScenarioListPanel(BuilderMode.NewAlert);
        }
    }

    public enum BuilderMode
    {
        NewAlert,
        Processing,
        ScenarioList
    }

    public enum AlertCreateProcessingStatus
    {
        Creating,
        Training,
        Exporting
    }
}
