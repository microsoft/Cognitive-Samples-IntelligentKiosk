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

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class VisionServiceHelper
    {
        private const int MaxRetriesOnTextRecognition = 10;
        private const int DelayOnTextRecognition = 1000;
        private const int NumberOfCharsInOperationId = 36;

        private const int RetryCountOnQuotaLimitError = 6;
        private const int RetryDelayOnQuotaLimitError = 500;

        private static ComputerVisionClient client { get; set; }

        static VisionServiceHelper()
        {
            InitializeVisionService();
        }

        public static Action Throttled;

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
                    InitializeVisionService();
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
                    InitializeVisionService();
                }
            }
        }

        private static void InitializeVisionService()
        {
            bool hasEndpoint = !string.IsNullOrEmpty(ApiEndpoint) ? Uri.IsWellFormedUriString(ApiEndpoint, UriKind.Absolute) : false;
            client = !hasEndpoint
                ? new ComputerVisionClient(new ApiKeyServiceClientCredentials(ApiKey))
                : new ComputerVisionClient(new ApiKeyServiceClientCredentials(ApiKey))
                {
                    Endpoint = ApiEndpoint
                };
        }

        private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = RetryCountOnQuotaLimitError;
            int delay = RetryDelayOnQuotaLimitError;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (ComputerVisionErrorResponseException exception) when (exception.Response?.StatusCode == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    ErrorTrackingHelper.TrackException(exception, "Vision API throttling error");
                    if (retriesLeft == 1 && Throttled != null)
                    {
                        Throttled();
                    }

                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        public static async Task<ImageDescription> DescribeAsync(Func<Task<Stream>> imageStreamCallback)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await client.DescribeImageInStreamAsync(await imageStreamCallback()));
        }

        public static async Task<ImageAnalysis> AnalyzeImageAsync(string imageUrl, IList<VisualFeatureTypes?> visualFeatures = null, IList<Details?> details = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.AnalyzeImageAsync(imageUrl, visualFeatures, details));
        }

        public static async Task<ImageAnalysis> AnalyzeImageAsync(Func<Task<Stream>> imageStreamCallback, IList<VisualFeatureTypes?> visualFeatures = null, IList<Details?> details = null)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await client.AnalyzeImageInStreamAsync(await imageStreamCallback(), visualFeatures, details));
        }

        public static async Task<ImageDescription> DescribeAsync(string imageUrl)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.DescribeImageAsync(imageUrl));
        }

        public static async Task<AnalyzeResults> ReadFileAsync(string imageUrl)
        {
            return await GetReadFileResultFromOperationAsync(await GetReadOperationRequestAsync(imageUrl));
        }

        public static async Task<AnalyzeResults> ReadFileAsync(Func<Task<Stream>> imageStreamCallback)
        {
            return await GetReadFileResultFromOperationAsync(await GetReadOperationRequestAsync(await imageStreamCallback()));
        }

        public static async Task<DetectResult> DetectObjectsInStreamAsync(Func<Task<Stream>> imageStreamCallback)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await client.DetectObjectsInStreamAsync(await imageStreamCallback()));
        }

        public static async Task<DetectResult> DetectObjectsAsync(string imageUrl)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.DetectObjectsAsync(imageUrl));
        }

        private static async Task<AnalyzeResults> GetReadFileResultFromOperationAsync(string operation)
        {
            bool isValidGuid = Guid.TryParse(operation, out Guid operationId);
            if (!isValidGuid)
            {
                throw new ArgumentException("Operation Id is invalid.");
            }

            var opResult = new ReadOperationResult();

            int i = 0;
            while (i++ < MaxRetriesOnTextRecognition)
            {
                // Get the operation result
                opResult = await client.GetReadResultAsync(operationId);

                // Wait if operation is running or has not started
                if (opResult.Status == OperationStatusCodes.NotStarted || opResult.Status == OperationStatusCodes.Running)
                {
                    await Task.Delay(DelayOnTextRecognition);
                }
                else
                {
                    break;
                }
            }

            if (opResult.Status != OperationStatusCodes.Succeeded)
            {
                throw new Exception($"Computer Vision operation was not successful with status: {opResult.Status}");
            }

            return opResult.AnalyzeResult;
        }

        private static async Task<string> GetReadOperationRequestAsync(string imageUrl)
        {
            ReadHeaders result = await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.ReadAsync(imageUrl));
            string operationLocation = result.OperationLocation;

            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            return operationLocation.Substring(operationLocation.Length - NumberOfCharsInOperationId);
        }

        private static async Task<string> GetReadOperationRequestAsync(Stream image)
        {
            ReadInStreamHeaders result = await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => client.ReadInStreamAsync(image));
            string operationLocation = result.OperationLocation;

            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            return operationLocation.Substring(operationLocation.Length - NumberOfCharsInOperationId);
        }
    }
}
