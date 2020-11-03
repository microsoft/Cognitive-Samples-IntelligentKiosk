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
using IntelligentKioskSample.Controls;
using Newtonsoft.Json;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.TextAnalyticsExplorer
{
    [KioskExperience(Id = "TextAnalyticsExplorer",
        DisplayName = "Text Analytics Explorer",
        Description = "See how Text Analytics API extracts insights from text",
        ImagePath = "ms-appx:/Assets/DemoGallery/Text Analytics Explorer.jpeg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.TextAnalytics,
        TechnologyArea = TechnologyAreaType.Language,
        DateAdded = "2020/09/17")]
    public sealed partial class TextAnalyticsExplorer : Page
    {
        private static readonly Color PositiveColor = Color.FromArgb(255, 137, 196, 2); // #89c402
        private static readonly Color NeutralColor = Color.FromArgb(255, 0, 120, 212);  // #0078d4
        private static readonly Color NegativeColor = Color.FromArgb(255, 165, 20, 25); // #a51419

        public ObservableCollection<MinedOpinion> OpinionMiningCollection { get; set; } = new ObservableCollection<MinedOpinion>();
        public ObservableCollection<SampleText> SampleTextList { get; set; } = new ObservableCollection<SampleText>();

        public TextAnalyticsExplorer()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsKey) || string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsApiKeyEndpoint))
            {
                this.mainPage.IsEnabled = false;
                await new MessageDialog("Missing Text Analytics Key. Please enter the key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                bool isNotAvailableRegion = TextAnalyticsHelper.NotAvailableAzureRegions.Any(r => SettingsHelper.Instance.TextAnalyticsApiKeyEndpoint.Contains(r, StringComparison.OrdinalIgnoreCase));
                if (isNotAvailableRegion)
                {
                    this.mainPage.IsEnabled = false;
                    await new MessageDialog("Text Analytics API v3 is not available in the following regions: China North 2, China East. Please change your Text Analytics key and region in the Settings page to a supported region.", "API key not supported").ShowAsync();
                }
                else
                {
                    this.mainPage.IsEnabled = true;
                    SampleTextList.AddRange(TextAnalyticsDataLoader.GetTextSamples());
                    if (SampleTextList.Any())
                    {
                        this.sampleTextComboBox.SelectedIndex = 0;
                        await AnalyzeTextAsync();
                    }
                }
            }

            base.OnNavigatedTo(e);
        }

        private void OnInputTextSelectionChanged(object sender, RoutedEventArgs e)
        {
            int inputTextLength = this.inputText.Text.Length;
            this.inputTextLength.Text = inputTextLength.ToString();
        }

        private void OnSampleTextComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.sampleTextComboBox.SelectedItem is SampleText sample)
            {
                this.inputText.Text = sample.Text;
                this.inputTextLength.Text = sample.Text.Length.ToString();
            }
        }

        private async void OnAnalyzeButtonClicked(object sender, RoutedEventArgs e)
        {
            await AnalyzeTextAsync();
        }

        private async Task AnalyzeTextAsync()
        {
            try
            {
                this.progressControl.IsActive = true;
                DisplayProcessingUI();

                string input = this.inputText.Text;
                var detectedLanguageTask = TextAnalyticsHelper.DetectLanguageAsync(input);
                var detectedKeyPhrasesTask = TextAnalyticsHelper.ExtractKeyPhrasesAsync(input);
                var documentSentimentTask = TextAnalyticsHelper.AnalyzeSentimentAsync(input, sentimentAnalyses: AdditionalSentimentAnalyses.OpinionMining);
                var namedEntitiesResponseTask = TextAnalyticsHelper.RecognizeEntitiesAsync(input);
                var linkedEntitiesResponseTask = TextAnalyticsHelper.RecognizeLinkedEntitiesAsync(input);

                await Task.WhenAll(detectedLanguageTask, detectedKeyPhrasesTask, documentSentimentTask, namedEntitiesResponseTask, linkedEntitiesResponseTask);
                var detectedLanguage = detectedLanguageTask.Result;
                var detectedKeyPhrases = detectedKeyPhrasesTask.Result;
                var documentSentiment = documentSentimentTask.Result;
                var namedEntitiesResponse = namedEntitiesResponseTask.Result;
                var linkedEntitiesResponse = linkedEntitiesResponseTask.Result;

                // detected language and key phrases
                this.detectedLangTextBlock.Text = !string.IsNullOrEmpty(detectedLanguage.Name) ? $"{detectedLanguage.Name} (confidence: {(int)(detectedLanguage.ConfidenceScore * 100)}%)" : "Not found";
                this.detectedKeyPhrasesTextBlock.Text = detectedKeyPhrases.Any() ? string.Join(", ", detectedKeyPhrases) : "Not found";

                // document sentiment
                CreateSentimentChart(documentSentiment);

                // mined opinions
                OpinionMiningCollection.Clear();
                var minedOpinions = documentSentiment?.Sentences.SelectMany(s => s.MinedOpinions);
                if (minedOpinions != null && minedOpinions.Any())
                {
                    var minedOpinionList = minedOpinions.Select(om => new MinedOpinion()
                    {
                        Aspect = om.Aspect.Text,
                        Opinions = string.Join(", ", om.Opinions.Select(o => $"{o.Text} ({o.Sentiment.ToString("G")})"))
                    });
                    OpinionMiningCollection.AddRange(minedOpinionList);
                }

                // entities
                if (namedEntitiesResponse.Any())
                {
                    this.namesEntitiesGridView.ItemsSource = namedEntitiesResponse.Select(x => new { x.Text, Category = $"[{x.Category}]" });
                }
                else
                {
                    this.namesEntitiesGridView.ItemsSource = new[] { new { Text = "No entities" } };
                }

                // linked entities
                if (linkedEntitiesResponse.Any())
                {
                    this.linkedEntitiesGridView.ItemsSource = linkedEntitiesResponse.Select(x => new { Name = $"{x.Name} ({x.DataSource})", x.Url });
                }
                else
                {
                    this.linkedEntitiesGridView.ItemsSource = new[] { new { Name = "No linked entities" } };
                }

                // prepare json result
                var jsonResult = new
                {
                    LanguageDetection = detectedLanguage,
                    KeyPhrases = detectedKeyPhrases,
                    Sentiment = documentSentiment,
                    Entities = namedEntitiesResponse,
                    EntityLinking = linkedEntitiesResponse
                };
                this.jsonResultTextBlock.Text = JsonConvert.SerializeObject(jsonResult, Formatting.Indented);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure analyzing text");
            }
            finally
            {
                this.progressControl.IsActive = false;
            }
        }

        private void CreateSentimentChart(DocumentSentiment documentSentiment)
        {
            this.sentimentChart.Visibility = Visibility.Visible;
            this.sentimentChart.Title = $"Document sentiment: {documentSentiment.Sentiment}";
            var data = new List<ChartItem>()
            {
                new ChartItem()
                {
                    Name = "Positive",
                    Value = documentSentiment.ConfidenceScores.Positive,
                    Background = new SolidColorBrush(PositiveColor)
                },
                new ChartItem()
                {
                    Name = "Neutral",
                    Value = documentSentiment.ConfidenceScores.Neutral,
                    Background = new SolidColorBrush(NeutralColor)
                },
                new ChartItem()
                {
                    Name = "Negative",
                    Value = documentSentiment.ConfidenceScores.Negative,
                    Background = new SolidColorBrush(NegativeColor),
                }
            };
            this.sentimentChart.GenerateChart(data);
        }

        private void DisplayProcessingUI()
        {
            string label = "Analyzing...";
            this.detectedLangTextBlock.Text = label;
            this.detectedKeyPhrasesTextBlock.Text = label;
            this.sentimentChart.Visibility = Visibility.Collapsed;
            this.sentimentTextBlock.Text = label;
            this.namesEntitiesGridView.ItemsSource = new[] { new { Text = label } };
            this.linkedEntitiesGridView.ItemsSource = new[] { new { Name = label } };
        }
    }

    public class MinedOpinion
    {
        public string Aspect { get; set; }
        public string Opinions { get; set; }
    }
}
