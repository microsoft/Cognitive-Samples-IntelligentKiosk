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

using Azure;
using Azure.AI.TextAnalytics;
using System;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class TextAnalyticsHelper
    {
        // NOTE 10/19/2020: Text Analytics API v3 is not available in the following regions: China North 2, China East.
        // https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/migration-guide?tabs=sentiment-analysis
        public static readonly string[] NotAvailableAzureRegions = new string[] { "chinanorth2", "chinaeast" };

        // Note: Data limits
        // See details: https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/concepts/data-limits?tabs=version-3#data-limits
        public const int MaxRequestsPerSecond = 100; // for S0 / F0 tier
        public const int MaximumLengthOfTextDocument = 5120; // 5,120 characters as measured by StringInfo.LengthInTextElements
        public const int MaxDocumentsPerRequestForSentimentAnalysis = 10;   // Max Documents Per Request for Sentiment Analysis feature
        public const int MaxDocumentsPerRequestForKeyPhraseExtraction = 10; // Max Documents Per Request for Key Phrase Extraction feature

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
            var credentials = !string.IsNullOrEmpty(ApiKey) ? new AzureKeyCredential(ApiKey) : null;
            var endpoint = !string.IsNullOrEmpty(ApiEndpoint) && Uri.IsWellFormedUriString(ApiEndpoint, UriKind.Absolute) ? new Uri(ApiEndpoint) : null;
            client = credentials != null && endpoint != null ? new TextAnalyticsClient(endpoint, credentials) : null;
        }

        public static async Task<DocumentSentiment> AnalyzeSentimentAsync(string input, string language = "en", AdditionalSentimentAnalyses sentimentAnalyses = AdditionalSentimentAnalyses.None)
        {
            var options = new AnalyzeSentimentOptions() { AdditionalSentimentAnalyses = sentimentAnalyses };
            return await client.AnalyzeSentimentAsync(input, language, options);
        }

        public static async Task<AnalyzeSentimentResultCollection> AnalyzeSentimentAsync(string[] input, string language = "en", AdditionalSentimentAnalyses sentimentAnalyses = AdditionalSentimentAnalyses.None)
        {
            var options = new AnalyzeSentimentOptions() { AdditionalSentimentAnalyses = sentimentAnalyses };
            return await client.AnalyzeSentimentBatchAsync(input, language, options);
        }

        public static async Task<DetectedLanguage> DetectLanguageAsync(string input)
        {
            return await client.DetectLanguageAsync(input);
        }

        public static async Task<CategorizedEntityCollection> RecognizeEntitiesAsync(string input)
        {
            return await client.RecognizeEntitiesAsync(input);
        }

        public static async Task<LinkedEntityCollection> RecognizeLinkedEntitiesAsync(string input)
        {
            return await client.RecognizeLinkedEntitiesAsync(input);
        }

        public static async Task<KeyPhraseCollection> ExtractKeyPhrasesAsync(string input, string language = "en")
        {
            return await client.ExtractKeyPhrasesAsync(input, language);
        }

        public static async Task<ExtractKeyPhrasesResultCollection> ExtractKeyPhrasesAsync(string[] input, string language = "en")
        {
            return await client.ExtractKeyPhrasesBatchAsync(input, language);
        }
    }
}
