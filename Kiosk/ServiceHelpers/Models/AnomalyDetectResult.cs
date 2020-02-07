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
using System.Globalization;

namespace ServiceHelpers.Models
{
    public class AnomalyDetectionRequest
    {
        public List<TimeSeriesData> Series { get; set; }
        public double MaxAnomalyRatio { get; set; }
        public int Sensitivity { get; set; }
        public string Granularity { get; set; }
        public int CustomInterval { get; set; }
        public int? Period { get; set; }

        public AnomalyDetectionRequest()
        {
            Series = new List<TimeSeriesData>();
            MaxAnomalyRatio = 0.25;
            CustomInterval = 1;
            Period = null;
        }

        public bool ShouldSerializePeriod()
        {
            return Period != null;
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

    public class TimeSeriesData
    {
        public string Timestamp;
        public double Value;

        public TimeSeriesData(string time, string data)
        {
            Timestamp = time;
            try
            {
                Value = double.Parse(data, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                Value = (double)int.Parse(data, CultureInfo.InvariantCulture);
            }
        }

        public TimeSeriesData(string time, double data)
        {
            Timestamp = time;
            Value = data;
        }
    }
}
