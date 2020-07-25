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

using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceHelpers.Extensions;
using ServiceHelpers.Models.FormRecognizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class ReceiptOCRHelper
    {
        private const int RetryCountOnQuotaLimitError = 6;
        private const int RetryDelayOnQuotaLimitError = 500;
        private const int MaxImageSize = 4200;
        private const string HEADER_SUB_KEY = "Ocp-Apim-Subscription-Key";
        private const string SERVICE_URL_FORMAT = "{0}/formrecognizer/v2.0/prebuilt/receipt";

        private static string SERVICE_URL = string.Empty;

        private static string apiKey;
        public static string ApiKey
        {
            get
            {
                return apiKey;
            }

            set
            {
                var changed = apiKey != value;
                apiKey = value;
                if (changed)
                {
                    InitializeService();
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
                    InitializeService();
                }
            }
        }

        private static void InitializeService()
        {
            SERVICE_URL = string.Format(SERVICE_URL_FORMAT, ApiEndpoint);
        }

        public static async Task<AnalyzeResultResponse> GetReceiptOCRResult(string imageUrl)
        {
            return await GetReceiptOCRResultFromOperation(await GetOperationRequestAsync(imageUrl));
        }

        public static async Task<AnalyzeResultResponse> GetReceiptOCRResult(Stream stream)
        {
            return await GetReceiptOCRResultFromOperation(await GetOperationRequestAsync(await stream.ToBytesAsync()));
        }

        private static async Task<AnalyzeResultResponse> GetReceiptOCRResultFromOperation(string operation)
        {
            HttpResponseMessage result = await CallReceiptOCRAsync(operation);

            string responseContent = await result.Content.ReadAsStringAsync();
            var entity = SafeJsonConvert.DeserializeObject<AnalyzeResultResponse>(responseContent, new JsonSerializerSettings()
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            });

            //add raw data
            if (entity != null)
            {
                entity.RawResponse = responseContent;
            }

            return entity;
        }

        private static async Task<HttpResponseMessage> CallReceiptOCRAsync(string url, int timeoutInSecond = 60)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(timeoutInSecond);
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.Add(HEADER_SUB_KEY, ApiKey);

                HttpResponseMessage result;
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    result = await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.GetAsync(url));
                    result.EnsureSuccessStatusCode();

                    // check the response status: Running or Succeeded
                    string responseContent = await result.Content.ReadAsStringAsync();
                    dynamic jsonObj = !string.IsNullOrEmpty(responseContent) ? JObject.Parse(responseContent) : null;
                    string status = jsonObj?.status?.Value ?? string.Empty;
                    if (string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(1000);
                    }
                    else if (string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    // check timeout
                    if (DateTime.Now - startTime > timeout)
                    {
                        throw new TimeoutException("The operation couldn't be completed due to a timeout.");
                    }
                }

                return result;
            }
        }

        private static async Task<string> GetOperationRequestAsync(string imageUrl)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(HEADER_SUB_KEY, ApiKey);

                StringContent content = new StringContent(JsonConvert.SerializeObject(new { source = imageUrl }), Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage result = await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.PostAsync($"{SERVICE_URL}/analyze", content));

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception($"Error processing the image. Please make sure the image is valid and is less than {MaxImageSize}x{MaxImageSize} in size.");
                }

                result.Headers.TryGetValues("Operation-Location", out IEnumerable<string> operations);
                return operations != null && operations.Any() ? operations.FirstOrDefault() : string.Empty;
            }
        }

        private static async Task<string> GetOperationRequestAsync(byte[] byteData)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(HEADER_SUB_KEY, ApiKey);

                // Request body
                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    HttpResponseMessage result = await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.PostAsync($"{SERVICE_URL}/analyze", content));

                    if (!result.IsSuccessStatusCode)
                    {
                        throw new Exception($"Error processing the image. Please make sure the image is valid and is less than {MaxImageSize}x{MaxImageSize} in size.");
                    }

                    result.Headers.TryGetValues("Operation-Location", out IEnumerable<string> operations);
                    return operations != null && operations.Any() ? operations.FirstOrDefault() : string.Empty;
                }
            }
        }

        private static async Task<HttpResponseMessage> RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task<HttpResponseMessage>> action)
        {
            int retriesLeft = RetryCountOnQuotaLimitError;
            int delay = RetryDelayOnQuotaLimitError;

            HttpResponseMessage response;
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
}
