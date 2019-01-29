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
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class TextAnalyticsHelper
    {
        private static TextAnalyticsClient client { get; set; }

        static TextAnalyticsHelper()
        {
            InitializeTextAnalyticsService();
        }

        public static Action Throttled;

        private static string apiKey;
        public static string ApiKey
        {
            get { return apiKey; }
            set
            {
                var changed = apiKey != value;
                apiKey = value;
                if (changed)
                {
                    InitializeTextAnalyticsService();
                }
            }
        }

        private static string apiEndpoint;
        public static string ApiEndpoint
        {
            get { return apiEndpoint; }
            set
            {
                var changed = apiEndpoint != value;
                apiEndpoint = value;
                if (changed)
                {
                    InitializeTextAnalyticsService();
                }
            }
        }

        private static void InitializeTextAnalyticsService()
        {
            bool hasEndpoint = !string.IsNullOrEmpty(ApiEndpoint) ? Uri.IsWellFormedUriString(ApiEndpoint, UriKind.Absolute) : false;
            client = !hasEndpoint
                ? new TextAnalyticsClient(new ApiKeyServiceClientCredentials(ApiKey))
                : new TextAnalyticsClient(new ApiKeyServiceClientCredentials(ApiKey))
                {
                    Endpoint = ApiEndpoint
                };
        }

        public static async Task<SentimentResult> GetSentimentAsync(string[] input, string language = "en")
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (!input.Any())
            {
                throw new ArgumentException("Input array is empty.");
            }

            var inputList = new List<MultiLanguageInput>();
            for (int i = 0; i < input.Length; i++)
            {
                inputList.Add(new MultiLanguageInput(language, i.ToString(), input[i]));
            }

            SentimentBatchResult result = await client.SentimentAsync(new MultiLanguageBatchInput(inputList));
            IEnumerable<double> scores = result.Documents.OrderBy(x => x.Id).Select(x => x.Score.GetValueOrDefault());
            return new SentimentResult { Scores = scores };
        }

        public static async Task<KeyPhrasesResult> GetKeyPhrasesAsync(string[] input, string language = "en")
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (!input.Any())
            {
                throw new ArgumentException("Input array is empty.");
            }

            var inputList = new List<MultiLanguageInput>();
            for (int i = 0; i < input.Length; i++)
            {
                inputList.Add(new MultiLanguageInput(language, i.ToString(), input[i]));
            }

            KeyPhraseBatchResult result = await client.KeyPhrasesAsync(new MultiLanguageBatchInput(inputList));
            IEnumerable<IList<string>> keyPhrases = result.Documents.OrderBy(x => x.Id).Select(x => x.KeyPhrases);
            return new KeyPhrasesResult() { KeyPhrases = keyPhrases };
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

    /// Class to hold result of Sentiment call
    /// </summary>
    public class SentimentResult
    {
        public IEnumerable<double> Scores { get; set; }
    }

    public class KeyPhrasesResult
    {
        public IEnumerable<IEnumerable<string>> KeyPhrases { get; set; }
    }
}
