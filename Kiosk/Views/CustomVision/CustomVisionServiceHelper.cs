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

using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class CustomVisionServiceHelper
    {
        public static int RetryCountOnQuotaLimitError = 6;
        public static int RetryDelayOnQuotaLimitError = 500;

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
                catch (HttpOperationException exception) when (exception.Response.StatusCode == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        public static async Task<ImagePredictionResultModel> PredictImageUrlWithRetryAsync(this IPredictionEndpoint predictionApi, Guid projectId, ImageUrl imageUrl, Guid iterationId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError<ImagePredictionResultModel>(async () => await predictionApi.PredictImageUrlAsync(projectId, imageUrl, iterationId));
        }

        public static async Task<ImagePredictionResultModel> PredictImageWithRetryAsync(this IPredictionEndpoint predictionApi, Guid projectId, Func<Task<Stream>> imageStreamCallback, Guid iterationId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError<ImagePredictionResultModel>(async () => await predictionApi.PredictImageAsync(projectId, await imageStreamCallback(), iterationId));
        }
    }
}
