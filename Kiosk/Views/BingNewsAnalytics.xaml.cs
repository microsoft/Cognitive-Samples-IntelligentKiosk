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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "BingNewsAnalytics",
        DisplayName = "Bing News Analytics",
        Description = "View sentiment and extract keywords in today's headlines",
        ImagePath = "ms-appx:/Assets/DemoGallery/Bing News Analytics.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologyArea = TechnologyAreaType.Search | TechnologyAreaType.Language,
        TechnologiesUsed = TechnologyType.BingAutoSuggest | TechnologyType.BingImages | TechnologyType.BingNews | TechnologyType.TextAnalytics,
        DateAdded = "2016/08/01",
        DateUpdated = "2020/09/25",
        UpdatedDescription = "Now supporting more languages")]
    public sealed partial class BingNewsAnalyticsPage : Page
    {
        private static readonly int TotalNewsCount = 50;
        public ObservableCollection<NewsAndSentimentScore> FilteredNewsResults { get; set; } = new ObservableCollection<NewsAndSentimentScore>();
        public ObservableCollection<KeyPhraseCount> TopKeyPhrases { get; set; } = new ObservableCollection<KeyPhraseCount>();
        private List<NewsAndSentimentScore> latestSearchResult = new List<NewsAndSentimentScore>();

        public BingNewsAnalyticsPage()
        {
            this.InitializeComponent();
            this.searchBox.Focus(FocusState.Programmatic);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsKey) || string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsApiKeyEndpoint))
            {
                this.page.IsEnabled = false;
                await new MessageDialog("Missing Text Analytics Key. Please enter the key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                this.page.IsEnabled = true;
            }

            base.OnNavigatedTo(e);
        }

        private async void SearchClicked(object sender, RoutedEventArgs e)
        {
            await SearchAsync(this.searchBox.Text);
        }

        private async Task SearchAsync(string query)
        {
            try
            {
                this.TopKeyPhrases.Clear();
                this.latestSearchResult.Clear();
                this.FilteredNewsResults.Clear();
                this.sentimentDistributionControl.UpdateData(Enumerable.Empty<double>());

                this.progressRing.IsActive = true;

                // get language and bing news titles
                string userLanguage = this.languageComboBox.SelectedValue.ToString();
                IEnumerable<NewsArticle> news = await BingSearchHelper.GetNewsSearchResults(query, count: TotalNewsCount, offset: 0, market: GetBingSearchMarketFromLanguage(userLanguage));

                // analyze news titles
                AnalyzeTextResult analyzedNews = news.Any() ? await AnalyzeNewsAsync(news, userLanguage) : null;
                List<double> scores = analyzedNews?.Scores ?? new List<double>();
                List<KeyPhraseCount> topKeyPhrases = analyzedNews?.TopKeyPhrases ?? new List<KeyPhraseCount>();

                // display result
                for (int i = 0; i < news.Count(); i++)
                {
                    NewsArticle article = news.ElementAt(i);
                    double score = i < scores.Count ? scores.ElementAt(i) : 0.5;
                    this.latestSearchResult.Add(new NewsAndSentimentScore { Article = article, TitleSentiment = Math.Round(score, 2) });
                }

                UpdateFilteredResults();

                this.sentimentDistributionControl.UpdateData(this.latestSearchResult.Select(n => n.TitleSentiment));
                this.TopKeyPhrases.AddRange(topKeyPhrases);
            }
            catch (HttpRequestException ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Processing error");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private async Task<AnalyzeTextResult> AnalyzeNewsAsync(IEnumerable<NewsArticle> news, string userLanguage)
        {
            var scores = new List<double>();
            var topKeyPhrases = new List<KeyPhraseCount>();

            // prepare news titles to text string
            string[] newsTitleList = news.Select(n => n.Title.Replace(".", ";")).ToArray();
            string newsTitles = string.Join(". ", newsTitleList);
            var strInfo = new StringInfo(newsTitles);
            int lenInTextElements = strInfo.LengthInTextElements;

            // check Text Analytics data limits
            string languageCode = GetTextAnalyticsLanguageCodeFromLanguage(userLanguage);
            bool isLangSupportedForKeyPhrases = IsLanguageSupportedByKeyPhraseAPI(userLanguage);
            if (lenInTextElements < TextAnalyticsHelper.MaximumLengthOfTextDocument)
            {
                Task<DocumentSentiment> sentimentTask = TextAnalyticsHelper.AnalyzeSentimentAsync(newsTitles, languageCode);
                Task<KeyPhraseCollection> keyPhrasesTask = isLangSupportedForKeyPhrases ? TextAnalyticsHelper.ExtractKeyPhrasesAsync(newsTitles, languageCode) : Task.FromResult<KeyPhraseCollection>(null);
                await Task.WhenAll(sentimentTask, keyPhrasesTask);

                var sentimentResult = sentimentTask.Result;
                var keyPhrasesResult = keyPhrasesTask.Result;

                scores = sentimentResult.Sentences.Select(s => GetSentimentScore(s)).ToList();
                var wordGroups = keyPhrasesResult?.GroupBy(phrase => phrase, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).Take(10).OrderBy(g => g.Key).ToList();
                topKeyPhrases = wordGroups != null && wordGroups.Any()
                    ? wordGroups.Select(w => new KeyPhraseCount { KeyPhrase = w.Key, Count = w.Count() }).ToList()
                    : new List<KeyPhraseCount>() { new KeyPhraseCount { KeyPhrase = "Not available in this language", Count = 1 } };
            }
            else
            {
                // if the input data is larger than max limit then split the input data into several different requests
                var sentimentTaskList = new List<Task<AnalyzeSentimentResultCollection>>();
                var keyPhrasesTaskList = new List<Task<ExtractKeyPhrasesResultCollection>>();

                int maxDocsPerRequest = Math.Min(TextAnalyticsHelper.MaxDocumentsPerRequestForSentimentAnalysis, TextAnalyticsHelper.MaxDocumentsPerRequestForKeyPhraseExtraction);
                int batchSize = Math.Min((int)Math.Ceiling((decimal)TotalNewsCount / maxDocsPerRequest), maxDocsPerRequest);
                for (int i = 0; i < TextAnalyticsHelper.MaxRequestsPerSecond; i++)
                {
                    int skip = i * batchSize;
                    string[] newsTitlesBatch = newsTitleList.Skip(skip).Take(batchSize).ToArray();
                    if (!newsTitlesBatch.Any())
                    {
                        break;
                    }

                    sentimentTaskList.Add(TextAnalyticsHelper.AnalyzeSentimentAsync(newsTitlesBatch, languageCode));
                    if (isLangSupportedForKeyPhrases)
                    {
                        keyPhrasesTaskList.Add(TextAnalyticsHelper.ExtractKeyPhrasesAsync(newsTitlesBatch, languageCode));
                    }
                }

                var taskList = new List<Task>();
                taskList.AddRange(sentimentTaskList);
                taskList.AddRange(keyPhrasesTaskList);
                await Task.WhenAll(taskList);

                foreach (var sentimentTask in sentimentTaskList)
                {
                    AnalyzeSentimentResultCollection sentimentResult = sentimentTask.Result;
                    scores.AddRange(sentimentResult.SelectMany(d => d.DocumentSentiment.Sentences).Select(s => GetSentimentScore(s)).ToList());
                }

                var keyPhrasesList = new List<string>();
                foreach (var keyPhrasesTask in keyPhrasesTaskList)
                {
                    ExtractKeyPhrasesResultCollection keyPhrasesResult = keyPhrasesTask.Result;
                    keyPhrasesList.AddRange(keyPhrasesResult.SelectMany(k => k.KeyPhrases));
                }

                var wordGroups = keyPhrasesList.GroupBy(phrase => phrase, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).Take(10).OrderBy(g => g.Key).ToList();
                topKeyPhrases = wordGroups.Any()
                    ? wordGroups.Select(w => new KeyPhraseCount { KeyPhrase = w.Key, Count = w.Count() }).ToList()
                    : new List<KeyPhraseCount>() { new KeyPhraseCount { KeyPhrase = "Not available in this language", Count = 1 } };
            }

            return new AnalyzeTextResult
            {
                Scores = scores,
                TopKeyPhrases = topKeyPhrases
            };
        }

        private double GetSentimentScore(SentenceSentiment sentenceSentiment)
        {
            switch (sentenceSentiment.Sentiment)
            {
                case TextSentiment.Positive:
                    return sentenceSentiment.ConfidenceScores.Positive;
                case TextSentiment.Neutral:
                    return sentenceSentiment.ConfidenceScores.Neutral;
                case TextSentiment.Negative:
                    return 1 - sentenceSentiment.ConfidenceScores.Negative;
                case TextSentiment.Mixed:
                default:
                    return 0.5;
            }
        }

        private static string GetBingSearchMarketFromLanguage(string language)
        {
            switch (language)
            {
                case "English": return "en-US";
                case "French": return "fr-FR";
                case "German": return "de-DE";
                case "Italian": return "it-IT";
                case "Chinese": return "zh-CN";
                case "Japanese": return "ja-JP";
                case "Korean": return "ko-KR";
                case "Norwegian": return "no-NO";
                case "Portuguese": return "pt-BR";
                case "Spanish": return "es-MX";
                case "Turkish": return "tr-TR";
                default:
                    return "en-US";
            }
        }

        /// <summary>
        /// Supported languages for Text Analytics API v3 Sentiment Analysis
        /// See details: https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/language-support?tabs=sentiment-analysis
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        private static string GetTextAnalyticsLanguageCodeFromLanguage(string language)
        {
            switch (language)
            {
                case "English": return "en";
                case "French": return "fr";
                case "German": return "de";
                case "Italian": return "it";
                case "Chinese": return "zh";
                case "Japanese": return "ja";
                case "Korean": return "ko";
                case "Norwegian": return "no";
                case "Portuguese": return "pt";
                case "Spanish": return "es";
                case "Turkish": return "tr";
                default:
                    return "en";
            }
        }

        private static bool IsLanguageSupportedByKeyPhraseAPI(string language)
        {
            switch (language)
            {
                case "English":
                case "French":
                case "German":
                case "Italian":
                case "Japanese":
                case "Korean":
                case "Norwegian":
                case "Portuguese":
                case "Spanish":
                    return true;
                case "Chinese":
                case "Turkish":
                default:
                    return false;
            }
        }

        private void UpdateFilteredResults()
        {
            this.FilteredNewsResults.Clear();

            if (this.sortBySentimentToggle.IsOn)
            {
                this.FilteredNewsResults.AddRange(this.latestSearchResult.OrderByDescending(n => n.TitleSentiment));
            }
            else
            {
                this.FilteredNewsResults.AddRange(this.latestSearchResult);
            }
        }

        private async void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                await SearchAsync(args.ChosenSuggestion.ToString());
            }
            else
            {
                await SearchAsync(args.QueryText);
            }

            sender.IsSuggestionListOpen = false;
        }

        private async void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try
                {
                    this.searchBox.ItemsSource = await BingSearchHelper.GetAutoSuggestResults(this.searchBox.Text, GetBingSearchMarketFromLanguage(this.languageComboBox.SelectedValue.ToString()));
                }
                catch (HttpRequestException)
                {
                    // default to no suggestions
                    this.searchBox.ItemsSource = null;
                }
            }
        }

        private void SortBySentimentToggleChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateFilteredResults();
        }

        private async void LanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.searchBox.Text))
            {
                await SearchAsync(this.searchBox.Text);
            }
        }
    }

    public class NewsAndSentimentScore
    {
        public NewsArticle Article { get; set; }
        public double TitleSentiment { get; set; }
        public IEnumerable<string> KeyPhrases { get; set; }
    }

    public class KeyPhraseCount
    {
        public string KeyPhrase { get; set; }
        public int Count { get; set; }
    }

    public class AnalyzeTextResult
    {
        public List<double> Scores { get; set; }
        public List<KeyPhraseCount> TopKeyPhrases { get; set; }
    }

    public class SentimentToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double score = (double)value;

            // Linear gradient function, from a red 0x99 when score = 0 to a green 0x77 when score = 1.
            return new SolidColorBrush(Color.FromArgb(0xff, (byte)(0x99 - (score * 0x99)), (byte)(score * 0x77), 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // not used
            return 0.5;
        }
    }

    public class WordCountToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return 10 + Math.Min(36, (int)value * 1.5);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // not used
            return 0;
        }
    }
}
