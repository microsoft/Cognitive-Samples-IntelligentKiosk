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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class TextAnalyticsHelper
    {
        // NOTE 12/17/2020: Text Analytics API v3 language support
        // See details: https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/language-support
        public static readonly string DefaultLanguageCode = "en";
        public static readonly string[] SentimentAnalysisSupportedLanguages = { "zh", "zh-hans", "zh-hant", "en", "fr", "de", "hi", "it", "ja", "ko", "no", "pt", "pt-BR", "pt-PT", "es", "tr" };
        public static readonly string[] OpinionMiningSupportedLanguages = { "en" };
        public static readonly string[] KeyPhraseExtractionSupportedLanguages = { "da", "nl", "en", "fi", "fr", "de", "it", "ja", "ko", "no", "nb", "pl", "pt", "pt-BR", "pt-PT", "ru", "es", "sv" };
        public static readonly string[] NamedEntitySupportedLanguages = { "ar", "zh", "zh-hans", "zh-hant", "cs", "da", "nl", "en", "fi", "fr", "de", "he", "hu", "it", "ja", "ko", "no", "nb", "pl", "pt", "pt-BR", "pt-PT", "ru", "es", "sv", "tr" };
        public static readonly string[] EntityLinkingSupportedLanguages = { "en", "es" };
        public static readonly Uri LanguageSupportUri = new Uri("https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/language-support");
        public static readonly Dictionary<string, string> LanguageCodeMap = new Dictionary<string, string>()
        {
            { "zh_chs", "zh-hans" },
            { "zh_cht", "zh-hant" }
        };

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

        public static async Task<DetectedLanguage> DetectLanguageAsync(string input)
        {
            return await client.DetectLanguageAsync(input);
        }

        public static async Task<DocumentSentiment> AnalyzeSentimentAsync(string input, string language = null, bool includeOpinionMining = false)
        {
            var options = new AnalyzeSentimentOptions() { IncludeOpinionMining = includeOpinionMining };
            return await client.AnalyzeSentimentAsync(input, language, options);
        }

        public static async Task<AnalyzeSentimentResultCollection> AnalyzeSentimentAsync(string[] input, string language = null, bool includeOpinionMining = false)
        {
            var options = new AnalyzeSentimentOptions() { IncludeOpinionMining = includeOpinionMining };
            return await client.AnalyzeSentimentBatchAsync(input, language, options);
        }

        public static async Task<KeyPhraseCollection> ExtractKeyPhrasesAsync(string input, string language = null)
        {
            return await client.ExtractKeyPhrasesAsync(input, language);
        }

        public static async Task<ExtractKeyPhrasesResultCollection> ExtractKeyPhrasesAsync(string[] input, string language = null)
        {
            return await client.ExtractKeyPhrasesBatchAsync(input, language);
        }

        public static async Task<CategorizedEntityCollection> RecognizeEntitiesAsync(string input, string language = null)
        {
            return await client.RecognizeEntitiesAsync(input, language);
        }

        public static async Task<LinkedEntityCollection> RecognizeLinkedEntitiesAsync(string input, string language = null)
        {
            return await client.RecognizeLinkedEntitiesAsync(input, language);
        }

        public static string GetLanguageCode(DetectedLanguage detectedLanguage)
        {
            if (LanguageCodeMap.ContainsKey(detectedLanguage.Iso6391Name))
            {
                return LanguageCodeMap[detectedLanguage.Iso6391Name];
            }

            return !string.IsNullOrEmpty(detectedLanguage.Iso6391Name) ? detectedLanguage.Iso6391Name : DefaultLanguageCode;
        }
    }
}
