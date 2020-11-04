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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls
{
    public sealed partial class HorizontalStackedBarChartControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(HorizontalStackedBarChartControl),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public HorizontalStackedBarChartControl()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        public void GenerateChart(List<ChartItem> chartItems, string subtitle = "")
        {
            this.subtitleTextBlock.Text = subtitle;

            this.chartGrid.Children.Clear();
            this.chartGrid.ColumnDefinitions.Clear();
            for (int index = 0; index < chartItems.Count; index++)
            {
                var item = chartItems[index];

                if (item.Value >= 0)
                {
                    double percentage = Math.Round(item.Value * 100, 1);
                    ColumnDefinition column = new ColumnDefinition
                    {
                        Width = new GridLength(item.Value, GridUnitType.Star)
                    };
                    var border = new Border()
                    {
                        Background = item.Background,
                        Height = 24,
                        Child = new TextBlock()
                        {
                            Text = $"{percentage} %",
                            Foreground = item.Foreground,
                            TextWrapping = TextWrapping.NoWrap,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    };
                    ToolTip toolTip = new ToolTip
                    {
                        Content = percentage > 0 ? $"{item.Name}: {percentage} %" : $"{item.Name}: < 1 %"
                    };
                    ToolTipService.SetToolTip(border, toolTip);

                    this.chartGrid.ColumnDefinitions.Add(column);
                    this.chartGrid.Children.Add(border);
                    Grid.SetColumn(border, index);
                }
            }

            this.chartLegendItemsControl.ItemsSource = chartItems.Select(i => new Tuple<string, SolidColorBrush>(i.Name, i.Background));
        }
    }

    public class ChartItem
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public SolidColorBrush Background { get; set; }
        public SolidColorBrush Foreground { get; set; } = new SolidColorBrush(Colors.White);
    }
}
