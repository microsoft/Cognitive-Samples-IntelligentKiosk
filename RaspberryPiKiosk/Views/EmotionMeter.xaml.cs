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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Emmellsoft.IoT.Rpi.SenseHat;
using RPi.SenseHat.Demo.Demos;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.UI;

namespace IntelligentKioskSample.Views
{

    [KioskExperience(Title = "EMeter", ImagePath = "ms-appx:/Assets/EMeter.jpg", ExperienceType = ExperienceType.Kiosk)]
    public sealed partial class EmotionMeter : Page
    {
        private ISenseHat senseHat;

        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;

        public EmotionMeter()
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
                        await this.ProcessCameraCapture(await this.cameraControl.TakeAutoCapturePhoto());
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

            await e.DetectEmotionAsync();

            if (e.DetectedEmotion.Any())
            {
                this.UpdateEmotionMeter(e);
            }
            else
            {
                this.UpdateUIForNoFacesDetected();
            }

            TimeSpan latency = DateTime.Now - start;
            this.faceLantencyDebugText.Text = string.Format("Emotion API latency: {0}ms", (int)latency.TotalMilliseconds);

            this.isProcessingPhoto = false;
        }

        private void UpdateEmotionMeter(ImageAnalyzer img)
        {
            if (senseHat != null)
            {
                var scores = img.DetectedEmotion.First().Scores;

                senseHat.Display.Clear();

                // col 0: anger
                senseHat.Display.Screen[0, 7] = Color.FromArgb(0xff, 0xff, 0x54, 0x2c);
                for (int row = 1; row < Math.Round(8 * scores.Anger); row++)
                {
                    senseHat.Display.Screen[0, 7 - row] = Color.FromArgb(0xff, 0xff, 0x54, 0x2c);
                }

                // col 1: contempt
                senseHat.Display.Screen[1, 7] = Color.FromArgb(0xff, 0xce, 0x2d, 0x90);
                for (int row = 1; row < Math.Round(8 * scores.Contempt); row++)
                {
                    senseHat.Display.Screen[1, 7 - row] = Color.FromArgb(0xff, 0xce, 0x2d, 0x90);
                }

                //col 2: disgust
                senseHat.Display.Screen[2, 7] = Color.FromArgb(0xff, 0x8c, 0x43, 0xbd);
                for (int row = 1; row < Math.Round(8 * scores.Disgust); row++)
                {
                    senseHat.Display.Screen[2, 7 - row] = Color.FromArgb(0xff, 0x8c, 0x43, 0xbd);
                }

                //col 3: fear
                senseHat.Display.Screen[3, 7] = Color.FromArgb(0xff, 0xfe, 0xb5, 0x52);
                for (int row = 1; row < Math.Round(8 * scores.Disgust); row++)
                {
                    senseHat.Display.Screen[3, 7 - row] = Color.FromArgb(0xff, 0xfe, 0xb5, 0x52);
                }

                // col 4: happiness
                senseHat.Display.Screen[4, 7] = Color.FromArgb(0xff, 0x4f, 0xc7, 0x45);
                for (int row = 1; row < Math.Round(8 * scores.Happiness); row++)
                {
                    senseHat.Display.Screen[4, 7 - row] = Color.FromArgb(0xff, 0x4f, 0xc7, 0x45);
                }

                //col 5: neutral
                senseHat.Display.Screen[5, 7] = Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e);
                for (int row = 1; row < Math.Round(8 * scores.Neutral); row++)
                {
                    senseHat.Display.Screen[5, 7 - row] = Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e);
                }

                //col 6: sadness
                senseHat.Display.Screen[6, 7] = Color.FromArgb(0xff, 0x47, 0x8b, 0xcb);
                for (int row = 1; row < Math.Round(8 * scores.Sadness); row++)
                {
                    senseHat.Display.Screen[6, 7 - row] = Color.FromArgb(0xff, 0x47, 0x8b, 0xcb);
                }

                //col 7: surprise
                senseHat.Display.Screen[7, 7] = Color.FromArgb(0xff, 0xff, 0xf6, 0xd6);
                for (int row = 1; row < Math.Round(8 * scores.Surprise); row++)
                {
                    senseHat.Display.Screen[7, 7 - row] = Color.FromArgb(0xff, 0xff, 0xf6, 0xd6);
                }

                senseHat.Display.Update();
            }
        }

        private void UpdateUIForNoFacesDetected()
        {
            if (senseHat != null)
            {
                senseHat.Display.Clear();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            EnterKioskMode();

            if (string.IsNullOrEmpty(SettingsHelper.Instance.EmotionApiKey))
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