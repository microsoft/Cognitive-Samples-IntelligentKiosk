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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class BingSearchHelper
    {
        private const string ImageSearchEndPoint = "https://api.bing.microsoft.com/v7.0/images/search";
        private const string ImageInsightsEndPoint = "https://api.bing.microsoft.com/v7.0/images/details";
        private const string AutoSuggestionEndPoint = "https://api.bing.microsoft.com/v7.0/suggestions";
        private const string NewsSearchEndPoint = "https://api.bing.microsoft.com/v7.0/news/search";

        private const string SAFE_SEARCH = "&safeSearch=Strict";

        private static int RetryCountOnQuotaLimitError = 6;
        private static int RetryDelayOnQuotaLimitError = 500;

        private static HttpClient searchClient { get; set; }

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
            searchClient = new HttpClient();
            searchClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SearchApiKey);
        }

        public static async Task<IEnumerable<string>> GetImageSearchResults(string query, string imageContent = "Face", int count = 20, int offset = 0, string market = "en-US")
        {
            var urls = new List<string>();
            string content = string.IsNullOrEmpty(imageContent) ? "" : "&imageContent=" + imageContent;
            string requestUri = $"{ImageSearchEndPoint}?q={WebUtility.UrlEncode(query)}{SAFE_SEARCH}&mkt={market}&imageType=Photo&color=ColorOnly&count={count}&offset={offset}{content}";

            HttpResponseMessage result = await RequestAndAutoRetryWhenThrottled(() => searchClient.GetAsync(requestUri));
            result.EnsureSuccessStatusCode();

            string json = await result.Content.ReadAsStringAsync();
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
            var suggestions = new List<string>();
            string requestUri = $"{AutoSuggestionEndPoint}?q={WebUtility.UrlEncode(query)}&mkt={market}";

            HttpResponseMessage result = await RequestAndAutoRetryWhenThrottled(() => searchClient.GetAsync(requestUri));
            result.EnsureSuccessStatusCode();

            string json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);

            if (data.suggestionGroups != null && data.suggestionGroups.Count > 0 && data.suggestionGroups[0].searchSuggestions != null)
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
            var articles = new List<NewsArticle>();
            string requestUri = $"{NewsSearchEndPoint}?q={WebUtility.UrlEncode(query)}&count={count}&offset={offset}&mkt={market}";

            HttpResponseMessage result = await RequestAndAutoRetryWhenThrottled(() => searchClient.GetAsync(requestUri));
            result.EnsureSuccessStatusCode();

            string json = await result.Content.ReadAsStringAsync();
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

        public static async Task<IEnumerable<VisualSearchCelebrityResult>> GetVisuallySimilarCelebrities(string imgUrl)
        {
            HttpResponseMessage result = await CallBingImageInsightsAsync(imgUrl, "RecognizedEntities");
            return ParseCelebrityResults(await result.Content.ReadAsStringAsync());
        }
        public static async Task<IEnumerable<VisualSearchCelebrityResult>> GetVisuallySimilarCelebrities(Stream stream)
        {
            HttpResponseMessage result = await CallBingImageInsightsAsync(stream, "RecognizedEntities");
            return ParseCelebrityResults(await result.Content.ReadAsStringAsync());
        }

        private static async Task<HttpResponseMessage> CallBingImageInsightsAsync(string imgUrl, string module)
        {
            string requestUri = $"{ImageInsightsEndPoint}?imgUrl={WebUtility.UrlEncode(imgUrl)}&modules={module}";
            HttpResponseMessage result = await RequestAndAutoRetryWhenThrottled(() => searchClient.GetAsync(requestUri));
            result.EnsureSuccessStatusCode();

            return result;
        }

        private static async Task<HttpResponseMessage> CallBingImageInsightsAsync(Stream stream, string module)
        {
            var strContent = new StreamContent(stream);
            strContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { FileName = "AnyNameWorks" };

            var content = new MultipartFormDataContent();
            content.Add(strContent);

            string requestUri = $"{ImageInsightsEndPoint}?modules={module}";
            HttpResponseMessage result = await RequestAndAutoRetryWhenThrottled(() => searchClient.PostAsync(requestUri, content));
            result.EnsureSuccessStatusCode();

            return result;
        }

        private static List<VisualSearchCelebrityResult> ParseCelebrityResults(string json)
        {
            List<VisualSearchCelebrityResult> results = new List<VisualSearchCelebrityResult>();

            dynamic data = JObject.Parse(json);
            if (data.recognizedEntityGroups != null && data.recognizedEntityGroups.value.Count > 0)
            {
                for (int i = 0; i < data.recognizedEntityGroups.value.Count; i++)
                {
                    for (int j = 0; j < data.recognizedEntityGroups.value[i].recognizedEntityRegions.Count; j++)
                    {
                        for (int k = 0; k < data.recognizedEntityGroups.value[i].recognizedEntityRegions[j].matchingEntities.Count; k++)
                        {
                            dynamic entity = data.recognizedEntityGroups.value[i].recognizedEntityRegions[j].matchingEntities[k];
                            results.Add(new VisualSearchCelebrityResult
                            {
                                Name = entity.entity.name.Value,
                                SimilarityScore = Math.Round(entity.matchConfidence.Value, 2),
                                Occupation = entity.entity.jobTitle != null ? entity.entity.jobTitle.Value : "",
                                ReferenceUrl = entity.entity.image.hostPageUrl.Value,
                                ImageUrl = entity.entity.image.contentUrl.Value
                            });
                        }
                    }
                }
            }
            return results;
        }

        public static async Task<IEnumerable<VisualSearchPhotoResult>> GetVisuallySimilarImages(string imgUrl)
        {
            var result = await CallBingImageInsightsAsync(imgUrl, "SimilarImages");
            return ParsePhotoResults(await result.Content.ReadAsStringAsync());
        }

        public static async Task<IEnumerable<VisualSearchPhotoResult>> GetVisuallySimilarImages(Stream stream)
        {
            var result = await CallBingImageInsightsAsync(stream, "SimilarImages");
            return ParsePhotoResults(await result.Content.ReadAsStringAsync());
        }

        private static List<VisualSearchPhotoResult> ParsePhotoResults(string json)
        {
            List<VisualSearchPhotoResult> results = new List<VisualSearchPhotoResult>();

            dynamic data = JObject.Parse(json);
            if (data.visuallySimilarImages != null && data.visuallySimilarImages.value.Count > 0)
            {
                for (int i = 0; i < data.visuallySimilarImages.value.Count; i++)
                {
                    results.Add(new VisualSearchPhotoResult
                    {
                        ImageUrl = data.visuallySimilarImages.value[i].thumbnailUrl.Value
                    });
                }
            }

            return results;
        }

        public static async Task<IEnumerable<VisualSearchProductResult>> GetVisuallySimilarProducts(string imgUrl)
        {
            var result = await CallBingImageInsightsAsync(imgUrl, "SimilarProducts");
            return ParseProductResults(await result.Content.ReadAsStringAsync());
        }

        public static async Task<IEnumerable<VisualSearchProductResult>> GetVisuallySimilarProducts(Stream stream)
        {
            var result = await CallBingImageInsightsAsync(stream, "SimilarProducts");
            return ParseProductResults(await result.Content.ReadAsStringAsync());
        }

        private static List<VisualSearchProductResult> ParseProductResults(string json)
        {
            List<VisualSearchProductResult> products = new List<VisualSearchProductResult>();

            dynamic data = JObject.Parse(json);
            if (data.visuallySimilarProducts != null && data.visuallySimilarProducts.value.Count > 0)
            {
                for (int i = 0; i < data.visuallySimilarProducts.value.Count; i++)
                {
                    dynamic prod = data.visuallySimilarProducts.value[i];
                    if (prod?.insightsMetadata?.aggregateOffer?.priceCurrency != null && prod?.insightsMetadata?.aggregateOffer?.lowPrice != null)
                    {
                        products.Add(new VisualSearchProductResult
                        {
                            Name = prod.insightsMetadata.aggregateOffer.name.Value,
                            ImageUrl = prod.thumbnailUrl.Value,
                            ReferenceUrl = prod.hostPageUrl.Value,
                            Price = string.Format("{0} {1}", prod.insightsMetadata.aggregateOffer.priceCurrency.Value, prod.insightsMetadata.aggregateOffer.lowPrice.Value)
                        });
                    }
                }
            }

            return products;
        }

        private static async Task<HttpResponseMessage> RequestAndAutoRetryWhenThrottled(Func<Task<HttpResponseMessage>> action)
        {
            int retriesLeft = RetryCountOnQuotaLimitError;
            int delay = RetryDelayOnQuotaLimitError;

            HttpResponseMessage response;
            while (true)
            {
                response = await action();

                if ((int)response.StatusCode == 429 && retriesLeft > 0)
                {
                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
                else
                {
                    break;
                }
            }

            return response;
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

    public abstract class VisualSearchResult
    {
        public string ImageUrl { get; set; }
        public string ReferenceUrl { get; set; }
    }

    public class VisualSearchPhotoResult : VisualSearchResult
    {
    }

    public class VisualSearchProductResult : VisualSearchResult
    {
        public string Name { get; set; }
        public string Price { get; set; }
    }

    public class VisualSearchCelebrityResult : VisualSearchResult
    {
        public string Name { get; set; }
        public string Occupation { get; set; }
        public double SimilarityScore { get; set; }
    }
}
