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
using IntelligentKioskSample.Controls.Overlays;
using IntelligentKioskSample.Controls.Overlays.Primitives;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ServiceHelpers;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "How-Old Kiosk",
        DisplayName = "How Old",
        Description = "Get an estimate of a person's age and gender from a photo",
        ImagePath = "ms-appx:/Assets/DemoGallery/How Old.jpg",
        ExperienceType = ExperienceType.Automated | ExperienceType.Fun,
        TechnologiesUsed = TechnologyType.Face,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2015/10/30")]
    public sealed partial class HowOldKioskPage : Page
    {
        public HowOldKioskPage()
        {
            this.InitializeComponent();

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.EnableAutoCaptureMode = true;
            this.cameraControl.FilterOutSmallFaces = true;
            this.cameraControl.AutoCaptureStateChanged += CameraControl_AutoCaptureStateChanged;
            this.cameraControl.CameraAspectRatioChanged += CameraControl_CameraAspectRatioChanged;
            this.cameraControl.ShowDialogOnApiErrors = SettingsHelper.Instance.ShowDialogOnApiErrors;
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
                await this.cameraControl.StartStreamAsync();
            }
        }

        private async void CameraControl_AutoCaptureStateChanged(object sender, AutoCaptureState e)
        {
            switch (e)
            {
                case AutoCaptureState.WaitingForFaces:
                    this.cameraGuideBallon.Opacity = 1;
                    this.cameraGuideText.Text = "Step in front of the camera to start!";
                    this.cameraGuideHost.Opacity = 1;
                    break;
                case AutoCaptureState.WaitingForStillFaces:
                    this.cameraGuideText.Text = "Hold still...";
                    break;
                case AutoCaptureState.ShowingCountdownForCapture:
                    this.cameraGuideText.Text = "";
                    this.cameraGuideBallon.Opacity = 0;

                    this.cameraGuideCountdownHost.Opacity = 1;
                    this.countDownTextBlock.Text = "3";
                    await Task.Delay(750);
                    this.countDownTextBlock.Text = "2";
                    await Task.Delay(750);
                    this.countDownTextBlock.Text = "1";
                    await Task.Delay(750);
                    this.cameraGuideCountdownHost.Opacity = 0;

                    this.ProcessCameraCapture(await this.cameraControl.TakeAutoCapturePhoto());
                    break;
                case AutoCaptureState.ShowingCapturedPhoto:
                    this.cameraGuideHost.Opacity = 0;
                    break;
                default:
                    break;
            }
        }

        private async void ProcessCameraCapture(ImageAnalyzer e)
        {
            if (e == null)
            {
                this.cameraControl.RestartAutoCaptureCycle();
                return;
            }

            //Show age label
            ProgressIndicator.IsActive = true;
            var image = await e.GetImageSource();
            OverlayPresenter.Source = image;
            await e.DetectFacesAsync(true);
            await e.IdentifyFacesAsync();
            Overlays.ItemsSource = e.DetectedFaces.Select(i => new OverlayInfo<Object, AgeInfo>
            {
                Rect = i.FaceRectangle.ToRect(),
                LabelsExt = new[] { new AgeInfo()
                {
                    Age = (int)Math.Round(i.FaceAttributes.Age.GetValueOrDefault()),
                    Gender = i.FaceAttributes.Gender.GetValueOrDefault() == Gender.Female ? AgeInfoGender.Female : AgeInfoGender.Male,
                    Name = e.IdentifiedPersons.FirstOrDefault(t => i.FaceId == t.FaceId)?.Person?.Name,
                    Confidence = e.IdentifiedPersons.FirstOrDefault(t => i.FaceId == t.FaceId)?.Confidence ?? 0
                } }
            }).ToArray();
            ProgressIndicator.IsActive = false;

            //Show timer
            this.photoBalloonHost.Opacity = 1;
            this.photoBalloonHost.IsHitTestVisible = true;
            double decrementPerSecond = 100.0 / SettingsHelper.Instance.HowOldKioskResultDisplayDuration;
            for (double i = 100; i >= 0; i -= decrementPerSecond)
            {
                this.resultDisplayTimerUI.Value = i;
                await Task.Delay(1000);
            }

            this.photoBalloonHost.Opacity = 0;
            this.photoBalloonHost.IsHitTestVisible = false;
            this.cameraControl.RestartAutoCaptureCycle();
            Overlays.ItemsSource = null;
            OverlayPresenter.Source = null;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!SettingsHelper.Instance.ShowAgeAndGender)
            {
                await new MessageDialog("To use this demo please enable Age and Gender prediction in the Settings screen.", "Age and Gender prediction is disabled").ShowAsync();
            }
            else if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey))
            {
                await new MessageDialog("Missing Face API Key. Please enter a key in the Settings page.", "Missing Face API Key").ShowAsync();
            }
            else
            {
                await this.cameraControl.StartStreamAsync();
            }

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
            this.cameraControl.AutoCaptureStateChanged -= CameraControl_AutoCaptureStateChanged;
            this.cameraControl.CameraAspectRatioChanged -= CameraControl_CameraAspectRatioChanged;

            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateCameraHostSize();
        }

        private void UpdateCameraHostSize()
        {
            this.cameraHostGrid.Height = this.cameraHostGrid.ActualWidth / (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }
    }
}
