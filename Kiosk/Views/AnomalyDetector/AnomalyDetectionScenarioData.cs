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
using System.Collections.Generic;
using System.Linq;

namespace IntelligentKioskSample.Views.AnomalyDetector
{
    public enum AnomalyDetectionScenarioType
    {
        BikeRental,
        Telecom,
        Manufacturing,
        Live
    }

    public enum AnomalyDetectorServiceType
    {
        //Batch,
        Streaming
    }

    public enum GranType
    {
        minutely,
        hourly,
        daily,
        weekly,
        monthly,
        yearly,
    }

    public class AnomalyDetectionScenario
    {
        public const int DefaultRequiredPoints = 13;

        public AnomalyDetectionScenarioType ScenarioType { get; set; }
        public string FilePath { get; set; }
        public GranType Granularity { get; set; }
        public double MaxAnomalyRatio { get; set; }
        public int CustomInterval { get; set; } = 1;
        public int? Period { get; set; }

        public readonly List<TimeSeriesData> AllData;

        private double _maxValue = double.MaxValue;
        public double MaxValue
        {
            get
            {
                if (_maxValue.CompareTo(double.MaxValue) == 0)
                {
                    if (AllData != null && AllData.Count > 0)
                    {
                        _maxValue = (double)AllData.Max(data => data.Value);
                    }
                }

                return _maxValue;
            }
        }

        private double _minValue = double.MinValue;
        public double MinValue
        {
            get
            {
                if (_minValue.CompareTo(double.MinValue) == 0)
                {
                    if (AllData != null && AllData.Count > 0)
                    {
                        _minValue = (double)AllData.Min(data => data.Value);
                    }
                }

                return _minValue;
            }
        }

        private int _minIndexOfRequiredPoints = -1;
        public int MinIndexOfRequiredPoints
        {
            get
            {
                if (_minIndexOfRequiredPoints == -1)
                {
                    _minIndexOfRequiredPoints = (Period == null ? GetRequiredPointsPerGran(Granularity) : Period.Value * 4) - 1;
                }

                return _minIndexOfRequiredPoints;
            }
        }

        public AnomalyDetectionScenario()
        {
            AllData = new List<TimeSeriesData>();
        }

        private int GetRequiredPointsPerGran(GranType gran)
        {
            switch (gran)
            {
                case GranType.hourly:
                    return 168;
                case GranType.daily:
                    return 28;
                default:
                    return DefaultRequiredPoints;
            }
        }
    }

    public class AnomalyInfo
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public string ExpectedValue { get; set; }
    }
}
