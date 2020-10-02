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
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "RealtimeImageClassification",
        DisplayName = "Realtime Image Classification",
        Description = "Experience image classification in real-time at the edge",
        ImagePath = "ms-appx:/Assets/DemoGallery/Realtime Image Classification Demo.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business | ExperienceType.IntelligentEdge,
        TechnologiesUsed = TechnologyType.CustomVision | TechnologyType.WinML,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2018/11/06")]
    public sealed partial class RealtimeImageClassification : Page, ICameraFrameProcessor
    {
        private readonly int CustomVisionModelInputSize = 227;
        private readonly float MinProbabilityValue = 0.3f;
        private readonly string NoMatchesMessage;
        private readonly string CameraPreviewDescription;
        private CustomVisionModel customVisionModel;
        private string[] allModelObjects;
        private IEnumerable<Tuple<string, float>> lastMatches;
        private bool isModelLoadedSuccessfully = false;
        public ObservableCollection<CustomVisionModelData> Projects { get; set; } = new ObservableCollection<CustomVisionModelData>();

        public RealtimeImageClassification()
        {
            this.InitializeComponent();

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.HideCameraControls();
            this.cameraControl.CameraFrameProcessor = this;
            this.cameraControl.PerformFaceTracking = false;
            this.cameraControl.ShowFaceTracking = false;
            this.cameraControl.CameraAspectRatioChanged += CameraControl_CameraAspectRatioChanged;

            this.NoMatchesMessage = "Ready to go!";
            this.CameraPreviewDescription = "Please point the camera to an object type supported by this model";
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

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
            this.cameraControl.CameraAspectRatioChanged -= CameraControl_CameraAspectRatioChanged;

            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = this;
            ShowWelcomeMessages(NoMatchesMessage, CameraPreviewDescription);
            await this.LoadProjectsFromFile(e.Parameter as CustomVisionModelData);
            await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);

            base.OnNavigatedTo(e);
        }

        private void ShowWelcomeMessages(string title, string subtitle = "")
        {
            this.landingMessagePanel.Visibility = Visibility.Visible;
            this.camerePreviewMessage.Text = title;
            this.camerePreviewDescriptionLabel.Text = subtitle;
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCameraHostSize();
        }

        private void UpdateCameraHostSize()
        {
            this.webCamHostGrid.Width = this.webCamHostGrid.ActualHeight * (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }

        private async void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.isModelLoadedSuccessfully = false;
                if (this.projectsComboBox.SelectedValue is CustomVisionModelData currentProject)
                {
                    ResetState();
                    ShowWelcomeMessages(NoMatchesMessage, CameraPreviewDescription);
                    await LoadCurrentModelAsync(currentProject);
                }
            }
            finally
            {
                this.isModelLoadedSuccessfully = true;
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
                    CustomVisionModelInputSize, CustomVisionModelInputSize, BitmapAlphaMode.Ignore))
                {
                    using (VideoFrame buffer = VideoFrame.CreateWithSoftwareBitmap(bitmapBuffer))
                    {
                        await videoFrame.CopyToAsync(buffer);

                        var input = new CustomVisionModelInput() { data = buffer };

                        DateTime start = DateTime.Now;

                        // Prediction process with ONNX model
                        CustomVisionModelOutput output = await this.customVisionModel.EvaluateAsync(input);

                        DateTime end = DateTime.Now;

                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ShowResults(output);
                            double predictionTimeInMilliseconds = (end - start).TotalMilliseconds;
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
                    ResetState();
                    await Util.GenericApiCallExceptionHandler(ex, "Failure processing frame");
                });
            }
        }

        private void ShowResults(CustomVisionModelOutput output)
        {
            List<Tuple<string, float>> result = GetModelOutputs(output);
            IEnumerable<Tuple<string, float>> matches = result?.Where(x => x.Item2 > MinProbabilityValue)?.OrderByDescending(x => x.Item2);

            // Remove nonexistent tags from the result panel
            IEnumerable<string> matchesTagNameCollection = matches?.Select(x => x.Item1);
            List<TagScoreControl> nonexistentTagList = this.tagScorePanel.Children?
                .Where(x => !matchesTagNameCollection.Contains((string)((UserControl)x).Tag))
                .Select(x => x as TagScoreControl)
                .ToList();
            foreach (TagScoreControl tagItem in nonexistentTagList)
            {
                this.tagScorePanel.Children?.Remove(tagItem);
            }

            // Update tags in the result panel
            int numClassesOnLastFrame = matches != null ? matches.Count() : 0;
            if (numClassesOnLastFrame > 0)
            {
                this.lastMatches = matches;
                this.resultsDetails.Visibility = Visibility.Visible;
                this.landingMessagePanel.Visibility = Visibility.Collapsed;

                int index = 0;
                foreach (var item in matches)
                {
                    TagScoreControl tagScoreControl = this.tagScorePanel.Children?.FirstOrDefault(x => (string)((UserControl)x).Tag == item.Item1) as TagScoreControl;
                    if (tagScoreControl != null)
                    {
                        TagScoreViewModel vm = tagScoreControl.DataContext as TagScoreViewModel;
                        vm.Probability = string.Format("{0}%", Math.Round(item.Item2 * 100));

                        int filterControlIndex = this.tagScorePanel.Children?.IndexOf(tagScoreControl) ?? -1;
                        if (filterControlIndex != index)
                        {
                            this.tagScorePanel.Children?.Move((uint)filterControlIndex, (uint)index);
                        }
                    }
                    else
                    {
                        TagScoreControl newControl = new TagScoreControl
                        {
                            Tag = item.Item1,
                            DataContext = new TagScoreViewModel(item.Item1) { Probability = string.Format("{0}%", Math.Round(item.Item2 * 100)) }
                        };
                        this.tagScorePanel.Children?.Add(newControl);
                    }
                    index++;
                }
            }
            else
            {
                this.resultsDetails.Visibility = Visibility.Collapsed;
            }
        }

        private List<Tuple<string, float>> GetModelOutputs(CustomVisionModelOutput output)
        {
            IList<IDictionary<string, float>> loss = output.loss;

            List<Tuple<string, float>> result = new List<Tuple<string, float>>();
            foreach (IDictionary<string, float> dict in loss)
            {
                foreach (var item in dict)
                {
                    result.Add(new Tuple<string, float>(item.Key, item.Value));
                }
            }
            return result;
        }

        private async Task LoadCurrentModelAsync(CustomVisionModelData currentProject)
        {
            try
            {
                this.deleteBtn.Visibility = currentProject.IsPrebuiltModel ? Visibility.Collapsed : Visibility.Visible;
                LoadSupportedClasses(currentProject);
                StorageFile modelFile = await GetModelFileAsync(currentProject);
                this.customVisionModel = await CustomVisionModel.CreateONNXModel(modelFile);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading current project");
            }
        }

        private void ResetState()
        {
            this.resultsDetails.Visibility = Visibility.Collapsed;
            this.tagScorePanel.Children.Clear();
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
                StorageFolder onnxProjectDataFolder = await CustomVisionDataLoader.GetOnnxModelStorageFolderAsync(CustomVisionProjectType.Classification);
                return await onnxProjectDataFolder.GetFileAsync(customVisionModelData.FileName);
            }
        }

        private async Task LoadProjectsFromFile(CustomVisionModelData preselectedProject = null)
        {
            try
            {
                this.Projects.Clear();
                List<CustomVisionModelData> prebuiltModelList = CustomVisionDataLoader.GetBuiltInModelData(CustomVisionProjectType.Classification) ?? new List<CustomVisionModelData>();
                foreach (CustomVisionModelData prebuiltModel in prebuiltModelList)
                {
                    this.Projects.Add(prebuiltModel);
                }

                List<CustomVisionModelData> customVisionModelList = await CustomVisionDataLoader.GetCustomVisionModelDataAsync(CustomVisionProjectType.Classification) ?? new List<CustomVisionModelData>();
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
                StorageFolder onnxProjectDataFolder = await CustomVisionDataLoader.GetOnnxModelStorageFolderAsync(CustomVisionProjectType.Classification);
                StorageFile modelFile = await onnxProjectDataFolder.GetFileAsync(currentProject.FileName);
                if (modelFile != null)
                {
                    await modelFile.DeleteAsync();
                }

                // update local file with custom models
                this.Projects.Remove(currentProject);
                List<CustomVisionModelData> updatedCustomModelList = this.Projects.Where(x => !x.IsPrebuiltModel).ToList();
                await CustomVisionDataLoader.SaveCustomVisionModelDataAsync(updatedCustomModelList, CustomVisionProjectType.Classification);

                this.projectsComboBox.SelectedIndex = 0;
            }
        }

        private void OnShowAllSupportedClassesTapped(object sender, TappedRoutedEventArgs e)
        {
            this.allSupportedClassesListView.ItemsSource = new ObservableCollection<string>(this.allModelObjects);
            supportedClassesBox.ShowAt((TextBlock)sender);
        }
    }

    public class TagScoreViewModel : INotifyPropertyChanged
    {
        public string Tag { get; set; }

        private string probability;
        public string Probability
        {
            get { return this.probability; }
            set
            {
                this.probability = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Probability"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TagScoreViewModel(string tag)
        {
            this.Tag = tag;
        }
    }
}
