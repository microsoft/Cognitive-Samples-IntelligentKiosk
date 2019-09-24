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

using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.UI.Input.Inking;

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

        public static async Task TestCustomVisionTrainingApiKeyAsync(string key, string apiEndpoint)
        {
            bool isUri = !string.IsNullOrEmpty(apiEndpoint) ? Uri.IsWellFormedUriString(apiEndpoint, UriKind.Absolute) : false;
            if (!isUri)
            {
                throw new ArgumentException("Invalid URI");
            }
            else
            {
                CustomVisionTrainingClient client = new CustomVisionTrainingClient { Endpoint = apiEndpoint, ApiKey = key };
                await client.GetDomainsAsync();
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

                await client.SentimentAsync(multiLanguageBatchInput:
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

        public static async Task TestAnomalyDetectorApiKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Invalid API Key");
            }

            string endpoint = "https://westus2.api.cognitive.microsoft.com/anomalydetector/v1.0/timeseries/last/detect";
            string requestBody = "{\"series\":[{\"timestamp\":\"1972-01-01T00:00:00Z\",\"value\":826},{\"timestamp\":\"1972-02-01T00:00:00Z\",\"value\":799},{\"timestamp\":\"1972-03-01T00:00:00Z\",\"value\":890},{\"timestamp\":\"1972-04-01T00:00:00Z\",\"value\":900},{\"timestamp\":\"1972-05-01T00:00:00Z\",\"value\":961},{\"timestamp\":\"1972-06-01T00:00:00Z\",\"value\":935},{\"timestamp\":\"1972-07-01T00:00:00Z\",\"value\":894},{\"timestamp\":\"1972-08-01T00:00:00Z\",\"value\":855},{\"timestamp\":\"1972-09-01T00:00:00Z\",\"value\":809},{\"timestamp\":\"1972-10-01T00:00:00Z\",\"value\":810},{\"timestamp\":\"1972-11-01T00:00:00Z\",\"value\":766},{\"timestamp\":\"1972-12-01T00:00:00Z\",\"value\":805}],\"maxAnomalyRatio\":0.25,\"sensitivity\":95,\"granularity\":\"monthly\"}";

            string response = string.Empty;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                HttpResponseMessage result = await RequestAndAutoRetryWhenThrottled(() => client.PostAsync(endpoint, new StringContent(requestBody, Encoding.UTF8, "application/json")));

                result.EnsureSuccessStatusCode();
                response = await result.Content.ReadAsStringAsync();
            }

            dynamic data = JObject.Parse(response);
            if (data.isAnomaly == null)
            {
                throw new Exception("Response data is missing");
            }
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

        public static async Task TestInkRecognizerApiKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Invalid API Key");
            }

            var inkRecognizer = new ServiceHelpers.InkRecognizer(key);

            JObject json = inkRecognizer.ConvertInkToJson();
            var response = await inkRecognizer.RecognizeAsync(json);
            response.EnsureSuccessStatusCode();
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
