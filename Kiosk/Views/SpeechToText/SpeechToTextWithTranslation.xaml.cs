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
using Microsoft.CognitiveServices.Speech.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Views.SpeechToText
{
    public sealed partial class SpeechToTextWithTranslation : UserControl
    {
        private StringBuilder dictatedTextBuilder = new StringBuilder();
        private StringBuilder firstTranslatedTextBuilder = new StringBuilder();
        private StringBuilder secondTranslatedTextBuilder = new StringBuilder();

        private RecognitionData recognitionData;
        private RecognitionData firstTranslationRecognitionData;
        private RecognitionData secondTranslationRecognitionData;

        private TranslationRecognizer translationRecognizer;
        private CancellationTokenSource recognizeCancellationTokenSource;
        private TaskCompletionSource<int> stopRecognitionTaskCompletionSource;

        public EventHandler<NotificationViewModel> ShowNotificationEventHandler;
        public EventHandler Closed;

        public SpeechToTextWithTranslation()
        {
            this.InitializeComponent();
        }

        public RecognitionData GetRecognizedData()
        {
            return this.recognitionData;
        }

        public RecognitionData GetFirstTranslatedText()
        {
            return this.firstTranslationRecognitionData;
        }

        public RecognitionData GetSecondTranslatedText()
        {
            return this.secondTranslationRecognitionData;
        }

        public void SetRecognitionData(RecognitionData inputData, TranslationRecognitionData outputData)
        {
            if (inputData != null)
            {
                UpdateTextBox(this.dictationTextBlock, inputData);
            }

            if (outputData?.FirstTranslationText != null)
            {
                UpdateTextBox(this.firstTranslationTextBlock, outputData.FirstTranslationText);
            }

            if (outputData?.SecondTranslationText != null)
            {
                UpdateTextBox(this.secondTranslationTextBlock, outputData.SecondTranslationText);
            }
        }

        public void UpdateTranslationView()
        {
            this.firstTranslateLanguageGrid.Visibility = this.firstTranslateLanguageCombobox.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
            mainGrid.ColumnDefinitions[1].Width = this.firstTranslateLanguageCombobox.SelectedItem != null ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

            this.secondTranslateLanguageGrid.Visibility = this.secondTranslateLanguageCombobox.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
            mainGrid.ColumnDefinitions[2].Width = this.secondTranslateLanguageCombobox.SelectedItem != null ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

            if (this.firstTranslateLanguageCombobox.SelectedItem == null && this.secondTranslateLanguageCombobox.SelectedItem == null)
            {
                this.Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        private SpeechTranslationConfig GetRecognizerConfig()
        {
            var translationLanguageCodes = new List<string>();
            if (this.firstTranslateLanguageCombobox.SelectedValue is SupportedLanguage firstLanguage)
            {
                translationLanguageCodes.Add(firstLanguage.Code);
            }
            if (this.secondTranslateLanguageCombobox.SelectedValue is SupportedLanguage secondLanguage)
            {
                translationLanguageCodes.Add(secondLanguage.Code);
            }
            if (!(this.inputLanguageCombobox.SelectedValue is SupportedLanguage language) || !translationLanguageCodes.Any())
            {
                return null;
            }

            var speechTranslationConfig = SpeechTranslationConfig.FromEndpoint(GetSpeechTranslationEndpoint(SettingsHelper.Instance.SpeechApiEndpoint), SettingsHelper.Instance.SpeechApiKey);
            speechTranslationConfig.SpeechRecognitionLanguage = language.Code;
            foreach (string code in translationLanguageCodes)
            {
                speechTranslationConfig.AddTargetLanguage(code);
            }
            return speechTranslationConfig;
        }

        public async Task StartSpeechRecognitionAsync()
        {
            SpeechTranslationConfig config = GetRecognizerConfig();
            if (config == null)
            {
                return;
            }

            ResetState();
            DisposeRecognizer();

            DeviceInformation microphoneInput = await Util.GetDeviceInformation(DeviceClass.AudioCapture, SettingsHelper.Instance.MicrophoneName);
            using (AudioConfig audioConfig = AudioConfig.FromMicrophoneInput(microphoneInput.Id))
            {
                translationRecognizer = audioConfig != null ? new TranslationRecognizer(config, audioConfig) : new TranslationRecognizer(config);
                translationRecognizer.Recognizing += OnTranslateRecognizing;
                translationRecognizer.Recognized += OnTranslateRecognized;
                translationRecognizer.Canceled += OnTranslateCanceled;
                translationRecognizer.SessionStarted += (s, e) =>
                {
                    recognizeCancellationTokenSource = new CancellationTokenSource();
                };

                await translationRecognizer.StartContinuousRecognitionAsync();
            }
        }

        public async Task StopSpeechRecognitionAsync()
        {
            if (translationRecognizer != null)
            {
                if (recognizeCancellationTokenSource != null && recognizeCancellationTokenSource.Token.CanBeCanceled)
                {
                    recognizeCancellationTokenSource.Cancel();
                }

                await translationRecognizer.StopContinuousRecognitionAsync();
                DisposeRecognizer();
            }
        }

        public async Task SpeechRecognitionFromFileAsync(StorageFile file)
        {
            SpeechTranslationConfig config = GetRecognizerConfig();
            if (config == null)
            {
                return;
            }

            ResetState();
            stopRecognitionTaskCompletionSource = new TaskCompletionSource<int>();
            using (var audioInput = AudioConfig.FromWavFileInput(file.Path))
            {
                using (var recognizer = new TranslationRecognizer(config, audioInput))
                {
                    recognizer.Recognizing += OnTranslateRecognizing;
                    recognizer.Recognized += OnTranslateRecognized;
                    recognizer.Canceled += OnTranslateCanceled;
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

        private async void OnTranslateRecognizing(object sender, TranslationRecognitionEventArgs args)
        {
            if (args.Result.Reason == ResultReason.TranslatingSpeech)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // origin
                    if (!string.IsNullOrEmpty(args.Result.Text))
                    {
                        recognitionData = new RecognitionData(dictatedTextBuilder.ToString(), null);
                        UpdateTextBox(this.dictationTextBlock, recognitionData, args.Result.Text);
                    }

                    // translation
                    var firstLanguage = this.firstTranslateLanguageCombobox.SelectedItem as SupportedLanguage;
                    var secondLanguage = this.secondTranslateLanguageCombobox.SelectedItem as SupportedLanguage;
                    var translations = args.Result.Translations;
                    foreach (var translation in translations)
                    {
                        if (firstLanguage != null && firstLanguage.TranslationCode.Contains(translation.Key) && !string.IsNullOrEmpty(translation.Value))
                        {
                            firstTranslationRecognitionData = new RecognitionData(firstTranslatedTextBuilder.ToString(), null);
                            UpdateTextBox(this.firstTranslationTextBlock, firstTranslationRecognitionData, translation.Value);
                        }

                        if (secondLanguage != null && secondLanguage.TranslationCode.Contains(translation.Key) && !string.IsNullOrEmpty(translation.Value))
                        {
                            secondTranslationRecognitionData = new RecognitionData(secondTranslatedTextBuilder.ToString(), null);
                            UpdateTextBox(this.secondTranslationTextBlock, secondTranslationRecognitionData, translation.Value);
                        }
                    }
                });
            }
        }

        private async void OnTranslateRecognized(object sender, TranslationRecognitionEventArgs args)
        {
            if (args.Result.Reason == ResultReason.TranslatedSpeech)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // origin
                    if (!string.IsNullOrEmpty(args.Result.Text))
                    {
                        dictatedTextBuilder.Append(args.Result.Text + "\n");
                        recognitionData = new RecognitionData(dictatedTextBuilder.ToString(), this.inputLanguageCombobox.SelectedItem as SupportedLanguage);
                        UpdateTextBox(this.dictationTextBlock, recognitionData);
                    }

                    // translation
                    var firstLanguage = this.firstTranslateLanguageCombobox.SelectedItem as SupportedLanguage;
                    var secondLanguage = this.secondTranslateLanguageCombobox.SelectedItem as SupportedLanguage;
                    var translations = args.Result.Translations;
                    foreach (var translation in translations)
                    {
                        if (firstLanguage != null && firstLanguage.TranslationCode.Contains(translation.Key) &&
                            !string.IsNullOrEmpty(translation.Value))
                        {
                            firstTranslatedTextBuilder.Append(translation.Value + "\n");
                            firstTranslationRecognitionData = new RecognitionData(firstTranslatedTextBuilder.ToString(), firstLanguage);
                            UpdateTextBox(this.firstTranslationTextBlock, firstTranslationRecognitionData);
                        }

                        if (secondLanguage != null && secondLanguage.TranslationCode.Contains(translation.Key) &&
                            !string.IsNullOrEmpty(translation.Value))
                        {
                            secondTranslatedTextBuilder.Append(translation.Value + "\n");
                            secondTranslationRecognitionData = new RecognitionData(secondTranslatedTextBuilder.ToString(), secondLanguage);
                            UpdateTextBox(this.secondTranslationTextBlock, secondTranslationRecognitionData);
                        }
                    }
                });
            }
        }

        private async void OnTranslateCanceled(object sender, TranslationRecognitionCanceledEventArgs args)
        {
            if (args.Reason == CancellationReason.Error)
            {
                stopRecognitionTaskCompletionSource?.TrySetResult(0);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    dictatedTextBuilder = new StringBuilder($"ErrorCode={args.ErrorCode};\nErrorDetails={args.ErrorDetails}");
                    recognitionData = new RecognitionData(dictatedTextBuilder.ToString(), null);
                    UpdateTextBox(this.dictationTextBlock, recognitionData);

                    ShowNotificationEventHandler?.Invoke(this, new NotificationViewModel { NotificationType = NotificationType.Error });
                });
            }
        }

        private void UpdateTextBox(TextBlock textBlock, RecognitionData recognitionData, string tempText = "")
        {
            textBlock.Text = recognitionData.Text;
            if (!string.IsNullOrEmpty(tempText))
            {
                var runText = new Run()
                {
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)),
                    Text = tempText
                };
                textBlock.Inlines.Add(runText);
            }

            if (recognitionData.HighlightStyle)
            {
                textBlock.FontStyle = Windows.UI.Text.FontStyle.Italic;
                textBlock.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void OnRemoveFirstTranslateLanguageClicked(object sender, RoutedEventArgs e)
        {
            this.firstTranslateLanguageCombobox.SelectedItem = null;
            this.firstTranslationTextBlock.Text = string.Empty;
            this.firstTranslatedTextBuilder.Clear();
            this.firstTranslationRecognitionData = new RecognitionData();
            UpdateTranslationView();
        }

        private void OnRemoveSecondTranslateLanguageClicked(object sender, RoutedEventArgs e)
        {
            this.secondTranslateLanguageCombobox.SelectedItem = null;
            this.secondTranslationTextBlock.Text = string.Empty;
            this.secondTranslatedTextBuilder.Clear();
            this.secondTranslationRecognitionData = new RecognitionData();
            UpdateTranslationView();
        }

        private void OnCopyTextButtonClicked(object sender, RoutedEventArgs e)
        {
            Util.CopyToClipboard(((HyperlinkButton)sender).Tag as string);
        }

        private void ResetState()
        {
            dictatedTextBuilder.Clear();
            firstTranslatedTextBuilder.Clear();
            secondTranslatedTextBuilder.Clear();

            this.dictationTextBlock.Text = string.Empty;
            this.dictationTextBlock.Inlines.Clear();
            this.dictationTextBlock.FontStyle = Windows.UI.Text.FontStyle.Normal;
            this.dictationTextBlock.Foreground = new SolidColorBrush(Colors.White);

            this.firstTranslationTextBlock.Text = string.Empty;
            this.firstTranslationTextBlock.Inlines.Clear();
            this.firstTranslationTextBlock.FontStyle = Windows.UI.Text.FontStyle.Normal;
            this.firstTranslationTextBlock.Foreground = new SolidColorBrush(Colors.White);

            this.secondTranslationTextBlock.Text = string.Empty;
            this.secondTranslationTextBlock.Inlines.Clear();
            this.secondTranslationTextBlock.FontStyle = Windows.UI.Text.FontStyle.Normal;
            this.secondTranslationTextBlock.Foreground = new SolidColorBrush(Colors.White);
        }

        private void DisposeRecognizer()
        {
            if (translationRecognizer != null)
            {
                translationRecognizer.Recognizing -= OnTranslateRecognizing;
                translationRecognizer.Recognized -= OnTranslateRecognized;
                translationRecognizer.Canceled -= OnTranslateCanceled;
                translationRecognizer = null;
            }
        }

        /// <summary>
        /// Convert Speech-to-Text endpoint to the Speech Translation endpoint
        /// Speech Translation endpoint template : 'wss://{REGION}.s2s.speech.microsoft.com/speech/translation/cognitiveservices/v1'
        /// </summary>
        /// <returns></returns>
        private Uri GetSpeechTranslationEndpoint(string sttEndpoint)
        {
            bool isUri = !string.IsNullOrEmpty(sttEndpoint) ? Uri.IsWellFormedUriString(sttEndpoint, UriKind.Absolute) : false;
            if (!isUri)
            {
                throw new ArgumentException("Invalid endpoint");
            }

            var sttEndpointUri = new Uri(sttEndpoint);
            if (Regex.IsMatch(sttEndpoint, @"wss://(.*).stt.speech.microsoft.com", RegexOptions.IgnoreCase))
            {
                string host = sttEndpointUri.Host.Replace("stt", "s2s");
                return new Uri($"wss://{host}/speech/translation/cognitiveservices/v1");
            }

            return sttEndpointUri;
        }
    }

    public class TranslationRecognitionData
    {
        public RecognitionData FirstTranslationText { get; set; }
        public RecognitionData SecondTranslationText { get; set; }
    }
}
