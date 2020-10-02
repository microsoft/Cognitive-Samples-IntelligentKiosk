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

using IntelligentKioskSample.Models;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.AnomalyDetector
{
    [KioskExperience(Id = "AnomalyDetector",
        DisplayName = "Anomaly Detector",
        Description = "An AI service that helps you foresee problems before they occur",
        ImagePath = "ms-appx:/Assets/DemoGallery/Anomaly detector.jpg",
        ExperienceType = ExperienceType.Preview | ExperienceType.Business,
        TechnologyArea = TechnologyAreaType.Decision,
        TechnologiesUsed = TechnologyType.AnomalyDetector,
        DateAdded = "2019/09/03")]
    public sealed partial class AnomalyDetectorDemo : Page, INotifyPropertyChanged
    {
        private MediaCapture mediaCapture;
        private MediaFrameReader mediaFrameReader;
        private float maxVolumeInSampleBuffer = 0;

        public TabHeader BikeRentalTab = new TabHeader() { Name = "Bike rental" };
        public TabHeader TelecomTab = new TabHeader() { Name = "Telecom" };
        public TabHeader ManufacturingTab = new TabHeader() { Name = "Manufacturing" };
        public TabHeader LiveTab = new TabHeader() { Name = "Live sound" };

        private PivotItem selectedTab;
        public PivotItem SelectedTab
        {
            get => selectedTab;
            set
            {
                if (SetProperty(ref selectedTab, value))
                {
                    TabChanged(SelectedTab);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public AnomalyDetectorDemo()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(SettingsHelper.Instance.AnomalyDetectorApiKey))
                {
                    this.pivot.IsEnabled = false;
                    await new MessageDialog("Missing Anomaly Detector Key. Please enter the key in the Settings page.", "Missing API Key").ShowAsync();
                }
                else
                {
                    this.pivot.IsEnabled = true;
                    await CheckMicrophoneAccessAsync();

                    if (AnomalyDetectorScenarioLoader.AllModelData == null || !AnomalyDetectorScenarioLoader.AllModelData.Any())
                    {
                        await AnomalyDetectorScenarioLoader.InitUserStories();
                    }

                    // initialize default tab
                    this.bikerentalChart.ResetState();
                    this.bikerentalChart.InitializeChart(AnomalyDetectionScenarioType.BikeRental, AnomalyDetectorServiceType.Streaming, sensitivy: 80);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure during navigation to Anomaly Detector Demo.");
            }

            base.OnNavigatedTo(e);
        }

        private async Task CheckMicrophoneAccessAsync()
        {
            try
            {
                await InitMicrophoneAsync();
                this.livePivotItem.IsEnabled = true;
            }
            catch (Exception ex)
            {
                this.livePivotItem.IsEnabled = false;
                await Util.GenericApiCallExceptionHandler(ex, "Failure during initializing Microphone device. Live demo is disabled.");
            }
        }

        private void TabChanged(PivotItem selectedPivot)
        {
            bool isAnyData = AnomalyDetectorScenarioLoader.AllModelData != null && AnomalyDetectorScenarioLoader.AllModelData.Any();
            if (selectedPivot.Header is TabHeader selectedTab && isAnyData)
            {
                // clear chart
                this.bikerentalChart.ResetState();
                this.telecomChart.ResetState();
                this.manufacturingChart.ResetState();
                this.liveChart.ResetState();

                // initialize new chart
                if (selectedTab == BikeRentalTab)
                {
                    this.bikerentalChart.InitializeChart(AnomalyDetectionScenarioType.BikeRental, AnomalyDetectorServiceType.Streaming, sensitivy: 80);
                }

                else if (selectedTab == TelecomTab)
                {
                    this.telecomChart.InitializeChart(AnomalyDetectionScenarioType.Telecom, AnomalyDetectorServiceType.Streaming, sensitivy: 85);
                }

                else if (selectedTab == ManufacturingTab)
                {
                    this.manufacturingChart.InitializeChart(AnomalyDetectionScenarioType.Manufacturing, AnomalyDetectorServiceType.Streaming, sensitivy: 80);
                }

                else if (selectedTab == LiveTab)
                {
                    this.liveChart.InitializeChart(AnomalyDetectionScenarioType.Live, AnomalyDetectorServiceType.Streaming, sensitivy: 80);
                }
            }
        }

        /// <summary>
        /// Process audio frames with MediaFrameReader: 
        /// https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-audio-frames-with-mediaframereader
        /// </summary>
        /// <returns></returns>
        private async Task InitMicrophoneAsync()
        {
            mediaCapture = new MediaCapture();
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings()
            {
                StreamingCaptureMode = StreamingCaptureMode.Audio,
            };
            await mediaCapture.InitializeAsync(settings);

            var audioFrameSources = mediaCapture.FrameSources.Where(x => x.Value.Info.MediaStreamType == MediaStreamType.Audio);
            if (audioFrameSources.Count() == 0)
            {
                throw new Exception("No audio frame source was found.");
            }

            MediaFrameSource frameSource = audioFrameSources.FirstOrDefault().Value;
            MediaFrameFormat format = frameSource.CurrentFormat;

            if (format.Subtype != MediaEncodingSubtypes.Float)
            {
                throw new Exception($"MediaFrameSource.Subtype is {format.Subtype} and NOT expected.");
            }

            if (format.AudioEncodingProperties.ChannelCount <= 0)
            {
                throw new Exception($"AudioEncodingProperties.ChannelCount is 0 and NOT expected.");
            }

            mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(frameSource);
            mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Buffered;
            mediaFrameReader.FrameArrived += MediaFrameReader_AudioFrameArrived;
        }

        private void MediaFrameReader_AudioFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (MediaFrameReference reference = sender.TryAcquireLatestFrame())
            {
                if (reference != null)
                {
                    ProcessAudioFrame(reference.AudioMediaFrame);
                }
            }
        }

        unsafe private void ProcessAudioFrame(AudioMediaFrame audioMediaFrame)
        {
            using (AudioFrame audioFrame = audioMediaFrame.GetAudioFrame())
            using (AudioBuffer buffer = audioFrame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacityInBytes);

                // The requested format was float
                float* dataInFloat = (float*)dataInBytes;

                // Duration can be gotten off the frame reference OR the audioFrame
                TimeSpan duration = audioMediaFrame.FrameReference.Duration;

                // frameDurMs is in milliseconds, while SampleRate is given per second.
                uint frameDurMs = (uint)duration.TotalMilliseconds;
                uint sampleRate = audioMediaFrame.AudioEncodingProperties.SampleRate;
                uint sampleCount = (frameDurMs * sampleRate) / 1000;

                maxVolumeInSampleBuffer = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    maxVolumeInSampleBuffer = Math.Max(maxVolumeInSampleBuffer, dataInFloat[i]);
                }

                // update volume value
                this.liveChart.SetVolumeValue(maxVolumeInSampleBuffer);
            }
        }

        private async void OnStartLiveAudio(object sender, EventArgs e)
        {
            if (mediaFrameReader != null)
            {
                MediaFrameReaderStartStatus status = await mediaFrameReader.StartAsync();
                if (status != MediaFrameReaderStartStatus.Success)
                {
                    throw new Exception("The MediaFrameReader couldn't start.");
                }
            }
        }

        private async void OnStopLiveAudio(object sender, EventArgs e)
        {
            if (mediaFrameReader != null)
            {
                await mediaFrameReader.StopAsync();
            }
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}