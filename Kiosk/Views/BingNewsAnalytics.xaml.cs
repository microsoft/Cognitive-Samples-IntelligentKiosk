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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace IntelligentKioskSample.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [KioskExperience(Title = "Bing News Analytics", ImagePath = "ms-appx:/Assets/BingNewsAnalytics.png", ExperienceType = ExperienceType.Kiosk)]
    public sealed partial class BingNewsAnalyticsPage : Page
    {
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
            EnterKioskMode();

            if (string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsKey))
            {
                await new MessageDialog("Missing Text Analytics Key. Please enter the key in the Settings page.", "Missing API Key").ShowAsync();
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

                string userLanguage = this.languageComboBox.SelectedValue.ToString();

                var news = await BingSearchHelper.GetNewsSearchResults(query, count: 50, offset:0, market: GetBingSearchMarketFromLanguage(userLanguage));

                Task<SentimentResult> sentimentTask = TextAnalyticsHelper.GetTextSentimentAsync(news.Select(n => n.Title).ToArray(), language: GetTextAnalyticsLanguageCodeFromLanguage(userLanguage));

                Task<KeyPhrasesResult> keyPhrasesTask;
                if (IsLanguageSupportedByKeyPhraseAPI(userLanguage))
                {
                    keyPhrasesTask = TextAnalyticsHelper.GetKeyPhrasesAsync(news.Select(n => n.Title).ToArray(), language: GetTextAnalyticsLanguageCodeFromLanguage(userLanguage));
                }
                else
                {
                    keyPhrasesTask = Task.FromResult(new KeyPhrasesResult { KeyPhrases = new string[][] { new string [] { "Not available in this language" } } });
                }

                await Task.WhenAll(sentimentTask, keyPhrasesTask);

                var sentiment = sentimentTask.Result;

                for (int i = 0; i < news.Count(); i++)
                {
                    NewsArticle article = news.ElementAt(i);
                    this.latestSearchResult.Add(new NewsAndSentimentScore { Article = article, TitleSentiment = Math.Round(sentiment.Scores.ElementAt(i), 2) });
                }

                UpdateFilteredResults();

                this.sentimentDistributionControl.UpdateData(this.latestSearchResult.Select(n => n.TitleSentiment));

                var wordGroups = keyPhrasesTask.Result.KeyPhrases.SelectMany(k => k).GroupBy(phrase => phrase, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).Take(10).OrderBy(g => g.Key);
                this.TopKeyPhrases.AddRange(wordGroups.Select(g => new KeyPhraseCount { KeyPhrase = g.Key, Count = g.Count() }));
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

        private static string GetBingSearchMarketFromLanguage(string language)
        {
            switch (language)
            {
                case "English": return "en-US";
                case "Spanish": return "es-MX";
                case "French": return "fr-FR";
                case "Portuguese": return "pt-BR";
                default:
                    return "en-US";
            }
        }

        private static string GetTextAnalyticsLanguageCodeFromLanguage(string language)
        {
            switch (language)
            {
                case "English": return "en";
                case "Spanish": return "es";
                case "French": return "fr";
                case "Portuguese": return "pt";
                default:
                    return "en";
            }
        }

        private static bool IsLanguageSupportedByKeyPhraseAPI(string language)
        {
            switch (language)
            {
                case "English": 
                case "Spanish": 
                case "German": 
                case "Japanese":
                    return true;
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

    public class SentimentToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double score = (double)value;

            // Linear gradient function, from a red 0x99 when score = 0 to a green 0x77 when score = 1.
            return new SolidColorBrush(Color.FromArgb(0xff, (byte) (0x99 - (score * 0x99)), (byte) (score * 0x77), 0));
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
