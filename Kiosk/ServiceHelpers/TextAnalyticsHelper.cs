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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json.Linq;

namespace ServiceHelpers
{
    public class TextAnalyticsHelper
    {
        private const string ServiceBaseUri = "https://westus.api.cognitive.microsoft.com/";

        private static HttpClient httpClient { get; set; }

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
                    InitializeTextAnalyticsClient();
                }
            }
        }

        private static void InitializeTextAnalyticsClient()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.BaseAddress = new Uri(ServiceBaseUri);
        }

        public static async Task<SentimentResult> GetTextSentimentAsync(string[] input, string language = "en")
        {
            SentimentResult sentimentResult = new SentimentResult() { Scores = new double[] { 0.5 } };

            if (input != null)
            {
                // Request body. 
                string requestString = "{\"documents\":[";
                for (int i = 0; i < input.Length; i++)
                {
                    requestString += string.Format("{{\"id\":\"{0}\",\"text\":\"{1}\", \"language\":\"{2}\"}}", i, input[i].Replace("\"", "'"), language);
                    if (i != input.Length - 1)
                    {
                        requestString += ",";
                    }
                }

                requestString += "]}";

                byte[] byteData = Encoding.UTF8.GetBytes(requestString);

                // get sentiment
                string uri = "text/analytics/v2.0/sentiment";
                var response = await CallEndpoint(httpClient, uri, byteData);
                string content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Text Analytics failed. " + content);
                }
                dynamic data = JObject.Parse(content);
                Dictionary<int, double> scores = new Dictionary<int, double>();
                if (data.documents != null)
                {
                    for (int i = 0; i < data.documents.Count; i++)
                    {
                        scores[(int)data.documents[i].id] = data.documents[i].score;
                    }
                }

                if (data.errors != null)
                { 
                    for (int i = 0; i < data.errors.Count; i++)
                    {
                        scores[(int)data.errors[i].id] = 0.5;
                    }
                }

                sentimentResult = new SentimentResult { Scores = scores.OrderBy(s => s.Key).Select(s => s.Value) };
            }

            return sentimentResult;
        }

        public static async Task<KeyPhrasesResult> GetKeyPhrasesAsync(string[] input, string language = "en")
        {
            KeyPhrasesResult result = new KeyPhrasesResult() { KeyPhrases = Enumerable.Empty<IEnumerable<string>>() };

            if (input != null)
            {
                // Request body. 
                string requestString = "{\"documents\":[";
                for (int i = 0; i < input.Length; i++)
                {
                    requestString += string.Format("{{\"id\":\"{0}\",\"text\":\"{1}\", \"language\":\"{2}\"}}", i, input[i].Replace("\"", "'"), language);
                    if (i != input.Length - 1)
                    {
                        requestString += ",";
                    }
                }

                requestString += "]}";

                byte[] byteData = Encoding.UTF8.GetBytes(requestString);

                // get sentiment
                string uri = "text/analytics/v2.0/keyPhrases";
                var response = await CallEndpoint(httpClient, uri, byteData);
                string content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Text Analytics failed. " + content);
                }
                dynamic data = JObject.Parse(content);
                Dictionary<int, IEnumerable<string>> phrasesDictionary = new Dictionary<int, IEnumerable<string>>();
                if (data.documents != null)
                {
                    for (int i = 0; i < data.documents.Count; i++)
                    {
                        List<string> phrases = new List<string>();

                        for (int j = 0; j < data.documents[i].keyPhrases.Count; j++)
                        {
                            phrases.Add((string)data.documents[i].keyPhrases[j]);
                        }
                        phrasesDictionary[i] = phrases;
                    }
                }

                if (data.errors != null)
                {
                    for (int i = 0; i < data.errors.Count; i++)
                    {
                        phrasesDictionary[i] = Enumerable.Empty<string>();
                    }
                }

                result.KeyPhrases = phrasesDictionary.OrderBy(e => e.Key).Select(e => e.Value);
            }

            return result;
        }

        static async Task<HttpResponseMessage> CallEndpoint(HttpClient client, string uri, byte[] byteData)
        {
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return await client.PostAsync(uri, content);
            }
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
