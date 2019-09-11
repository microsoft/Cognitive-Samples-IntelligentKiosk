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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;

namespace IntelligentKioskSample.Views.AnomalyDetector
{
    public class AnomalyDetectorScenarioLoader
    {
        private static readonly StorageFolder StorageFolder = Package.Current.InstalledLocation;
        public static readonly IDictionary<AnomalyDetectionScenarioType, AnomalyDetectionScenario> AllModelData = new Dictionary<AnomalyDetectionScenarioType, AnomalyDetectionScenario>();

        public static async Task InitUserStories()
        {
            foreach (KeyValuePair<AnomalyDetectionScenarioType, AnomalyDetectionScenario> scenario in AllScenarios)
            {
                AllModelData.Add(scenario.Key, await LoadTimeSeriesData(scenario.Value));
            }
        }

        public static int GetTimeOffsetInMinute(GranType granType)
        {
            switch (granType)
            {
                case GranType.hourly:
                    return 60;
                case GranType.minutely:
                    return 1;
                default:
                    return 1;
            }
        }

        private static async Task<AnomalyDetectionScenario> LoadTimeSeriesData(AnomalyDetectionScenario scenario)
        {
            if (!string.IsNullOrEmpty(scenario.FilePath))
            {
                StorageFile sampleFile = await StorageFolder.GetFileAsync(scenario.FilePath);
                IList<string> csvContents = await FileIO.ReadLinesAsync(sampleFile, UnicodeEncoding.Utf8);

                foreach (string record in csvContents)
                {
                    string[] allValues = record.Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                    scenario.AllData.Add(new TimeSeriesData(allValues[0], allValues[1]));
                }
            }

            return scenario;
        }

        public static readonly IDictionary<AnomalyDetectionScenarioType, AnomalyDetectionScenario> AllScenarios = new Dictionary<AnomalyDetectionScenarioType, AnomalyDetectionScenario>
        {
            { AnomalyDetectionScenarioType.BikeRental, new AnomalyDetectionScenario
                                            {
                                                ScenarioType = AnomalyDetectionScenarioType.BikeRental,
                                                FilePath = "Assets\\AnomalyDetector\\AnomalyDetector-Bike.csv",
                                                Granularity = GranType.hourly,
                                                MaxAnomalyRatio = 0.1,
                                                CustomInterval = 8,
                                                Period = 21
                                            }
            },
            { AnomalyDetectionScenarioType.Telecom, new AnomalyDetectionScenario
                                            {
                                                ScenarioType = AnomalyDetectionScenarioType.Telecom,
                                                FilePath = "Assets\\AnomalyDetector\\AnomalyDetector-Telcom.csv",
                                                Granularity = GranType.daily,
                                                MaxAnomalyRatio = 0.15,
                                                Period = 7
                                            }
            },
            { AnomalyDetectionScenarioType.Manufacturing, new AnomalyDetectionScenario
                                            {
                                                ScenarioType = AnomalyDetectionScenarioType.Manufacturing,
                                                FilePath = "Assets\\AnomalyDetector\\AnomalyDetector-Manufacture.csv",
                                                Granularity = GranType.hourly,
                                                MaxAnomalyRatio = 0.10,
                                                CustomInterval = 2,
                                                Period = 84
                                            }
            },
            { AnomalyDetectionScenarioType.Live, new AnomalyDetectionScenario
                                            {
                                                ScenarioType = AnomalyDetectionScenarioType.Live,
                                                FilePath = string.Empty,
                                                Granularity = GranType.hourly,
                                                MaxAnomalyRatio = 0.2,
                                                CustomInterval = 1
                                            }
            },
        };
    }
}
