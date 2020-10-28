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

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Views.SpeechToText
{
    public sealed partial class SpeechToTextView : UserControl
    {
        private SpeechRecognizer speechRecognizer;

        private StringBuilder dictatedTextBuilder = new StringBuilder();
        private RecognitionData recognitionData;
        private CancellationTokenSource recognizeCancellationTokenSource;
        private TaskCompletionSource<int> stopRecognitionTaskCompletionSource;

        public EventHandler<NotificationViewModel> ShowNotificationEventHandler;

        public SpeechToTextView()
        {
            this.InitializeComponent();
        }

        public RecognitionData GetRecognizedData()
        {
            return this.recognitionData;
        }

        public void SetRecognitionData(RecognitionData recognitionData, string tempText = "")
        {
            this.dictationTextBlock.Text = recognitionData.Text;
            if (!string.IsNullOrEmpty(tempText))
            {
                var runText = new Run()
                {
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)),
                    Text = tempText
                };
                this.dictationTextBlock.Inlines.Add(runText);
            }

            if (recognitionData.HighlightStyle)
            {
                this.dictationTextBlock.FontStyle = Windows.UI.Text.FontStyle.Italic;
                this.dictationTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        public async Task StartSpeechRecognitionAsync()
        {
            SpeechConfig config = GetRecognizerConfig();
            if (config == null)
            {
                return;
            }
            ResetState();
            DisposeRecognizer();

            DeviceInformation microphoneInput = await Util.GetDeviceInformation(DeviceClass.AudioCapture, SettingsHelper.Instance.MicrophoneName);
            using (AudioConfig audioConfig = AudioConfig.FromMicrophoneInput(microphoneInput.Id))
            {
                speechRecognizer = audioConfig != null ? new SpeechRecognizer(config, audioConfig) : new SpeechRecognizer(config);
                speechRecognizer.Recognizing += OnRecognizing;
                speechRecognizer.Recognized += OnRecognized;
                speechRecognizer.Canceled += OnCanceled;
                speechRecognizer.SessionStarted += (s, e) =>
                {
                    recognizeCancellationTokenSource = new CancellationTokenSource();
                };

                await speechRecognizer.StartContinuousRecognitionAsync();
            }
        }

        public async Task StopSpeechRecognitionAsync()
        {
            if (speechRecognizer != null)
            {
                if (recognizeCancellationTokenSource != null && recognizeCancellationTokenSource.Token.CanBeCanceled)
                {
                    recognizeCancellationTokenSource.Cancel();
                }

                await speechRecognizer.StopContinuousRecognitionAsync();
                DisposeRecognizer();
            }
        }

        public async Task SpeechRecognitionFromFileAsync(StorageFile file)
        {
            SpeechConfig config = GetRecognizerConfig();
            if (config == null)
            {
                return;
            }

            ResetState();
            stopRecognitionTaskCompletionSource = new TaskCompletionSource<int>();
            using (var audioInput = AudioConfig.FromWavFileInput(file.Path))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    recognizer.Recognizing += OnRecognizing;
                    recognizer.Recognized += OnRecognized;
                    recognizer.Canceled += OnCanceled;
                    recognizer.SessionStarted += (s, e) =>
                    {
                        recognizeCancellationTokenSource = new CancellationTokenSource();
                    };
                    recognizer.SessionStopped += (s, e) =>
                    {
                        if (recognizeCancellationTokenSource != null && recognizeCancellationTokenSource.Token.CanBeCanceled)
                        {
                            recognizeCancellationTokenSource.Cancel();
                        }
                        stopRecognitionTaskCompletionSource.TrySetResult(0);
                    };

                    // Starts continuous recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                    // Waits for completion.
                    await stopRecognitionTaskCompletionSource.Task.ConfigureAwait(false);
                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
        }

        private SpeechConfig GetRecognizerConfig()
        {
            if (!(this.inputLanguageCombobox.SelectedItem is SupportedLanguage language))
            {
                return null;
            }
            var speechConfig = SpeechConfig.FromEndpoint(new Uri(SettingsHelper.Instance.SpeechApiEndpoint), SettingsHelper.Instance.SpeechApiKey);
            speechConfig.SpeechRecognitionLanguage = language.Code ?? "en-US";
            return speechConfig;
        }

        private async void OnRecognizing(object sender, SpeechRecognitionEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Result.Text))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    recognitionData = new RecognitionData(dictatedTextBuilder.ToString(), null);
                    SetRecognitionData(recognitionData, args.Result.Text);
                });
            }
        }

        private async void OnRecognized(object sender, SpeechRecognitionEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Result.Text))
            {
                dictatedTextBuilder.Append(args.Result.Text + "\n");
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    recognitionData = new RecognitionData(dictatedTextBuilder.ToString(), this.inputLanguageCombobox.SelectedItem as SupportedLanguage);
                    SetRecognitionData(recognitionData);
                });
            }
        }

        private async void OnCanceled(object sender, SpeechRecognitionCanceledEventArgs args)
        {
            if (args.Reason == CancellationReason.Error)
            {
                stopRecognitionTaskCompletionSource?.TrySetResult(0);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    dictatedTextBuilder = new StringBuilder($"ErrorCode={args.ErrorCode};\nErrorDetails={args.ErrorDetails}");
                    recognitionData = new RecognitionData(dictatedTextBuilder.ToString(), null);
                    SetRecognitionData(recognitionData);

                    ShowNotificationEventHandler?.Invoke(this, new NotificationViewModel { NotificationType = NotificationType.Error });
                });
            }
        }

        private void ResetState()
        {
            dictatedTextBuilder.Clear();
            this.dictationTextBlock.Inlines.Clear();
            this.dictationTextBlock.Text = string.Empty;

            this.dictationTextBlock.FontStyle = Windows.UI.Text.FontStyle.Normal;
            this.dictationTextBlock.Foreground = new SolidColorBrush(Colors.White);
        }

        private void DisposeRecognizer()
        {
            if (speechRecognizer != null)
            {
                speechRecognizer.Recognizing -= OnRecognizing;
                speechRecognizer.Recognized -= OnRecognized;
                speechRecognizer.Canceled -= OnCanceled;
                speechRecognizer = null;
            }
        }

        private void OnCopyTextButtonClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Util.CopyToClipboard(this.dictationTextBlock.Text);
        }
    }
}
