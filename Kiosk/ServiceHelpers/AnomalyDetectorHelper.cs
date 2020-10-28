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
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class AnomalyDetectorHelper : ServiceBase
    {
        private static readonly string HEADER_SUB_KEY = "Ocp-Apim-Subscription-Key";
        private static readonly string SERVICE_URL_FORMAT = "{0}/anomalydetector/v1.0";
        private static readonly string BATCH_SERVICE_URL = "/timeseries/entire/detect";
        private static readonly string STREAMING_SERVICE_URL = "/timeseries/last/detect";

        private static IDictionary<string, string> defaultRequestHeaders;

        private static string apiKey = string.Empty;
        public static string ApiKey
        {
            get { return apiKey; }
            set
            {
                var changed = ( apiKey != value );
                apiKey = value;
                if (changed)
                {
                    InitializeService();
                }
            }
        }

        private static string endpoint = string.Empty;

        public static string Endpoint
        {
            get { return endpoint; }
            set
            {
                endpoint = string.Format(SERVICE_URL_FORMAT, value) ;
            }
        }

        static AnomalyDetectorHelper()
        {
            InitializeService();
        }

        private static void InitializeService()
        {
            defaultRequestHeaders = new Dictionary<string, string>()
            {
                {  HEADER_SUB_KEY, ApiKey }
            };
        }

        public static async Task<AnomalyLastDetectResult> GetStreamingDetectionResult(AnomalyDetectionRequest dataRequest)
        {
            return await HttpClientUtility.PostAsJsonAsync<AnomalyLastDetectResult>(new Uri(endpoint + STREAMING_SERVICE_URL), defaultRequestHeaders, dataRequest);
        }

        public static async Task<AnomalyEntireDetectResult> GetBatchDetectionResult(AnomalyDetectionRequest dataRequest)
        {
            return await HttpClientUtility.PostAsJsonAsync<AnomalyEntireDetectResult>(new Uri(endpoint + BATCH_SERVICE_URL), defaultRequestHeaders, dataRequest);
        }
    }
}
