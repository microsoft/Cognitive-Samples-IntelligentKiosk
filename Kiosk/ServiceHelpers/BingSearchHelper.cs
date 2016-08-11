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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class BingSearchHelper
    {
        private static string ImageSearchEndPoint = "https://bingapis.azure-api.net/api/v5/images/search";
        private static string AutoSuggestionEndPoint = "https://bingapis.azure-api.net/api/v5/Suggestions";
        private static string NewsSearchEndPoint = "https://bingapis.azure-api.net/api/v5/news/search";

        private static HttpClient autoSuggestionClient { get; set; }
        private static HttpClient searchClient { get; set; }

        private static string autoSuggestionApiKey;
        public static string AutoSuggestionApiKey
        {
            get { return autoSuggestionApiKey; }
            set
            {
                var changed = autoSuggestionApiKey != value;
                autoSuggestionApiKey = value;
                if (changed)
                {
                    InitializeBingClients();
                }
            }
        }

        private static string searchApiKey;
        public static string SearchApiKey
        {
            get { return searchApiKey; }
            set
            {
                var changed = searchApiKey != value;
                searchApiKey = value;
                if (changed)
                {
                    InitializeBingClients();
                }
            }
        }

        private static void InitializeBingClients()
        {
            autoSuggestionClient = new HttpClient();
            autoSuggestionClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AutoSuggestionApiKey);

            searchClient = new HttpClient();
            searchClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SearchApiKey);
        }

        public static async Task<IEnumerable<string>> GetImageSearchResults(string query, string imageContent = "Face", int count = 20, int offset = 0)
        {
            List<string> urls = new List<string>();

            var result = await searchClient.GetAsync(string.Format("{0}?q={1}&safeSearch=Strict&imageType=Photo&color=ColorOnly&count={2}&offset={3}{4}", ImageSearchEndPoint, WebUtility.UrlEncode(query), count, offset, string.IsNullOrEmpty(imageContent) ? "" : "&imageContent=" + imageContent));
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);
            if (data.value != null && data.value.Count > 0)
            {
                for (int i = 0; i < data.value.Count; i++)
                {
                    urls.Add(data.value[i].contentUrl.Value);
                }
            }

            return urls;
        }

        public static async Task<IEnumerable<string>> GetAutoSuggestResults(string query, string market = "en-US")
        {
            List<string> suggestions = new List<string>();

            var result = await autoSuggestionClient.GetAsync(string.Format("{0}/?q={1}&mkt={2}", AutoSuggestionEndPoint, WebUtility.UrlEncode(query), market));
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);
            if (data.suggestionGroups != null && data.suggestionGroups.Count > 0 &&
                data.suggestionGroups[0].searchSuggestions != null)
            {
                for (int i = 0; i < data.suggestionGroups[0].searchSuggestions.Count; i++)
                {
                    suggestions.Add(data.suggestionGroups[0].searchSuggestions[i].displayText.Value);
                }
            }

            return suggestions;
        }


        public static async Task<IEnumerable<NewsArticle>> GetNewsSearchResults(string query, int count = 20, int offset = 0, string market = "en-US")
        {
            List<NewsArticle> articles = new List<NewsArticle>();

            var result = await searchClient.GetAsync(string.Format("{0}/?q={1}&count={2}&offset={3}&mkt={4}", NewsSearchEndPoint, WebUtility.UrlEncode(query), count, offset, market));
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);

            if (data.value != null && data.value.Count > 0)
            {
                for (int i = 0; i < data.value.Count; i++)
                {
                    articles.Add(new NewsArticle
                    {
                        Title = data.value[i].name,
                        Url = data.value[i].url,
                        Description = data.value[i].description,
                        ThumbnailUrl = data.value[i].image?.thumbnail?.contentUrl,
                        Provider = data.value[i].provider?[0].name
                    });
                }
            }
            return articles;
        }

    }

    public class NewsArticle
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Provider { get; set; }
    }
}
