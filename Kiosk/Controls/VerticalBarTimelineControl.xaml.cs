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

using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public enum BarType
    {
        Centered,
        UpDown
    }

    public enum WrapBehavior
    {
        Clear,
        Slide
    }

    public sealed partial class VerticalBarTimelineControl : UserControl
    {
        static SolidColorBrush DefaultBarColor = new SolidColorBrush(Color.FromArgb(0xaa, 0xff, 0xff, 0xff));

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
            "Header",
            typeof(string),
            typeof(VerticalBarTimelineControl),
            new PropertyMetadata("")
            );

        public static readonly DependencyProperty BarTypeProperty =
            DependencyProperty.Register(
            "BarType",
            typeof(BarType),
            typeof(VerticalBarTimelineControl),
            new PropertyMetadata(BarType.UpDown)
            );

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public BarType BarType
        {
            get { return (BarType)GetValue(BarTypeProperty); }
            set { SetValue(BarTypeProperty, value); }
        }

        public VerticalBarTimelineControl()
        {
            this.InitializeComponent();
        }

        private double leftMargin;
        public void DrawDataPoint(double value, Brush barColor = null, Image toolTip = null, WrapBehavior wrapBehavior = WrapBehavior.Clear)
        {
            if (leftMargin >= graph.ActualWidth)
            {
                if (wrapBehavior == WrapBehavior.Clear)
                {
                    leftMargin = 0;
                    graph.Children.Clear();
                }
                else
                {
                    // Remove first element and shift all the others to the left by 1
                    graph.Children.RemoveAt(0);

                    double widthPerChild = 6;
                    for (int i = 0; i < graph.Children.Count; i++)
                    {
                        (graph.Children[i] as Control).Margin = new Thickness(widthPerChild * i, 0, 0, 0);
                    }

                    leftMargin -= widthPerChild;

                    // Remove 20% element of the elements (from the beginning) and shift all the others to the left by that ammount
                    //int removeCount = graph.Children.Count / 5;
                    //for (int i = 0; i < removeCount; i++)
                    //{
                    //    graph.Children.RemoveAt(0);
                    //}

                    //double widthPerChild = 6;
                    //for (int i = 0; i < graph.Children.Count; i++)
                    //{
                    //    (graph.Children[i] as Control).Margin = new Thickness(widthPerChild * i, 0, 0, 0);
                    //}

                    //leftMargin = (graph.Children.Count - 1) * widthPerChild;
                }
            }

            Control bar;
            if (this.BarType == BarType.UpDown)
            {
                var upDownBar = new UpDownVerticalBarControl();
                upDownBar.DrawDataPoint(value, barColor != null ? barColor : DefaultBarColor, toolTip);

                bar = upDownBar;
            }
            else
            {
                var centeredBar = new CenteredVerticalBarControl();
                centeredBar.DrawDataPoint(value, barColor != null ? barColor : DefaultBarColor, toolTip);

                bar = centeredBar;
            }

            bar.Width = 4;
            bar.HorizontalAlignment = HorizontalAlignment.Left;
            bar.Margin = new Thickness(leftMargin += (bar.Width + 2), 0, 0, 0);

            graph.Children.Add(bar);
        }

        public void Clear()
        {
            leftMargin = 0;
            graph.Children.Clear();
        }
    }
}
