using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IntelligentKioskSample
{
    class CognitiveServiceApiKeyTester
    {
        public static async Task TestFaceApiKeyAsync(string key, string apiEndpoint)
        {
            bool isUri = !string.IsNullOrEmpty(apiEndpoint) ? Uri.IsWellFormedUriString(apiEndpoint, UriKind.Absolute) : false;
            if (!isUri)
            {
                throw new ArgumentException("Invalid URI");
            }
            else
            {
                FaceClient client = new FaceClient(new ApiKeyServiceClientCredentials(key))
                {
                    Endpoint = apiEndpoint
                };
                await client.PersonGroup.ListAsync();
            }
        }

        public static async Task TestComputerVisionApiKeyAsync(string key, string apiEndpoint)
        {
            bool isUri = !string.IsNullOrEmpty(apiEndpoint) ? Uri.IsWellFormedUriString(apiEndpoint, UriKind.Absolute) : false;
            if (!isUri)
            {
                throw new ArgumentException("Invalid URI");
            }
            else
            {
                ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
                {
                    Endpoint = apiEndpoint
                };
                await client.ListModelsAsync();
            }
        }

        public static async Task TestTextAnalyticsApiKeyAsync(string key, string apiEndpoint)
        {
            bool isUri = !string.IsNullOrEmpty(apiEndpoint) ? Uri.IsWellFormedUriString(apiEndpoint, UriKind.Absolute) : false;
            if (!isUri)
            {
                throw new ArgumentException("Invalid URI");
            }
            else
            {
                TextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials(key))
                {
                    Endpoint = apiEndpoint
                };

                await client.SentimentAsync(
                        new MultiLanguageBatchInput(
                            new List<MultiLanguageInput>()
                            {
                          new MultiLanguageInput("en", "0", "I had the best day of my life."),
                            }));
            }
        }

        public static async Task TestBingSearchApiKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Invalid API Key");
            }

            string endpoint = "https://api.cognitive.microsoft.com/bing/v7.0/news/search";
            string testQuery = "Seattle news";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            var result = await RequestAndAutoRetryWhenThrottled(() => 
                               client.GetAsync(string.Format("{0}/?q={1}&count={2}&offset={3}&mkt={4}", endpoint, WebUtility.UrlEncode(testQuery), 2, 0, "en-US")));
            result.EnsureSuccessStatusCode();
            string json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);
            if (data.value == null || data.value.Count == 0)
            {
                throw new Exception("Response data is missing");
            }
        }

        public static async Task TestBingAutosuggestApiKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Invalid API Key");
            }

            string endpoint = "https://api.cognitive.microsoft.com/bing/v7.0/suggestions";
            string testQuery = "Seattle";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            var result = await RequestAndAutoRetryWhenThrottled(() => 
                               client.GetAsync(string.Format("{0}/?q={1}&mkt={2}", endpoint, WebUtility.UrlEncode(testQuery), "en-US")));
            result.EnsureSuccessStatusCode();
            string json = await result.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(json);
            if (data.suggestionGroups == null || data.suggestionGroups.Count == 0)
            {
                throw new Exception("Response data is missing");
            }
        }

        public static async Task TestTranslatorTextApiKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Invalid API Key");
            }

            string testQuery = "Hello world";
            var service = new TranslatorTextService(key);
            var result = await service.DetectLanguageAsync(testQuery);
        }

        private static async Task<HttpResponseMessage> RequestAndAutoRetryWhenThrottled(Func<Task<HttpResponseMessage>> action)
        {
            int retriesLeft = 10;
            int delay = 500;

            HttpResponseMessage response = null;
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

    class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        private readonly string key;
        public ApiKeyServiceClientCredentials(string key)
        {
            this.key = key;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}
