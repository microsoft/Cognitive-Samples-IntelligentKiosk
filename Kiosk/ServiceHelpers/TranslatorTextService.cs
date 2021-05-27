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

using ServiceHelpers.Models;
using ServiceHelpers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    /// <summary>
    /// Translator Text API V3.0
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-reference
    /// </summary>
    public class TranslatorTextService : ServiceBase
    {
        private readonly string HEADER_SUB_KEY = "Ocp-Apim-Subscription-Key";
        private readonly string HEADER_REGION = "Ocp-Apim-Subscription-Region";
        private readonly string SERVICE_URL_FORMAT = "https://api.cognitive.microsofttranslator.com";
        private readonly string API_VERSION = "api-version=3.0";

        public TranslatorTextService(string subscriptionKey, string region = null)
        {
            this.BaseServiceUrl = SERVICE_URL_FORMAT;
            this.RequestHeaders = new Dictionary<string, string>()
            {
                {  this.HEADER_SUB_KEY, subscriptionKey }
            };

            if (!string.IsNullOrEmpty(region))
            {
                this.RequestHeaders.Add(this.HEADER_REGION, region);
            }
        }

        /// <summary>
        /// Translates text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="languageCodeList"></param>
        /// <returns></returns>
        public async Task<TranslationTextResult> TranslateTextAsync(string text, List<string> languageCodeList)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("Input text is empty.");
            }

            if (languageCodeList == null || !languageCodeList.Any())
            {
                throw new ArgumentNullException("Input language code is empty.");
            }

            // Request uri
            string languageCodeParams = $"&to={string.Join("&to=", languageCodeList)}";
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/translate?{this.API_VERSION}{languageCodeParams}");
            var content = new object[] { new { Text = text } };

            List<TranslationTextResult> response = await HttpClientUtility.PostAsJsonAsync<List<TranslationTextResult>>(requestUri, this.RequestHeaders, content);
            return response?.FirstOrDefault();
        }

        /// <summary>
        /// Identifies the language of a piece of text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<DetectedLanguageResult> DetectLanguageAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("Input text is empty.");
            }

            // Request uri
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/detect?{this.API_VERSION}");
            var content = new object[] { new { Text = text } };

            List<DetectedLanguageResult> response = await HttpClientUtility.PostAsJsonAsync<List<DetectedLanguageResult>>(requestUri, this.RequestHeaders, content);
            return response?.FirstOrDefault();
        }

        /// <summary>
        /// Get languages currently supported by Translator Text API
        /// </summary>
        /// <returns></returns>
        public async Task<SupportedLanguages> GetSupportedLanguagesAsync()
        {
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/languages?{this.API_VERSION}");
            return await HttpClientUtility.GetAsync<SupportedLanguages>(requestUri, this.RequestHeaders);
        }

        /// <summary>
        /// Provides alternative translations for a word and a small number of idiomatic phrases.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="languageFrom"></param>
        /// <param name="languageTo"></param>
        /// <returns></returns>
        public async Task<LookupLanguage> GetDictionaryLookup(string text, string languageFrom, string languageTo)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(languageFrom) || string.IsNullOrEmpty(languageTo))
            {
                throw new ArgumentNullException("Input parameter is empty.");
            }

            // Request uri
            string languageCodeParams = $"&from={languageFrom}&to={languageTo}";
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/dictionary/lookup?{this.API_VERSION}{languageCodeParams}");
            var content = new object[] { new { Text = text } };

            List<LookupLanguage> response = await HttpClientUtility.PostAsJsonAsync<List<LookupLanguage>>(requestUri, this.RequestHeaders, content);
            return response?.FirstOrDefault();
        }
    }
}
