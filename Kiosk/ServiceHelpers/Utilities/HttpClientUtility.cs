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
using ServiceHelpers.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServiceHelpers.Utilities
{
    public static class HttpClientUtility
    {
        public static readonly int RETRY_COUNT = 10;
        public static readonly int RETRY_DELAY = 500;

        private static HttpClient Client;

        /// <summary>
        /// Static constructor of the HttpClientUtility
        /// </summary>
        static HttpClientUtility()
        {
            if (Client == null)
            {
                Client = new HttpClient();
            }
        }

        /// <summary>
        /// Send Http Get to the request uri and get the TResult from response content
        /// </summary>
        public static async Task<TResult> GetAsync<TResult>(Uri requestUri, IDictionary<string, string> headers)
        {
            // Get response
            HttpResponseMessage response = await GetAsync(requestUri, headers);

            // Read response
            string responseContent = await response.Content.ReadAsStringAsync();

            // Get result
            TResult result = JsonConvert.DeserializeObject<TResult>(responseContent);
            return result;
        }

        /// <summary>
        /// Send Http Get to the request uri and get HttpResponseMessage
        /// </summary>
        public static async Task<HttpResponseMessage> GetAsync(Uri requestUri, IDictionary<string, string> headers)
        {
            // Create new request function
            Func<HttpRequestMessage> createRequestMessage = () =>
            {
                // Create new request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);

                // Add headers to request
                request.AddHeaders(headers);
                return request;
            };

            // Send request and get response
            HttpResponseMessage response = await ExecuteActionkWithAutoRetry(() => Client.SendAsync(createRequestMessage()));
            return response;
        }

        /// <summary>
        /// Send Http Get to the request uri and get the byte array from response content
        /// </summary>
        public static async Task<byte[]> GetBytesAsync(Uri requestUri, IDictionary<string, string> headers = default(Dictionary<string, string>))
        {
            // Get response
            HttpResponseMessage response = await GetAsync(requestUri, headers);

            // Read response
            byte[] responseContent = await response.Content.ReadAsByteArrayAsync();
            return responseContent;
        }

        /// <summary>
        /// Send Http Post to request uri and get TResult from response content 
        /// </summary>
        public static async Task<TResult> PostAsBytesAsync<TResult>(Uri requestUri, IDictionary<string, string> headers, byte[] content)
        {
            // Post request and get response
            HttpResponseMessage response = await PostAsBytesAsync(requestUri, headers, content);

            // Read response
            string responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResult>(responseContent);
        }

        /// <summary>
        /// Send Http Post to request uri and get TResult from response content 
        /// </summary>
        public static async Task<TResult> PostAsJsonAsync<TResult>(Uri requestUri, IDictionary<string, string> headers, object content)
        {
            // Post request and get response
            HttpResponseMessage response = await PostAsJsonAsync(requestUri, headers, content);

            // Read response
            string responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResult>(responseContent);
        }

        /// <summary>
        /// Send Http Post to request uri and get HttpResponseMessage
        /// </summary>
        public static async Task<HttpResponseMessage> PostAsBytesAsync(Uri requestUri, IDictionary<string, string> headers, byte[] content)
        {
            // Create new request function
            Func<HttpRequestMessage> createRequestMessage = () =>
            {
                // Create new request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);

                // Add headers to request
                request.AddHeaders(headers);

                // Add content as Json
                request.AddContentAsBytes(content);

                return request;
            };

            // Post request
            HttpResponseMessage response = await ExecuteActionkWithAutoRetry(() => Client.SendAsync(createRequestMessage()));
            return response;
        }

        /// <summary>
        /// Send Http Post to request uri and get HttpResponseMessage
        /// </summary>
        public static async Task<HttpResponseMessage> PostAsJsonAsync(Uri requestUri, IDictionary<string, string> headers, object content)
        {
            // Create new request function
            Func<HttpRequestMessage> createRequestMessage = () =>
            {
                // Create new request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);

                // Add headers to request
                request.AddHeaders(headers);

                // Add content as Json
                request.AddContentAsJson(content);

                return request;
            };

            // Post request
            HttpResponseMessage response = await ExecuteActionkWithAutoRetry(() => Client.SendAsync(createRequestMessage()));
            return response;
        }

        public static async Task<HttpResponseMessage> PutAsJsonAsync(Uri requestUri, IDictionary<string, string> headers, object content)
        {
            // Create new request function
            Func<HttpRequestMessage> createRequestMessage = () =>
            {
                // Create new request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUri);

                // Add headers to request
                request.AddHeaders(headers);

                // Add content as Json
                request.AddContentAsJson(content);

                return request;
            };

            // Put request
            HttpResponseMessage response = await ExecuteActionkWithAutoRetry(() => Client.SendAsync(createRequestMessage()));
            return response;
        }

        /// <summary>
        /// Send Http Delete to request uri and get HttpResponseMessage
        /// </summary>
        public static async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, IDictionary<string, string> headers)
        {
            // Create new request function
            Func<HttpRequestMessage> deleteRequestMessage = () =>
            {
                // Create new request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

                // Add headers to request
                request.AddHeaders(headers);

                return request;
            };

            // Delete request
            HttpResponseMessage response = await ExecuteActionkWithAutoRetry(() => Client.SendAsync(deleteRequestMessage()));
            return response;
        }

        /// <summary>
        /// Execute the action which returns HttpResponseMessage with auto retry
        /// </summary>
        private static async Task<HttpResponseMessage> ExecuteActionkWithAutoRetry(Func<Task<HttpResponseMessage>> action)
        {
            int retryCount = RETRY_COUNT;
            int retryDelay = RETRY_DELAY;

            HttpResponseMessage response;

            while (true)
            {
                response = await action();

                if (response.StatusCode == (HttpStatusCode)429 && retryCount > 0)
                {
                    await Task.Delay(retryDelay);
                    retryCount--;
                    retryDelay *= 2;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(await response.Content.ReadAsStringAsync());
                }

                break;
            }

            return response;
        }

    }
}
