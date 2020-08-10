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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ServiceHelpers.Extensions
{
    public static class HttpExtensions
    {
        /// <summary>
        /// Add headers to request
        /// </summary>
        public static void AddHeaders(this HttpRequestMessage request, IDictionary<string, string> headers)
        {
            // Add headers to request
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    request.Headers.Add(key, headers[key]);
                }
            }
        }

        /// <summary>
        /// Add headers to request
        /// </summary>
        public static void AddHeaders(this HttpRequestHeaders httpRequestHeaders, IDictionary<string, string> headers)
        {
            // Add headers to request
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    httpRequestHeaders.Add(key, headers[key]);
                }
            }
        }

        /// <summary>
        /// Add content to request as byte array
        /// </summary>
        public static void AddContentAsBytes(this HttpRequestMessage request, byte[] content)
        {
            if (content?.Count() > 0)
            {
                ByteArrayContent byteContent = new ByteArrayContent(content);
                byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                request.Content = byteContent;
            }
        }

        /// <summary>
        /// Add content to request as json
        /// </summary>
        public static void AddContentAsJson(this HttpRequestMessage request, object content)
        {
            if (content != null)
            {
                string jsonContent = JsonConvert.SerializeObject(content);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }
        }
    }
}
