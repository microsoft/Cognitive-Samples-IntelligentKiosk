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

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace IntelligentKioskSample.Views.NeuralTTS
{
    public class Authentication
    {
        private const string SubscriptionKeyHeaderName = "Ocp-Apim-Subscription-Key";
        private const string TokenTemplate = "https://{0}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

        private Uri tokenUri;
        private string subscriptionKey;

        public Authentication(string subscriptionKey, string region)
        {
            this.subscriptionKey = subscriptionKey;
            this.tokenUri = new Uri(string.Format(TokenTemplate, region));
        }

        public async Task<string> RetrieveNewTokenAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(SubscriptionKeyHeaderName, this.subscriptionKey);
                HttpResponseMessage response = await client.PostAsync(this.tokenUri, new StringContent(string.Empty)).ConfigureAwait(false);

                return response.IsSuccessStatusCode
                    ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                    : throw new WebException(response.ReasonPhrase);
            }
        }
    }
}
