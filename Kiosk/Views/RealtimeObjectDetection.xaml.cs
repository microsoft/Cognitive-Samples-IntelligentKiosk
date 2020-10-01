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
using IntelligentKioskSample.Models;
using IntelligentKioskSample.Views.CustomVision;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "OnDeviceObjectDetection",
        DisplayName = "Realtime Object Detection",
        Description = "Detect objects in real-time using Windows ML",
        ImagePath = "ms-appx:/Assets/DemoGallery/Realtime Object Detection.jpg",
        ExperienceType = ExperienceType.Automated | ExperienceType.Business | ExperienceType.IntelligentEdge,
        TechnologiesUsed = TechnologyType.WinML,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2018/03/14")]
    public sealed partial class RealtimeObjectDetection : Page, ICameraFrameProcessor
    {
        private readonly int ObjectDetectionModelInputSize = 416;
        private readonly float MinProbabilityValue = 0.6f;

        private string[] allModelObjects;
        private ObjectDetection objectDetectionModel;
        private bool isModelLoadedSuccessfully = false;

        public ObservableCollection<CustomVisionModelData> Projects { get; set; } = new ObservableCollection<CustomVisionModelData>();

        public RealtimeObjectDetection()
        {
            this.InitializeComponent();

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.HideCameraControls();
            this.cameraControl.CameraFrameProcessor = this;
            this.cameraControl.PerformFaceTracking = false;
            this.cameraControl.ShowFaceTracking = false;
            this.cameraControl.CameraAspectRatioChanged += CameraControl_CameraAspectRatioChanged;
        }

        private void CameraControl_CameraAspectRatioChanged(object sender, EventArgs e)
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
            this.DataContext = this;
            EnterKioskMode();

            await this.LoadProjectsFromFile(e.Parameter as CustomVisionModelData);
            await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);

            base.OnNavigatedTo(e);
        }

        private void EnterKioskMode()
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
            this.cameraControl.CameraAspectRatioChanged -= CameraControl_CameraAspectRatioChanged;

            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        private async Task LoadProjectsFromFile(CustomVisionModelData preselectedProject = null)
        {
            try
            {
                this.Projects.Clear();
                List<CustomVisionModelData> prebuiltModelList = CustomVisionDataLoader.GetBuiltInModelData(CustomVisionProjectType.ObjectDetection) ?? new List<CustomVisionModelData>();
                foreach (CustomVisionModelData prebuiltModel in prebuiltModelList)
                {
                    this.Projects.Add(prebuiltModel);
                }

                List<CustomVisionModelData> customVisionModelList = await CustomVisionDataLoader.GetCustomVisionModelDataAsync(CustomVisionProjectType.ObjectDetection) ?? new List<CustomVisionModelData>();
                foreach (CustomVisionModelData customModel in customVisionModelList)
                {
                    this.Projects.Add(customModel);
                }

                CustomVisionModelData defaultProject = preselectedProject != null ? this.Projects.FirstOrDefault(x => x.Id.Equals(preselectedProject.Id)) : null;
                if (defaultProject != null)
                {
                    this.projectsComboBox.SelectedValue = defaultProject;
                }
                else
                {
                    this.projectsComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading projects");
            }
        }

        private void LoadSupportedClasses(CustomVisionModelData currentProject)
        {
            string modelLabelsPrefix = "Supported classes: ";
            this.modelObjects.Text = string.Empty;
            this.moreModelObjects.Text = string.Empty;

            this.allModelObjects = currentProject?.ClassLabels?.OrderBy(x => x).ToArray() ?? new string[] { };
            if (this.allModelObjects.Length > 2)
            {
                this.modelObjects.Text = $"{modelLabelsPrefix}{string.Join(", ", this.allModelObjects.Take(2))}";
                this.moreModelObjects.Text = $"and {allModelObjects.Length - 2} more"; ;
            }
            else
            {
                this.modelObjects.Text = modelLabelsPrefix + string.Join(", ", allModelObjects);
            }
        }

        private void OnShowAllSupportedClassesTapped(object sender, TappedRoutedEventArgs e)
        {
            this.allSupportedClassesListView.ItemsSource = new ObservableCollection<string>(this.allModelObjects);
            supportedClassesBox.ShowAt((TextBlock)sender);
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateCameraHostSize();
        }

        private void UpdateCameraHostSize()
        {
            this.cameraHostGrid.Width = this.cameraHostGrid.ActualHeight;
        }

        private async void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.isModelLoadedSuccessfully = false;
                if (this.projectsComboBox.SelectedValue is CustomVisionModelData currentProject)
                {
                    await LoadCurrentModelAsync(currentProject);
                }
            }
            finally
            {
                this.isModelLoadedSuccessfully = true;
            }
        }

        private async Task LoadCurrentModelAsync(CustomVisionModelData currentProject)
        {
            try
            {
                this.deleteBtn.Visibility = currentProject.IsPrebuiltModel ? Visibility.Collapsed : Visibility.Visible;
                LoadSupportedClasses(currentProject);
                StorageFile modelFile = await GetModelFileAsync(currentProject);
                this.objectDetectionModel = new ObjectDetection(this.allModelObjects, probabilityThreshold: MinProbabilityValue);
                await this.objectDetectionModel.Init(modelFile);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading current project");
            }
        }

        private async Task<StorageFile> GetModelFileAsync(CustomVisionModelData customVisionModelData)
        {
            if (customVisionModelData.IsPrebuiltModel)
            {
                string modelPath = $"Assets\\{customVisionModelData.FileName}";
                return await Package.Current.InstalledLocation.GetFileAsync(modelPath);
            }
            else
            {
                StorageFolder onnxProjectDataFolder = await CustomVisionDataLoader.GetOnnxModelStorageFolderAsync(CustomVisionProjectType.ObjectDetection);
                return await onnxProjectDataFolder.GetFileAsync(customVisionModelData.FileName);
            }
        }

        private async void OnAddProjectClicked(object sender, RoutedEventArgs e)
        {
            await new MessageDialog("To add a new project here, please select one of your projects in the Custom Vision Setup page and use the ONNX Export feature.", "New project").ShowAsync();
        }

        private async void OnDeleteProjectClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete project?", async () => { await DeleteProjectAsync(); });
        }

        private async Task DeleteProjectAsync()
        {
            CustomVisionModelData currentProject = this.projectsComboBox.SelectedValue as CustomVisionModelData;
            if (currentProject != null && !currentProject.IsPrebuiltModel)
            {
                // delete ONNX model file
                StorageFolder onnxProjectDataFolder = await CustomVisionDataLoader.GetOnnxModelStorageFolderAsync(CustomVisionProjectType.ObjectDetection);
                StorageFile modelFile = await onnxProjectDataFolder.GetFileAsync(currentProject.FileName);
                if (modelFile != null)
                {
                    await modelFile.DeleteAsync();
                }

                // update local file with custom models
                this.Projects.Remove(currentProject);
                List<CustomVisionModelData> updatedCustomModelList = this.Projects.Where(x => !x.IsPrebuiltModel).ToList();
                await CustomVisionDataLoader.SaveCustomVisionModelDataAsync(updatedCustomModelList, CustomVisionProjectType.ObjectDetection);

                this.projectsComboBox.SelectedIndex = 0;
            }
        }

        public async Task ProcessFrame(VideoFrame videoFrame, Canvas visualizationCanvas)
        {
            if (!isModelLoadedSuccessfully)
            {
                return;
            }

            try
            {
                using (SoftwareBitmap bitmapBuffer = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                    ObjectDetectionModelInputSize, ObjectDetectionModelInputSize, BitmapAlphaMode.Ignore))
                {
                    using (VideoFrame buffer = VideoFrame.CreateWithSoftwareBitmap(bitmapBuffer))
                    {
                        await videoFrame.CopyToAsync(buffer);

                        DateTime start = DateTime.Now;

                        IList<PredictionModel> predictions = await this.objectDetectionModel.PredictImageAsync(buffer);

                        double predictionTimeInMilliseconds = (DateTime.Now - start).TotalMilliseconds;

                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            this.ShowVisualization(visualizationCanvas, predictions);
                            this.fpsTextBlock.Text = predictionTimeInMilliseconds > 0 ? $"{Math.Round(1000 / predictionTimeInMilliseconds)} fps" : string.Empty;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                this.isModelLoadedSuccessfully = false;
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await Util.GenericApiCallExceptionHandler(ex, "Failure processing frame");
                });
            }
        }

        private void ShowVisualization(Canvas visualizationCanvas, IList<PredictionModel> detectedObjects)
        {
            visualizationCanvas.Children.Clear();

            double canvasWidth = visualizationCanvas.ActualWidth;
            double canvasHeight = visualizationCanvas.ActualHeight;

            foreach (PredictionModel prediction in detectedObjects)
            {
                visualizationCanvas.Children.Add(
                    new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Lime),
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(prediction.BoundingBox.Left * canvasWidth,
                                           prediction.BoundingBox.Top * canvasHeight, 0, 0),
                        Width = prediction.BoundingBox.Width * canvasWidth,
                        Height = prediction.BoundingBox.Height * canvasHeight,
                    });

                visualizationCanvas.Children.Add(
                    new Border
                    {
                        Width = 300,
                        Height = 40,
                        FlowDirection = FlowDirection.LeftToRight,
                        Margin = new Thickness(prediction.BoundingBox.Left * canvasWidth + prediction.BoundingBox.Width * canvasWidth - 300,
                                               prediction.BoundingBox.Top * canvasHeight - 40, 0, 0),

                        Child = new Border
                        {
                            Background = new SolidColorBrush(Colors.Lime),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Child =
                                new TextBlock
                                {
                                    Text = $"{prediction.TagName}",
                                    FontSize = 24,
                                    Foreground = new SolidColorBrush(Colors.Black),
                                    Margin = new Thickness(6, 0, 6, 0)
                                }
                        }
                    });
            }
        }
    }
}
