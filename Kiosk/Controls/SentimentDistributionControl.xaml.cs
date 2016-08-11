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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public sealed partial class SentimentDistributionControl : UserControl
    {
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(
            "HeaderText",
            typeof(string),
            typeof(SentimentDistributionControl),
            new PropertyMetadata("Distribution")
            );

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, (string)value); }
        }

        public SentimentDistributionControl()
        {
            this.InitializeComponent();
        }

        public void UpdateData(IEnumerable<double> sentiments)
        {
            if (sentiments.Any())
            {
                this.chartHostGrid.Visibility = Visibility.Visible;

                // group at one decimal point precision
                var sentimentGroups = sentiments.GroupBy(s => Math.Round(s, 1));
                int largestGroupSize = sentimentGroups.OrderByDescending(g => g.Count()).First().Count();

                var barCharts = this.chartGrid.Children.Where(c => typeof(VerticalBarWithValueControl) == c.GetType()).Cast<VerticalBarWithValueControl>().ToArray();

                for (int i = 0; i < barCharts.Length; i++)
                {
                    var group = sentimentGroups.FirstOrDefault(g => (g.Key * 10) == i);
                    if (group != null)
                    {
                        barCharts[i].Update(group.Count(), 0, (double)group.Count() / largestGroupSize);
                    }
                    else
                    {
                        barCharts[i].Update(0, 0, 0);
                    }
                }

                int positivePercentage = (sentiments.Where(s => s >= 0.5).Count() * 100) / sentiments.Count();
                this.overallPositiveTextBlock.Text = string.Format("{0}%", positivePercentage);
                this.overallNegativeTextBlock.Text = string.Format("{0}%", 100 - positivePercentage);
            }
            else
            {
                this.overallPositiveTextBlock.Text = this.overallNegativeTextBlock.Text = "";

                var barCharts = this.chartGrid.Children.Where(c => typeof(VerticalBarWithValueControl) == c.GetType()).Cast<VerticalBarWithValueControl>().ToArray();

                for (int i = 0; i < barCharts.Length; i++)
                {
                    barCharts[i].Update(0, 0, 0);
                }

                this.chartHostGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
