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

using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceHelpers.Models
{
    public enum UserStoryType
    {
        BikeRental,
        Telcom,
        Manufacturing,
        Live
    }

    public enum GranType
    {
        minutely,
        hourly,
        daily,
        weekly,
        monthly,
        yearly,
    };

    public enum AnomalyDetectorServiceType
    {
        Streaming,
        Batch
    }

    public class DemoDetection
    {
        public AnomalyDetectorModelData DemoStory;
        public int Sensitivity;
        public AnomalyDetectorServiceType DetectionType;
        public int RunFrom;

        public DemoDetection(AnomalyDetectorServiceType detectionType, AnomalyDetectorModelData story, int sensitivity)
        {
            DemoStory = story;
            Sensitivity = sensitivity;
            DetectionType = detectionType;
            RunFrom = 0;
        }
    }

    public class AnomalyDetectorModelData
    {
        public const int DefaultMinimumStartIndex = 12;
        public readonly ADUserStory UserStory;
        public readonly List<TimeSeriesData> AllData;
        public readonly int IndexOfFirstValidPoint;

        private int _maxValue = int.MaxValue;
        public int MaxValue
        {
            get
            {
                if (_maxValue.CompareTo(int.MaxValue) == 0)
                {
                    if (AllData != null && AllData.Count > 0)
                    {
                        _maxValue = (int)AllData.Max(data => data.Value);
                    }
                }

                return _maxValue;
            }
        }

        private int _minValue = int.MinValue;
        public int MinValue
        {
            get
            {
                if (_minValue.CompareTo(int.MinValue) == 0)
                {
                    if (AllData != null && AllData.Count > 0)
                    {
                        _minValue = (int)AllData.Min(data => data.Value);
                    }
                }

                return _minValue;
            }
        }

        public AnomalyDetectorModelData(ADUserStory userStory)
        {
            UserStory = userStory;
            AllData = new List<TimeSeriesData>();
            IndexOfFirstValidPoint = GetIndexOfFirstPoint(UserStory.Granuarity.ToString());
        }

        private int GetIndexOfFirstPoint(String gran)
        {
            GranType granType = (GranType)Enum.Parse(typeof(GranType), gran, true);

            switch (granType)
            {
                case GranType.hourly:
                    return 168;
                case GranType.daily:
                    return 28;
                case GranType.weekly:
                    return 23;
                case GranType.monthly:
                    return 24;
                case GranType.yearly:
                    return 23;
                case GranType.minutely:
                    return 168;
                default:
                    return 28;
            }
        }
    }

    public class TimeSeriesData
    {
        public string Timestamp;
        public double Value;

        public TimeSeriesData(string time, string data)
        {
            Timestamp = time;
            try
            {
                Value = double.Parse(data);
            }
            catch (Exception)
            {
                Value = (double)int.Parse(data);
            }
        }

        public TimeSeriesData(string time, double data)
        {
            Timestamp = time;
            Value = data;
        }
    }

    public class AnomalyEntireDetectRequest
    {
        public List<TimeSeriesData> Series { get; set; }
        public double MaxAnomalyRatio { get; set; }
        public int Sensitivity { get; set; }
        public string Granularity { get; set; }
        public int CustomInterval { get; set; }
        public int? Period { get; set; }

        public AnomalyEntireDetectRequest()
        {
            Series = new List<TimeSeriesData>();
            MaxAnomalyRatio = 0.25;
            CustomInterval = 1;
            Period = null;
        }

        public bool ShouldSerializePeriod()
        {
            return (Period != null);
        }
    }

    public class AnomalyEntireDetectResult
    {
        public List<double> ExpectedValues { get; set; }
        public List<double> UpperMargins { get; set; }
        public List<double> LowerMargins { get; set; }
        public List<bool> IsAnomaly { get; set; }
        public List<bool> IsPositiveAnomaly { get; set; }
        public List<bool> IsNegativeAnomaly { get; set; }
        public int Period { get; set; }
    }

    public class AnomalyLastDetectRequest
    {
        public List<TimeSeriesData> Series { get; set; }
        public double MaxAnomalyRatio { get; set; }
        public int Sensitivity { get; set; }
        public string Granularity { get; set; }
        public int CustomInterval { get; set; }
        public int? Period { get; set; }

        public AnomalyLastDetectRequest()
        {
            Series = new List<TimeSeriesData>();
            MaxAnomalyRatio = 0.25;
            CustomInterval = 1;
            Period = null;
        }

        public bool ShouldSerializePeriod()
        {
            return (Period != null);
        }
    }

    public class AnomalyLastDetectResult
    {
        public double ExpectedValue { get; set; }
        public bool IsAnomaly { get; set; }
        public bool IsNegativeAnomaly { get; set; }
        public bool IsPositiveAnomaly { get; set; }
        public double LowerMargin { get; set; }
        public double UpperMargin { get; set; }
        public int Period { get; set; }
        public int SuggestedWindow { get; set; }
    }

    public class ADUserStory
    {
        public UserStoryType StoryType { get; set; }
        public string FilePath { get; set; }
        public GranType Granuarity { get; set; }
        public double MaxAnomalyRatio { get; set; }
        public int CustomInterval { get; set; } = 1;
        public int? Period { get; set; }
    }
}
