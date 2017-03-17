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

using Emmellsoft.IoT.Rpi.SenseHat;
using Microsoft.ProjectOxford.Face.Contract;
using RPi.SenseHat.Demo.Demos;
using ServiceHelpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{

    [KioskExperience(Title = "Greetings", ImagePath = "ms-appx:/Assets/GreetingKiosk.jpg", ExperienceType = ExperienceType.Kiosk)]
    public sealed partial class GreetingKiosk : Page
    {
        private ISenseHat senseHat;
        private SingleColorScrollText senseHatScrollText;

        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;

        private string currentScrollingName;

        public GreetingKiosk()
        {
            this.InitializeComponent();

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.PerformFaceTracking = false;
            this.cameraControl.FilterOutSmallFaces = true;
            this.cameraControl.HideCameraControls();
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
                        await this.ProcessCameraCapture(await this.cameraControl.CaptureFrameAsync());
                    }
                });

                await Task.Delay(1000);
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
                this.UpdateUIForNoFacesDetected();
                this.isProcessingPhoto = false;
                return;
            }

            DateTime start = DateTime.Now;

            await e.DetectFacesAsync(detectFaceAttributes: true);

            if (e.DetectedFaces.Any())
            {
                await e.IdentifyFacesAsync();

                UpdateGreeting(e);

                if (e.IdentifiedPersons.Any())
                {
                    this.greetingTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.GreenYellow);
                    this.greetingSymbol.Foreground = new SolidColorBrush(Windows.UI.Colors.GreenYellow);
                    this.greetingSymbol.Symbol = Symbol.Comment;
                }
                else
                {
                    this.greetingTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Yellow);
                    this.greetingSymbol.Foreground = new SolidColorBrush(Windows.UI.Colors.Yellow);
                    this.greetingSymbol.Symbol = Symbol.View;
                }
            }
            else
            {
                this.UpdateUIForNoFacesDetected();
            }

            TimeSpan latency = DateTime.Now - start;
            this.faceLantencyDebugText.Text = string.Format("Face API latency: {0}ms", (int)latency.TotalMilliseconds);

            this.isProcessingPhoto = false;
        }

        private void UpdateGreeting(ImageAnalyzer img)
        {
            Face mainFace = img.DetectedFaces.First();
            int age = (int)Math.Round(mainFace.FaceAttributes.Age);

            string name = "";
            if (img.IdentifiedPersons.Any())
            {
                IdentifiedPerson mainFaceId = img.IdentifiedPersons.FirstOrDefault(p => p.FaceId == mainFace.FaceId);
                if (mainFaceId != null)
                {
                    name = mainFaceId.Person.Name.Split(' ')[0];
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                if (img.DetectedFaces.First().FaceAttributes.Gender == "male")
                {
                    name = "Male";
                }
                else
                {
                    name = "Female";
                }
            }

            string greeting = string.Format("{0}, {1}", name, age);

            this.greetingTextBlock.Text = greeting;
            if (this.currentScrollingName != name)
            {
                this.UpdateSenseHatScrollText(greeting);
                this.currentScrollingName = name;
            }
        }

        private void UpdateSenseHatScrollText(string text)
        {
            if (this.senseHatScrollText != null)
            {
                this.senseHatScrollText.StopScroll();
            }

            this.senseHatScrollText = new SingleColorScrollText(text);
            this.senseHatScrollText.StartScroll();
        }

        private void UpdateUIForNoFacesDetected()
        {
            this.greetingTextBlock.Text = "Step in front of the camera to start";
            this.greetingTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            this.greetingSymbol.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            this.greetingSymbol.Symbol = Symbol.Contact;

            if (this.senseHatScrollText != null)
            {
                this.senseHatScrollText.StopScroll();
                this.currentScrollingName = "";
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            EnterKioskMode();

            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey))
            {
                await new MessageDialog("Missing Face API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
                this.StartProcessingLoop();
            }

            //get a reference to SenseHat
            try
            {
                senseHat = await SenseHatFactory.GetSenseHat();

                senseHat.Display.Clear();
                senseHat.Display.Update();
            }
            catch (Exception)
            {
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

            await this.cameraControl.StopStreamAsync();

            if (this.senseHatScrollText != null)
            {
                this.senseHatScrollText.StopScroll();
            }

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
    }
}