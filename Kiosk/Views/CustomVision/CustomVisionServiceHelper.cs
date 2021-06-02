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

using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

namespace ServiceHelpers
{
    public static class CustomVisionServiceHelper
    {
        private static readonly string platform = "onnx";
        public static readonly List<Guid> ObjectDetectionDomainGuidList = new List<Guid>()
        {
            new Guid("da2e3a8a-40a5-4171-82f4-58522f70fbc1"), // Object Detection, General
            new Guid("1d8ffafe-ec40-4fb2-8f90-72b3b6cecea4"), // Object Detection, Logo
            new Guid("a27d5ca5-bb19-49d8-a70a-fec086c47f5b")  // Object Detection, General (exportable)
        };
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

        public static async Task<ImagePrediction> ClassifyImageUrlWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, ImageUrl imageUrl, string publishedName)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.ClassifyImageUrlAsync(projectId, publishedName, imageUrl));
        }

        public static async Task<ImagePrediction> ClassifyImageWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, Func<Task<Stream>> imageStreamCallback, string publishedName)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.ClassifyImageAsync(projectId, publishedName, await imageStreamCallback()));
        }

        public static async Task<TrainingModels::Export> ExportIteration(this ICustomVisionTrainingClient trainingApi, Guid projectId, Guid iterationId, int timeoutInSecond = 30)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(timeoutInSecond);

            TrainingModels::Export exportIteration = null;
            try
            {
                exportIteration = await trainingApi.ExportIterationAsync(projectId, iterationId, platform);
            }
            catch (TrainingModels::CustomVisionErrorException ex)
            {
                if (ex.Body.Code != TrainingModels::CustomVisionErrorCodes.BadRequestExportAlreadyInProgress)
                {
                    throw ex;
                }
            }

            DateTime startTime = DateTime.Now;
            while (true)
            {
                IList<TrainingModels::Export> exports = await trainingApi.GetExportsAsync(projectId, iterationId);
                exportIteration = exports?.FirstOrDefault(x => string.Equals(x.Platform, platform, StringComparison.OrdinalIgnoreCase));

                if (exportIteration?.Status == "Exporting")
                {
                    await Task.Delay(1000);
                }
                else
                {
                    break;
                }

                if (DateTime.Now - startTime > timeout)
                {
                    throw new TimeoutException("The operation couldn't be completed due to a timeout.");
                }
            }

            return exportIteration;
        }
    }
}
