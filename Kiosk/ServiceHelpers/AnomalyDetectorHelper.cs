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
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ServiceHelpers
{
    public class AnomalyDetectorHelper : ServiceBase
    {
        private static readonly string HEADER_SUB_KEY = "Ocp-Apim-Subscription-Key";
        private static readonly Dictionary<AnomalyDetectorServiceType, Uri> Services_URL = new Dictionary<AnomalyDetectorServiceType, Uri>
        {
            { AnomalyDetectorServiceType.Streaming, new Uri("https://westus2.api.cognitive.microsoft.com/anomalydetector/v1.0/timeseries/last/detect") },
            { AnomalyDetectorServiceType.Batch, new Uri("https://westus2.api.cognitive.microsoft.com/anomalydetector/v1.0/timeseries/entire/detect") }
        };

        public static readonly IDictionary<UserStoryType, ADUserStory> AllUserStories = new Dictionary<UserStoryType, ADUserStory>
        {
            { UserStoryType.BikeRental, new ADUserStory
                                            {
                                                StoryType = UserStoryType.BikeRental,
                                                FilePath = "Assets\\AnomalyDetector\\AnomalyDetector-Bike.csv",
                                                Granuarity = GranType.hourly,
                                                MaxAnomalyRatio = 0.1
                                            }
            },
            { UserStoryType.Telcom, new ADUserStory
                                            {
                                                StoryType = UserStoryType.Telcom,
                                                FilePath = "Assets\\AnomalyDetector\\AnomalyDetector-Telcom.csv",
                                                Granuarity = GranType.daily,
                                                MaxAnomalyRatio = 0.25
                                            }
            },
            { UserStoryType.Manufacturing, new ADUserStory
                                            {
                                                StoryType = UserStoryType.Manufacturing,
                                                FilePath = "Assets\\AnomalyDetector\\AnomalyDetector-Manufacture.csv",
                                                Granuarity = GranType.minutely,
                                                MaxAnomalyRatio = 0.25,
                                                CustomInterval = 30,
                                                Period = 336
                                            }
            },
            { UserStoryType.Live, new ADUserStory
                                            {
                                                StoryType = UserStoryType.Live,
                                                FilePath = string.Empty,
                                                Granuarity = GranType.hourly,
                                                MaxAnomalyRatio = 0.2,
                                                CustomInterval = 1
                                            }
            },
        };

        public static readonly IDictionary<UserStoryType, AnomalyDetectorModelData> AllModelData = new Dictionary<UserStoryType, AnomalyDetectorModelData>();

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

        public const int DefaultDurationOfLiveDemoInSecond = 480;

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

        public static async Task InitUserStories()
        {
            foreach(KeyValuePair<UserStoryType, ADUserStory> aDUserStory in AllUserStories)
            {
                AllModelData.Add(aDUserStory.Key, await AnomalyDataLoader.LoadTimeSeriesData(aDUserStory.Value));
            }
        }

        public static async Task<AnomalyLastDetectResult> GetStreamingDetectionResult(AnomalyDetectorModelData demoData, int dataPointIndex, int sensitivity)
        {
            AnomalyLastDetectRequest dataRequest = new AnomalyLastDetectRequest
            {
                Sensitivity = sensitivity,
                MaxAnomalyRatio = demoData.UserStory.MaxAnomalyRatio,
                Granularity = demoData.UserStory.Granuarity.ToString(),
                CustomInterval = demoData.UserStory.CustomInterval,
                Period = demoData.UserStory.Period
            };

            int minStartIndex = -1;

            if (dataPointIndex >= demoData.IndexOfFirstValidPoint)
            {
                minStartIndex = demoData.IndexOfFirstValidPoint;
            }
            else if (dataPointIndex < demoData.IndexOfFirstValidPoint && dataPointIndex >= 11)
            {
                minStartIndex = dataPointIndex;
            }

            if (minStartIndex > -1)
            {
                dataRequest.Series = demoData.AllData.GetRange(dataPointIndex - minStartIndex, minStartIndex + 1);

                AnomalyLastDetectResult result = await HttpClientUtility.PostAsJsonAsync<AnomalyLastDetectResult>(Services_URL[AnomalyDetectorServiceType.Streaming], defaultRequestHeaders, dataRequest);

                return result;
            }

            return null;
        }

        public static async Task<AnomalyEntireDetectResult> GetBatchDetectionResult(AnomalyDetectorModelData demoData, int sensitivity)
        {
            AnomalyEntireDetectRequest dataRequest = new AnomalyEntireDetectRequest
            {
                Sensitivity = sensitivity,
                MaxAnomalyRatio = demoData.UserStory.MaxAnomalyRatio,
                Granularity = demoData.UserStory.Granuarity.ToString(),
                CustomInterval = demoData.UserStory.CustomInterval,
                Period = demoData.UserStory.Period
            };

            dataRequest.Series = demoData.AllData;

            AnomalyEntireDetectResult result = await HttpClientUtility.PostAsJsonAsync<AnomalyEntireDetectResult>(Services_URL[AnomalyDetectorServiceType.Batch], defaultRequestHeaders, dataRequest);

            return result;
        }
    }

    public class AnomalyDataLoader
    {
        private static readonly StorageFolder StorageFolder = Package.Current.InstalledLocation;

        public static async Task<AnomalyDetectorModelData> LoadTimeSeriesData(ADUserStory userStory)
        {
            AnomalyDetectorModelData result = new AnomalyDetectorModelData(userStory);

            if (!string.IsNullOrEmpty(userStory.FilePath))
            {
                StorageFile sampleFile = await StorageFolder.GetFileAsync(userStory.FilePath);
                IList<string> csvContents = await FileIO.ReadLinesAsync(sampleFile, UnicodeEncoding.Utf8);

                foreach (string record in csvContents)
                {
                    string[] allValues = record.Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                    result.AllData.Add(new TimeSeriesData(allValues[0], allValues[1]));
                }
            }

            return result;
        }
    }
}
