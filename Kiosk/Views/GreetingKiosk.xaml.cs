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

using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using IntelligentKioskSample.ViewModels;

namespace IntelligentKioskSample.Views
{

    [KioskExperience(Title = "Greeting Kiosk", ImagePath = "ms-appx:/Assets/GreetingKiosk.jpg", ExperienceType = ExperienceType.Kiosk)]
    public sealed partial class GreetingKiosk : Page
    {
        private Task _processingLoopTask;
        private bool _isProcessingLoopInProgress;
        private bool _isProcessingPhoto;
        

        public GreetingKioskViewModel ViewModel => DataContext as GreetingKioskViewModel;

        public GreetingKiosk()
        {
            InitializeComponent();
            DataContext = new GreetingKioskViewModel();
            Window.Current.Activated += CurrentWindowActivationStateChanged;
            cameraControl.FilterOutSmallFaces = true;
            cameraControl.HideCameraControls();
            cameraControl.CameraAspectRatioChanged += CameraControl_CameraAspectRatioChanged;
        }

        private void StartProcessingLoop()
        {
            _isProcessingLoopInProgress = true;

            if (_processingLoopTask == null || _processingLoopTask.Status != TaskStatus.Running)
            {
                _processingLoopTask = Task.Run(() => ProcessingLoop());
            }
        }

        private async void ProcessingLoop()
        {
            while (_isProcessingLoopInProgress)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (_isProcessingPhoto)
                        return;

                    _isProcessingPhoto = true;
                    var start = DateTime.Now;

                    if (cameraControl.NumFacesOnLastFrame == 0)
                    {
                        await ViewModel.ProcessCameraCapture(null);
                    }
                    else
                    {
                        await ViewModel.ProcessCameraCapture(await cameraControl.CaptureFrameAsync());
                    }

                    var latency = DateTime.Now - start;
                    faceLantencyDebugText.Text = string.Format("Face API latency: {0}ms", (int)latency.TotalMilliseconds);
                    _isProcessingPhoto = false;
                });

                //simplify timing 
                //add counter 
                await Task.Delay(cameraControl.NumFacesOnLastFrame == 0 ? 100 : 1000);
            }
        }

        private async void CurrentWindowActivationStateChanged(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if ((e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated ||
                e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.PointerActivated) &&
                cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Shutdown)
            {
                // When our Window loses focus due to user interaction Windows shuts it down, so we 
                // detect here when the window regains focus and trigger a restart of the camera.
                await cameraControl.StartStreamAsync(true);
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
                await cameraControl.StartStreamAsync(true);
                StartProcessingLoop();
            }

            base.OnNavigatedTo(e);
        }

        private void EnterKioskMode()
        {
#if !DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
                return;

            var view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
            }
#endif


        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _isProcessingLoopInProgress = false;
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
            cameraControl.CameraAspectRatioChanged -= CameraControl_CameraAspectRatioChanged;

            await cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCameraHostSize();
        }

        private void CameraControl_CameraAspectRatioChanged(object sender, EventArgs e)
        {
            UpdateCameraHostSize();
        }

        private void UpdateCameraHostSize()
        {
            cameraHostGrid.Width = cameraHostGrid.ActualHeight * (cameraControl.CameraAspectRatio != 0 ? cameraControl.CameraAspectRatio : 1.777777777777);
        }
    }
}