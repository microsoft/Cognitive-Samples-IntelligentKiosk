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

using IntelligentKioskSample.Controls.Animation;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Media.Effects;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{

    [KioskExperience(Id = "RealtimeDriverMonitoring",
        DisplayName = "Realtime Driver Monitoring",
        Description = "See how AI can identify a distracted driver",
        ImagePath = "ms-appx:/Assets/DemoGallery/Realtime Driver Monitoring.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.Face | TechnologyType.Vision,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2016/11/09")]
    public sealed partial class RealtimeDriverMonitoring : Page
    {
        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;
        private bool isDescribingPhoto;
        private bool isProcessingDriverId;

        public const double SleepingApertureThreshold = 0.45;
        public const double YawningApertureThreshold = 0.7;
        public const int LookingAwayAngleThreshold = 20;

        private bool isInputSourceFromVideo = false;
        private Queue<VideoFrameData> queuedVideoFrames = new Queue<VideoFrameData>();
        private HashSet<double> processedFrames = new HashSet<double>();

        static SolidColorBrush BarChartColor = new SolidColorBrush(Color.FromArgb(0xff, 0x42, 0xbb, 0xfa));
        static SolidColorBrush BarChartAlertColor = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x44, 0x44));

        public RealtimeDriverMonitoring()
        {
            this.InitializeComponent();

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.FilterOutSmallFaces = true;
            this.cameraControl.HideCameraControls();
            this.cameraControl.ShowDialogOnApiErrors = SettingsHelper.Instance.ShowDialogOnApiErrors;
            this.cameraControl.CameraAspectRatioChanged += CameraControl_CameraAspectRatioChanged;

            this.inputSourceComboBox.SelectionChanged += this.InputSourceChanged;
        }

        private void CameraControl_CameraAspectRatioChanged(object sender, EventArgs e)
        {
            this.UpdateCameraHostSize();
        }

        private void StartProcessingLoop()
        {
            this.isProcessingLoopInProgress = true;

            if (this.processingLoopTask == null || this.processingLoopTask.Status != TaskStatus.Running)
            {
                this.processingLoopTask = Task.Run(() => this.ProcessingLoop());
            }
        }


        private async void ProcessingLoop()
        {
            while (this.isProcessingLoopInProgress)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (isInputSourceFromVideo)
                    {
                        if (FrameRelayVideoEffect.LatestSoftwareBitmap != null &&
                            (this.videoPlayer.CurrentState == MediaElementState.Playing || this.videoPlayer.CurrentState == MediaElementState.Paused))
                        {
                            this.ProcessVideoFrame();
                        }
                    }
                    else
                    {
                        if (!this.isProcessingPhoto)
                        {
                            this.isProcessingPhoto = true;
                            if (this.cameraControl.NumFacesOnLastFrame == 0)
                            {
                                await this.ProcessImage(null);
                            }
                            else
                            {
                                await this.ProcessImage(await this.cameraControl.TakeAutoCapturePhoto());
                            }
                        }
                    }

                    await Task.Delay(500);
                });
            }
        }

        private async void ProcessVideoFrame()
        {
            int frameNumber = GetVideoFrameNumber();

            if (!this.processedFrames.Contains(frameNumber))
            {
                this.processedFrames.Add(frameNumber);

                ImageAnalyzer img = new ImageAnalyzer(await Util.GetPixelBytesFromSoftwareBitmapAsync(FrameRelayVideoEffect.LatestSoftwareBitmap));
                img.UpdateDecodedImageSize(FrameRelayVideoEffect.LatestSoftwareBitmap.PixelHeight, FrameRelayVideoEffect.LatestSoftwareBitmap.PixelWidth);

                this.queuedVideoFrames.Enqueue(
                    new VideoFrameData
                    {
                        FrameNumber = (int)frameNumber,
                        Image = img
                    });
            }

            if (this.isProcessingPhoto || !this.queuedVideoFrames.Any())
            {
                return;
            }

            this.isProcessingPhoto = true;

            var queuedVideoFrame = this.queuedVideoFrames.Dequeue();
            var analyzer = queuedVideoFrame.Image;

            await this.ProcessImage(analyzer);
        }

        private int GetVideoFrameNumber()
        {
            // Equivalent of 2 fps
            return (int)(this.videoPlayer.Position.TotalMilliseconds / 500);
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

        private async Task ProcessImage(ImageAnalyzer e)
        {
            if (e == null)
            {
                this.UpdateUIForNoDriverDetected();
                this.isProcessingPhoto = false;
                return;
            }

            DateTime start = DateTime.Now;

            await e.DetectFacesAsync(detectFaceAttributes: true, detectFaceLandmarks: true);

            TimeSpan latency = DateTime.Now - start;

            this.faceLantencyDebugText.Text = string.Format("Face API latency: {0}ms", (int)latency.TotalMilliseconds);
            this.highLatencyWarning.Visibility = latency.TotalSeconds <= 1 ? Visibility.Collapsed : Visibility.Visible;

            this.StartCaptioningAsync(e);
            this.StartDriverIdAsync(e);

            await UpdateStateFromFacialFeatures(e);

            this.isProcessingPhoto = false;
        }

        private async void StartCaptioningAsync(ImageAnalyzer e)
        {
            if (this.isDescribingPhoto || !e.DetectedFaces.Any())
            {
                return;
            }

            DateTime start = DateTime.Now;

            await e.DescribeAsync();

            TimeSpan latency = DateTime.Now - start;
            this.visionLantencyDebugText.Text = string.Format("Vision API latency: {0}ms", (int)latency.TotalMilliseconds);
            this.highLatencyWarning.Visibility = latency.TotalSeconds <= 3 ? Visibility.Collapsed : Visibility.Visible;

            string desc = e.AnalysisResult.Description?.Captions?.FirstOrDefault()?.Text;
            bool distractionDetected = desc.Contains("phone") || desc.Contains("banana");
            this.distractionChart.DrawDataPoint(distractionDetected ? 1 : 0.02,
                                                distractionDetected ? BarChartAlertColor : BarChartColor,
                                                await GetFaceCropAsync(e),
                                                this.isInputSourceFromVideo ? Controls.WrapBehavior.Slide : Controls.WrapBehavior.Clear);

            this.isDescribingPhoto = false;
        }

        private void UpdateUIForNoDriverDetected()
        {
            this.faceLantencyDebugText.Text = "";
            this.visionLantencyDebugText.Text = "";
            this.highLatencyWarning.Visibility = Visibility.Collapsed;

            this.driverId.Text = "No faces detected";
        }

        private async void StartDriverIdAsync(ImageAnalyzer e)
        {
            if (this.isProcessingDriverId)
            {
                return;
            }

            if (!e.DetectedFaces.Any())
            {
                this.UpdateUIForNoDriverDetected();
                return;
            }

            await Task.WhenAll(e.IdentifyFacesAsync(), e.FindSimilarPersistedFacesAsync());

            SimilarFaceMatch faceMatch = e.SimilarFaceMatches.FirstOrDefault();
            if (faceMatch != null)
            {
                string name = "Unknown";

                IdentifiedPerson p = e.IdentifiedPersons.FirstOrDefault(f => f.FaceId == faceMatch.Face.FaceId);
                if (p != null)
                {
                    name = p.Person.Name;
                }
                else if (SettingsHelper.Instance.ShowAgeAndGender)
                {
                    switch (faceMatch.Face.FaceAttributes.Gender)
                    {
                        case Gender.Male:
                            name = "Unknown male";
                            break;
                        case Gender.Female:
                            name = "Unknown female";
                            break;
                    }
                }

                this.driverId.Text = string.Format("{0}", name, faceMatch.SimilarPersistedFace.PersistedFaceId.GetValueOrDefault().ToString("N").Substring(0, 4));
            }

            this.isProcessingDriverId = false;
        }

        private async Task UpdateStateFromFacialFeatures(ImageAnalyzer e)
        {
            var f = e.DetectedFaces.FirstOrDefault();
            if (f == null)
            {
                this.UpdateUIForNoDriverDetected();
                return;
            }

            this.ProcessHeadPose(f, await GetFaceCropAsync(e));
            this.ProcessMouth(f, await GetFaceCropAsync(e));
            this.ProcessEyes(f, await GetFaceCropAsync(e));
        }

        private void ProcessHeadPose(DetectedFace f, Image img)
        {
            double headPoseDeviation = Math.Abs(f.FaceAttributes.HeadPose.Yaw);

            double deviationRatio = f.FaceAttributes.HeadPose.Yaw / 35;

            this.headPoseChart.DrawDataPoint(deviationRatio >= 0 ? Math.Max(0.05, deviationRatio) : Math.Min(-0.02, deviationRatio),
                                             headPoseDeviation <= LookingAwayAngleThreshold ? BarChartColor : BarChartAlertColor,
                                             img,
                                             this.isInputSourceFromVideo ? Controls.WrapBehavior.Slide : Controls.WrapBehavior.Clear);
        }

        private void ProcessMouth(DetectedFace f, Image img)
        {
            double mouthWidth = Math.Abs(f.FaceLandmarks.MouthRight.X - f.FaceLandmarks.MouthLeft.X);
            double mouthHeight = Math.Abs(f.FaceLandmarks.UpperLipBottom.Y - f.FaceLandmarks.UnderLipTop.Y);

            double mouthAperture = mouthHeight / mouthWidth;
            mouthAperture = Math.Min((mouthAperture - 0.1) / 0.4, 1);

            this.mouthApertureChart.DrawDataPoint(Math.Max(0.05, mouthAperture),
                                                  mouthAperture <= YawningApertureThreshold ? BarChartColor : BarChartAlertColor,
                                                  img,
                                                  this.isInputSourceFromVideo ? Controls.WrapBehavior.Slide : Controls.WrapBehavior.Clear);
        }

        private void ProcessEyes(DetectedFace f, Image img)
        {
            double leftEyeWidth = Math.Abs(f.FaceLandmarks.EyeLeftInner.X - f.FaceLandmarks.EyeLeftOuter.X);
            double leftEyeHeight = Math.Abs(f.FaceLandmarks.EyeLeftBottom.Y - f.FaceLandmarks.EyeLeftTop.Y);

            double rightEyeWidth = Math.Abs(f.FaceLandmarks.EyeRightInner.X - f.FaceLandmarks.EyeRightOuter.X);
            double rightEyeHeight = Math.Abs(f.FaceLandmarks.EyeRightBottom.Y - f.FaceLandmarks.EyeRightTop.Y);

            double eyeAperture = Math.Max(leftEyeHeight / leftEyeWidth, rightEyeHeight / rightEyeWidth);
            eyeAperture = Math.Min((eyeAperture - 0.2) / 0.3, 1);

            this.eyeApertureChart.DrawDataPoint(Math.Max(0.05, eyeAperture),
                                                eyeAperture >= SleepingApertureThreshold ? BarChartColor : BarChartAlertColor,
                                                img,
                                                this.isInputSourceFromVideo ? Controls.WrapBehavior.Slide : Controls.WrapBehavior.Clear);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            EnterKioskMode();

            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey) || string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey))
            {
                await new MessageDialog("Missing Face API or Vision API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                FaceListManager.FaceListsUserDataFilter = SettingsHelper.Instance.WorkspaceKey + "_RealTimeDriverMonitoring";

                await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
                this.StartProcessingLoop();
            }

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
            this.isProcessingLoopInProgress = false;
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
            this.cameraControl.CameraAspectRatioChanged -= CameraControl_CameraAspectRatioChanged;

            await FaceListManager.ResetFaceLists();

            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateCameraHostSize();
        }

        private void UpdateCameraHostSize()
        {
            this.cameraHostGrid.Width = this.cameraHostGrid.ActualHeight * (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }

        private async void OpenVideoClicked(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Downloads, ViewMode = PickerViewMode.Thumbnail };
            fileOpenPicker.FileTypeFilter.Add(".mp4");

            var pickedFile = await fileOpenPicker.PickSingleFileAsync();
            if (pickedFile != null)
            {
                //Set the stream source to the MediaElement control
                await this.StartVideoAsync(pickedFile);

                this.ResetState();
                StartProcessingLoop();
            }
        }

        private async Task StartVideoAsync(StorageFile file)
        {
            try
            {
                if (this.videoPlayer.CurrentState == MediaElementState.Playing)
                {
                    this.videoPlayer.Stop();
                }

                FrameRelayVideoEffect.ResetState();

                MediaClip clip = await MediaClip.CreateFromFileAsync(file);
                clip.VideoEffectDefinitions.Add(new VideoEffectDefinition(typeof(FrameRelayVideoEffect).FullName));

                MediaComposition compositor = new MediaComposition();
                compositor.Clips.Add(clip);

                this.videoPlayer.SetMediaStreamSource(compositor.GenerateMediaStreamSource());
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error starting playback.");
            }
        }

        private async void InputSourceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.videoInputSource.IsSelected)
            {
                await this.cameraControl.StopStreamAsync();
                this.cameraControl.Visibility = Visibility.Collapsed;
                this.videoPlayer.Visibility = Visibility.Visible;
                this.isInputSourceFromVideo = true;
            }
            else
            {
                await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
                this.cameraControl.Visibility = Visibility.Visible;
                this.videoPlayer.Visibility = Visibility.Collapsed;
                this.isInputSourceFromVideo = false;
            }

            this.ResetState();
        }

        private void ResetState()
        {
            this.queuedVideoFrames.Clear();
            this.processedFrames.Clear();
            this.distractionChart.Clear();
            this.headPoseChart.Clear();
            this.mouthApertureChart.Clear();
            this.eyeApertureChart.Clear();
        }

        private async Task<Image> GetFaceCropAsync(ImageAnalyzer img)
        {
            ImageSource croppedImage;

            if (img.DetectedFaces == null || !img.DetectedFaces.Any())
            {
                croppedImage = new BitmapImage();
                await ((BitmapImage)croppedImage).SetSourceAsync((await img.GetImageStreamCallback()).AsRandomAccessStream());
            }
            else
            {
                // Crop the primary face
                FaceRectangle rect = img.DetectedFaces.First().FaceRectangle;
                double heightScaleFactor = 1.8;
                double widthScaleFactor = 1.8;
                FaceRectangle biggerRectangle = new FaceRectangle
                {
                    Height = Math.Min((int)(rect.Height * heightScaleFactor), img.DecodedImageHeight),
                    Width = Math.Min((int)(rect.Width * widthScaleFactor), img.DecodedImageWidth)
                };
                biggerRectangle.Left = Math.Max(0, rect.Left - (int)(rect.Width * ((widthScaleFactor - 1) / 2)));
                biggerRectangle.Top = Math.Max(0, rect.Top - (int)(rect.Height * ((heightScaleFactor - 1) / 1.4)));

                croppedImage = await Util.GetCroppedBitmapAsync(img.GetImageStreamCallback, biggerRectangle.ToRect());
            }

            return new Image { Source = croppedImage, Height = 200 };
        }
    }

    internal class VideoFrameData
    {
        public double FrameNumber { get; set; }
        public ImageAnalyzer Image { get; set; }
    }

}