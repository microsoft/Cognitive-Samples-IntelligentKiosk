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

using ServiceHelpers.Extensions;
using ServiceHelpers.Models.FormRecognizer;
using ServiceHelpers.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    /// <summary>
    /// Form Recognizer API V2.0
    /// https://westus2.dev.cognitive.microsoft.com/docs/services/form-recognizer-api-v2
    /// </summary>
    public class FormRecognizerService : ServiceBase
    {
        const int RETRY_DELAY = 1000;
        const int RETRY_COUNT = 20;

        private readonly string HEADER_LOCATION_KEY = "Location";
        private readonly string HEADER_OPERATION_LOCATION_KEY = "Operation-Location";
        private readonly string HEADER_SUB_KEY = "Ocp-Apim-Subscription-Key";
        private readonly string SERVICE_URL_FORMAT = "{0}/formrecognizer/v2.0";

        public const string ImageJpegContentType = "image/jpeg";
        public const string PdfContentType = "application/pdf";

        public FormRecognizerService(string subscriptionKey, string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint) == true)
            {
                throw new ArgumentNullException("Endpoint is not initialized.");
            }

            if (string.IsNullOrEmpty(subscriptionKey) == true)
            {
                throw new ArgumentNullException("Subscription key is not initialized.");
            }

            this.BaseServiceUrl = string.Format(SERVICE_URL_FORMAT, endpoint);
            this.RequestHeaders = new Dictionary<string, string>()
            {
                {  this.HEADER_SUB_KEY, subscriptionKey }
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="trainingDataUrl">SAS Url to Azure Blob Storage container
        /// For example, @"https://<azure_storage_accountName>.blob.core.windows.net/<storage_container_name>?<SAS_Token>";
        /// For using SAS see: https://docs.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1
        /// </param>
        /// <returns>URL with ID of the new model being trained</returns>
        public async Task<Guid> TrainCustomModelAsync(string trainingDataUrl)
        {
            if (!Uri.IsWellFormedUriString(trainingDataUrl, UriKind.Absolute))
            {
                throw new ArgumentException("Invalid trainingDataUrl");
            }

            // Request uri
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/custom/models");

            // Create content of the request
            var content = new { source = trainingDataUrl };

            // Get response
            HttpResponseMessage response = await HttpClientUtility.PostAsJsonAsync(requestUri, this.RequestHeaders, content);

            return await GetResultFromLocationResponse(response);
        }

        public async Task<ListModelResultResponse> GetCustomModelsAsync()
        {
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/custom/models");
            return await HttpClientUtility.GetAsync<ListModelResultResponse>(requestUri, this.RequestHeaders);
        }

        public async Task<ModelResultResponse> GetCustomModelAsync(Guid modelId)
        {
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/custom/models/{modelId}");
            return await HttpClientUtility.GetAsync<ModelResultResponse>(requestUri, this.RequestHeaders);
        }

        public async Task DeleteCustomModelAsync(Guid modelId)
        {
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/custom/models/{modelId}");
            await HttpClientUtility.DeleteAsync(requestUri, this.RequestHeaders);
        }

        public async Task<AnalyzeFormResult> AnalyzeImageFormWithCustomModelAsync(Guid modelId, Func<Task<Stream>> imageStreamCallback, string contentType)
        {
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/custom/models/{modelId}/analyze");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.AddHeaders(this.RequestHeaders);

                byte[] imageBytes = await (await imageStreamCallback()).ToBytesAsync();
                ByteArrayContent byteArrayContent = new ByteArrayContent(imageBytes);
                byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                HttpResponseMessage response = await client.PostAsync(requestUri, byteArrayContent);
                response.EnsureSuccessStatusCode();

                return await GetResultFromResponse(response);
            }
        }

        public async Task<AnalyzeFormResult> AnalyzeImageFormWithCustomModelAsync(Guid modelId, Stream imageStream, string contentType)
        {
            Uri requestUri = new Uri($"{this.BaseServiceUrl}/custom/models/{modelId}/analyze");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.AddHeaders(this.RequestHeaders);

                byte[] imageBytes = await imageStream.ToBytesAsync();
                ByteArrayContent byteArrayContent = new ByteArrayContent(imageBytes);
                byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                HttpResponseMessage response = await client.PostAsync(requestUri, byteArrayContent);
                response.EnsureSuccessStatusCode();

                return await GetResultFromResponse(response);
            }
        }

        private async Task<Guid> GetResultFromLocationResponse(HttpResponseMessage response)
        {
            // Process operation
            if (response.Headers.Contains(this.HEADER_LOCATION_KEY) == false)
            {
                throw new InvalidOperationException("No location value returned from initial request.");
            }

            Uri locationUri = new Uri(response.Headers.GetValues(this.HEADER_LOCATION_KEY).First());

            var opResult = new ModelResultResponse();

            int i = 0;
            while (i++ < RETRY_COUNT)
            {
                // Get the operation result
                opResult = await HttpClientUtility.GetAsync<ModelResultResponse>(locationUri, this.RequestHeaders);

                // Wait if operation is running or has not started
                if (opResult.ModelInfo.Status == "creating")
                {
                    await Task.Delay(RETRY_DELAY);
                }
                else
                {
                    break;
                }
            }

            if (opResult.ModelInfo.Status != "ready")
            {
                throw new Exception($"Form recognition operation was not successful with status: {opResult.ModelInfo.Status}");
            }

            return opResult.ModelInfo.ModelId;
        }

        private async Task<AnalyzeFormResult> GetResultFromResponse(HttpResponseMessage response)
        {
            // Process operation
            if (response.Headers.Contains(this.HEADER_OPERATION_LOCATION_KEY) == false)
            {
                throw new InvalidOperationException("No operation-location value returned from initial request.");
            }

            Uri locationUri = new Uri(response.Headers.GetValues(this.HEADER_OPERATION_LOCATION_KEY).First());

            var opResult = new AnalyzeResultResponse();

            int i = 0;
            while (i++ < RETRY_COUNT)
            {
                // Get the operation result
                opResult = await HttpClientUtility.GetAsync<AnalyzeResultResponse>(locationUri, this.RequestHeaders);

                // Wait if operation is running or has not started
                if (opResult.Status == "notStarted" || opResult.Status == "running")
                {
                    await Task.Delay(RETRY_DELAY);
                }
                else
                {
                    break;
                }
            }

            if (opResult.Status != "succeeded")
            {
                throw new Exception($"Form recognition operation was not successful with status: {opResult.Status}");
            }

            return opResult.AnalyzeResult;
        }
    }
}
