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

using ServiceHelpers;
using IntelligentKioskSample.Controls;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using Windows.Media.SpeechSynthesis;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace IntelligentKioskSample.Views
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [KioskExperience(Title = "Realtime Driver Monitoring", ImagePath = "ms-appx:/Assets/RealtimeDriverMonitoring.jpg", ExperienceType = ExperienceType.Other)]
    public sealed partial class RealtimeDriverMonitoring : Page, INotifyPropertyChanged
    {
        private DateTime lastEyeOpenTime = DateTime.MinValue;
        private DateTime lastFrontalHeadPoseTime = DateTime.MinValue;
        private DateTime lastNotYawningTime = DateTime.MinValue;

        private double mouthAperture;
        private double eyeAperture;
        private double headPoseDeviation;

        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;
        private bool isDescribingPhoto;
        private bool isProcessingDriverId;

        public event PropertyChangedEventHandler PropertyChanged;

        public const double DefaultSleepingApertureThreshold = 0.15;
        public const double DefaultYawningApertureThreshold = 0.35;

        private double sleepingApertureThreshold;
        public double SleepingApertureThreshold
        {
            get { return this.sleepingApertureThreshold; }
            set
            {
                this.sleepingApertureThreshold = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SleepingApertureThreshold"));
            }
        }

        private double yawningApertureThreshold;
        public double YawningApertureThreshold
        {
            get { return this.yawningApertureThreshold; }
            set
            {
                this.yawningApertureThreshold = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("YawningApertureThreshold"));
            }
        }

        public RealtimeDriverMonitoring()
        {
            this.InitializeComponent();

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.FilterOutSmallFaces = true;
            this.cameraControl.HideCameraControls();
            this.cameraControl.ShowDialogOnApiErrors = false;
            this.cameraControl.CameraAspectRatioChanged += CameraControl_CameraAspectRatioChanged;
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
                    if (!this.isProcessingPhoto)
                    {
                        this.isProcessingPhoto = true;
                        if (this.cameraControl.NumFacesOnLastFrame == 0)
                        {
                            await this.ProcessCameraCapture(null);
                        }
                        else
                        {
                            await this.ProcessCameraCapture(await this.cameraControl.TakeAutoCapturePhoto());
                        }
                    }
                });

                await Task.Delay(500);
            }
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

        private async Task ProcessCameraCapture(ImageAnalyzer e)
        {
            if (e == null)
            {
                this.UpdateUIForNoDriverDetected();
                this.isProcessingPhoto = false;
                return;
            }

            if (this.visionToggle.IsOn)
            {
                this.StartCaptioningAsync(e);
            }

            DateTime start = DateTime.Now;

            await e.DetectFacesAsync(detectFaceAttributes: true, detectFaceLandmarks: true);

            TimeSpan latency = DateTime.Now - start;

            this.faceLantencyDebugText.Text = string.Format("Face API latency: {0}ms", (int)latency.TotalMilliseconds);
            this.highLatencyWarning.Visibility = latency.TotalSeconds <= 1 ? Visibility.Collapsed : Visibility.Visible;

            this.StartDriverIdAsync(e);

            UpdateStateFromFacialFeatures(e);

            this.isProcessingPhoto = false;
        }

        private async void StartCaptioningAsync(ImageAnalyzer e)
        {
            if (this.isDescribingPhoto)
            {
                return;
            }

            DateTime start = DateTime.Now;

            await e.DescribeAsync();

            TimeSpan latency = DateTime.Now - start;
            this.visionLantencyDebugText.Text = string.Format("Vision API latency: {0}ms", (int)latency.TotalMilliseconds);
            this.highLatencyWarning.Visibility = latency.TotalSeconds <= 3 ? Visibility.Collapsed : Visibility.Visible;

            string desc = e.AnalysisResult.Description?.Captions?[0].Text;
            if (string.IsNullOrEmpty(desc) || !this.visionToggle.IsOn)
            {
                this.objectDistraction.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.visionAPICaptionTextBlock.Text = string.Format("{0} ({1}%)", desc, (int)(e.AnalysisResult.Description.Captions[0].Confidence * 100));

                string distraction = string.Empty;
                if (desc.Contains("phone"))
                {
                    distraction = "On the phone!";
                }
                else if (desc.Contains("banana"))
                {
                    distraction = "Eating a banana!?";
                }

                this.objectDistraction.Text = distraction;
                this.objectDistraction.Visibility = distraction != "" ? Visibility.Visible : Visibility.Collapsed;
            }

            this.isDescribingPhoto = false;
        }

        private void UpdateUIForNoDriverDetected()
        {
            this.faceLantencyDebugText.Text = "";
            this.visionLantencyDebugText.Text = "";
            this.highLatencyWarning.Visibility = Visibility.Collapsed;

            this.driverId.Text = "No faces detected. Please look at the camera to start.";
            this.sleeping.Visibility = this.lookingAway.Visibility = this.yawning.Visibility = this.objectDistraction.Visibility  = Visibility.Collapsed;
            this.headPoseIndicator.Margin = new Thickness(0);
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
            if(faceMatch != null)
            {
                string name = "Unknown";

                IdentifiedPerson p = e.IdentifiedPersons.FirstOrDefault(f => f.FaceId == faceMatch.Face.FaceId);
                if (p != null)
                {
                    name = p.Person.Name;
                }
                else
                {
                    if (faceMatch.Face.FaceAttributes.Gender == "male")
                    {
                        name = "Unknown male";
                    }
                    else if (faceMatch.Face.FaceAttributes.Gender == "female")
                    {
                        name = "Unknown female";
                    }
                }

                this.driverId.Text = string.Format("{0}\nFace Id: {1}", name, faceMatch.SimilarPersistedFace.PersistedFaceId.ToString("N").Substring(0, 4));
            }

            this.isProcessingDriverId = false;
        }

        private void UpdateStateFromFacialFeatures(ImageAnalyzer e)
        {
            var f = e.DetectedFaces.FirstOrDefault();
            if (f == null)
            {
                this.UpdateUIForNoDriverDetected();
                return;
            }

            this.ProcessHeadPose(f);
            this.ProcessMouth(f);
            this.ProcessEyes(f);

            this.sleeping.Visibility = (DateTime.Now - this.lastEyeOpenTime).TotalSeconds >= 2 ? Visibility.Visible : Visibility.Collapsed;
            this.lookingAway.Visibility = (DateTime.Now - this.lastFrontalHeadPoseTime).TotalSeconds >= 0.5 ? Visibility.Visible : Visibility.Collapsed;
            this.yawning.Visibility = (DateTime.Now - this.lastNotYawningTime).TotalSeconds >= 2 ? Visibility.Visible : Visibility.Collapsed;

            if (this.sleeping.Visibility == Visibility.Visible &&
                this.alarmSound.CurrentState != Windows.UI.Xaml.Media.MediaElementState.Playing)
            {
                this.alarmSound.Play();
            }
        }

        private void ProcessHeadPose(Face f)
        {
            headPoseDeviation = Math.Abs(f.FaceAttributes.HeadPose.Yaw);

            this.headPoseIndicator.Margin = new Thickness((-f.FaceAttributes.HeadPose.Yaw / 90) * headPoseIndicatorHost.ActualWidth / 2, 0, 0, 0);

            double threshold = 25;
            if (headPoseDeviation <= threshold)
            {
                this.lastFrontalHeadPoseTime = DateTime.Now;
            }
        }

        private void ProcessMouth(Face f)
        {            
            double mouthWidth = Math.Abs(f.FaceLandmarks.MouthRight.X - f.FaceLandmarks.MouthLeft.X);
            double mouthHeight = Math.Abs(f.FaceLandmarks.UpperLipBottom.Y - f.FaceLandmarks.UnderLipTop.Y);

            this.mouthUX.Height = Math.Max(2, this.mouthUX.ActualWidth * 1.1 * mouthHeight / mouthWidth);

            mouthAperture = mouthHeight / mouthWidth;
            this.currentMouthAperture.Text = mouthAperture.ToString("F2");

            if (mouthAperture <= this.YawningApertureThreshold)
            {
                this.lastNotYawningTime = DateTime.Now;
            }
        }

        private void ProcessEyes(Face f)
        {
            double leftEyeWidth = Math.Abs(f.FaceLandmarks.EyeLeftInner.X - f.FaceLandmarks.EyeLeftOuter.X);
            double leftEyeHeight = Math.Abs(f.FaceLandmarks.EyeLeftBottom.Y - f.FaceLandmarks.EyeLeftTop.Y);

            this.leftEyeUX.Height = Math.Max(2, this.leftEyeUX.ActualWidth * 0.9 * leftEyeHeight / leftEyeWidth);

            double rightEyeWidth = Math.Abs(f.FaceLandmarks.EyeRightInner.X - f.FaceLandmarks.EyeRightOuter.X);
            double rightEyeHeight = Math.Abs(f.FaceLandmarks.EyeRightBottom.Y - f.FaceLandmarks.EyeRightTop.Y);

            this.rightEyeUX.Height = Math.Max(2, this.rightEyeUX.ActualWidth * 0.9 * rightEyeHeight / rightEyeWidth);

            eyeAperture = Math.Max(leftEyeHeight / leftEyeWidth, rightEyeHeight / rightEyeWidth);
            this.currentEyeAperture.Text = eyeAperture.ToString("F2");

            if (eyeAperture >= this.SleepingApertureThreshold)
            {
                this.lastEyeOpenTime = DateTime.Now;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.SleepingApertureThreshold = SettingsHelper.Instance.DriverMonitoringSleepingThreshold;
            this.YawningApertureThreshold = SettingsHelper.Instance.DriverMonitoringYawningThreshold;

            using (var speech = new SpeechSynthesizer())
            {
                speech.Voice = SpeechSynthesizer.AllVoices.First(gender => gender.Gender == VoiceGender.Female);
                SpeechSynthesisStream stream = await speech.SynthesizeTextToStreamAsync("Wake up!");
                this.alarmSound.SetSource(stream, stream.ContentType);
            }

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

            if (SettingsHelper.Instance.DriverMonitoringSleepingThreshold != this.SleepingApertureThreshold)
            {
                SettingsHelper.Instance.DriverMonitoringSleepingThreshold = this.SleepingApertureThreshold;
            }

            if (SettingsHelper.Instance.DriverMonitoringYawningThreshold != this.YawningApertureThreshold)
            {
                SettingsHelper.Instance.DriverMonitoringYawningThreshold = this.YawningApertureThreshold;
            }

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

        private void visionToggleChanged(object sender, RoutedEventArgs e)
        {
            if (!this.visionToggle.IsOn)
            {
                this.visionAPICaptionTextBlock.Text = "enable to start analyzing activities (e.g. cell phone)";
                this.objectDistraction.Visibility = Visibility.Collapsed;
            }
        }

        private void RestoreDefaultThresholdsClicked(object sender, RoutedEventArgs e)
        {
            this.SleepingApertureThreshold = DefaultSleepingApertureThreshold;
            this.YawningApertureThreshold = DefaultYawningApertureThreshold;
        }
    }
}