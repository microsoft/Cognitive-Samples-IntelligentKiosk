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

using Azure.AI.TextAnalytics;
using ServiceHelpers;
using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Controls
{
    public class SpeechRecognitionAndSentimentResult
    {
        public string SpeechRecognitionText { get; set; }
        public double TextAnalysisSentiment { get; set; }
    }

    public sealed partial class SpeechToTextControl : UserControl
    {
        public event EventHandler<SpeechRecognitionAndSentimentResult> SpeechRecognitionAndSentimentProcessed;

        private SpeechRecognizer speechRecognizer;
        private bool isCapturingSpeech;
        private static uint HResultPrivacyStatementDeclined = 0x80045509;

        // Keep track of existing text that we've accepted in ContinuousRecognitionSession_ResultGenerated(), so
        // that we can combine it and Hypothesized results to show in-progress dictation mid-sentence.
        private StringBuilder dictatedTextBuilder;

        public SpeechToTextControl()
        {
            this.InitializeComponent();
        }

        #region Speech Recognizer and Text Analytics

        public async Task InitializeSpeechRecognizerAsync()
        {
            if (this.speechRecognizer != null)
            {
                this.DisposeSpeechRecognizer();
            }

            this.dictatedTextBuilder = new StringBuilder();
            this.speechRecognizer = new SpeechRecognizer();

            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();

            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                await new MessageDialog("CompileConstraintsAsync returned " + result.Status, "Error initializing SpeechRecognizer").ShowAsync();
                return;
            }

            this.speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated; ;
            this.speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
            this.speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High)
            {
                dictatedTextBuilder.Append(args.Result.Text + " ");

                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.speechRecognitionTextBox.Text = dictatedTextBuilder.ToString();
                });
            }
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                // If TimeoutExceeded occurs, the user has been silent for too long. We can use this to 
                // cancel recognition if the user in dictation mode and walks away from their device, etc.
                // In a global-command type scenario, this timeout won't apply automatically.
                // With dictation (no grammar in place) modes, the default timeout is 20 seconds.
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.speechRecognitionControlButtonSymbol.Symbol = Symbol.Refresh;
                        this.speechRecognitionTextBox.PlaceholderText = "";
                        this.speechRecognitionTextBox.Text = dictatedTextBuilder.ToString();
                        this.isCapturingSpeech = false;
                    });
                }
                else
                {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.speechRecognitionControlButtonSymbol.Symbol = Symbol.Refresh;
                        this.speechRecognitionTextBox.PlaceholderText = "";
                        this.isCapturingSpeech = false;
                    });
                }
            }
        }

        public void DisposeSpeechRecognizer()
        {
            if (this.speechRecognizer != null)
            {
                try
                {
                    this.speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
                    this.speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                    this.speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated;
                    this.speechRecognizer.Dispose();
                    this.speechRecognizer = null;
                }
                catch (Exception) { }
            }
        }

        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            string hypothesis = args.Hypothesis.Text;

            // Update the textbox with the currently confirmed text, and the hypothesis combined.
            string textboxContent = dictatedTextBuilder.ToString() + " " + hypothesis + " ...";

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.speechRecognitionTextBox.Text = textboxContent;
            });
        }

        private async void OnSpeechRecognitionFlyoutOpened(object sender, object e)
        {
            try
            {
                this.speechRecognitionControlButton.Focus(FocusState.Programmatic);
                await StartSpeechRecognition();
            }
            catch (Exception ex) when ((uint)ex.HResult == HResultPrivacyStatementDeclined)
            {
                await Util.ConfirmActionAndExecute(
                    "The Speech Privacy settings need to be enabled. Under 'Settings->Privacy->Speech', ensure you have enabled 'Online speech recognition'. Want to open the settings now?",
                    async () =>
                    {
                        // Open the privacy/speech settings page.
                        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-speech"));
                    });
            }
            catch (UnauthorizedAccessException)
            {
                await new DeviceAccessErrorDialog() { DeviceName = "microphone" }.ShowAsync();
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error starting SpeechRecognizer.");
            }
        }

        private async void OnSpeechRecognitionFlyoutClosed(object sender, object e)
        {
            try
            {
                if (this.speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    await speechRecognizer.ContinuousRecognitionSession.StopAsync();
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task StartSpeechRecognition()
        {
            this.isCapturingSpeech = true;
            this.speechRecognitionControlButtonSymbol.Symbol = Symbol.Stop;

            if (this.speechRecognizer == null)
            {
                await this.InitializeSpeechRecognizerAsync();

            }

            this.speechRecognitionTextBox.Text = "";
            this.speechRecognitionTextBox.PlaceholderText = "Listening...";

            this.dictatedTextBuilder.Clear();
            this.sentimentControl.Sentiment = 0.5;

            await this.speechRecognizer.ContinuousRecognitionSession.StartAsync();
        }

        private async void SpeechRecognitionButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.isCapturingSpeech)
            {
                this.isCapturingSpeech = false;
                this.speechRecognitionControlButtonSymbol.Symbol = Symbol.Refresh;
                this.speechRecognitionTextBox.PlaceholderText = "";

                if (speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    // Cancelling recognition prevents any currently recognized speech from
                    // generating a ResultGenerated event. StopAsync() will allow the final session to 
                    // complete.
                    try
                    {
                        await speechRecognizer.ContinuousRecognitionSession.StopAsync();
                        string dictatedTextAfterStop = dictatedTextBuilder.ToString();

                        // Ensure we don't leave any hypothesis text behind
                        if (!string.IsNullOrEmpty(dictatedTextAfterStop))
                        {
                            this.speechRecognitionTextBox.Text = dictatedTextAfterStop;
                        }
                        else if (!string.IsNullOrEmpty(this.speechRecognitionTextBox.Text) && this.speechRecognitionTextBox.Text.EndsWith(" ..."))
                        {
                            this.speechRecognitionTextBox.Text = this.speechRecognitionTextBox.Text.Replace(" ...", ".");
                        }
                    }
                    catch (Exception exception)
                    {
                        await Util.GenericApiCallExceptionHandler(exception, "Error stopping SpeechRecognizer.");
                    }
                }

                await this.AnalyzeTextAsync();
            }
            else
            {
                await this.StartSpeechRecognition();
            }
        }

        private async Task AnalyzeTextAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.speechRecognitionTextBox.Text))
                {
                    DocumentSentiment textAnalysisResult = await TextAnalyticsHelper.AnalyzeSentimentAsync(this.speechRecognitionTextBox.Text);
                    this.sentimentControl.Sentiment = GetSentimentScore(textAnalysisResult);
                }
                else
                {
                    this.sentimentControl.Sentiment = 0.5;
                }

                this.OnSpeechRecognitionAndSentimentProcessed(new SpeechRecognitionAndSentimentResult { SpeechRecognitionText = this.speechRecognitionTextBox.Text, TextAnalysisSentiment = this.sentimentControl.Sentiment });
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error during Text Analytics call.");
            }
        }

        private double GetSentimentScore(DocumentSentiment sentiment)
        {
            switch (sentiment.Sentiment)
            {
                case TextSentiment.Positive:
                    return sentiment.ConfidenceScores.Positive;
                case TextSentiment.Neutral:
                    return sentiment.ConfidenceScores.Neutral;
                case TextSentiment.Negative:
                    return 1 - sentiment.ConfidenceScores.Negative;
                case TextSentiment.Mixed:
                default:
                    return 0.5;
            }
        }

        private void OnSpeechRecognitionAndSentimentProcessed(SpeechRecognitionAndSentimentResult result)
        {
            this.SpeechRecognitionAndSentimentProcessed?.Invoke(this, result);
        }

        #endregion

    }
}
