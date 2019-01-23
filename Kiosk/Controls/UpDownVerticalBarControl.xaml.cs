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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public sealed partial class UpDownVerticalBarControl : UserControl
    {
        static SolidColorBrush BackgroundColor = new SolidColorBrush(Colors.Transparent);

        public UpDownVerticalBarControl()
        {
            this.InitializeComponent();
        }

        public void DrawDataPoint(double value, Brush barColor, Image toolTip = null)
        {
            if (value > 0)
            {
                value *= 0.5;
                topRowDefinition.Height = new GridLength(0.5 - value, GridUnitType.Star);
                upBarRowDefinition.Height = new GridLength(value, GridUnitType.Star);
                downBarRowDefinition.Height = new GridLength(0);
                bottomRowDefinition.Height = new GridLength(0.5, GridUnitType.Star);
            }
            else
            {
                value *= -0.5;
                topRowDefinition.Height = new GridLength(0.5, GridUnitType.Star);
                upBarRowDefinition.Height = new GridLength(0);
                downBarRowDefinition.Height = new GridLength(value, GridUnitType.Star);
                bottomRowDefinition.Height = new GridLength(0.5 - value, GridUnitType.Star);
            }

            Border slice = new Border { Background = BackgroundColor };
            Grid.SetRow(slice, 0);
            graph.Children.Add(slice);

            slice = new Border { Background = barColor };
            Grid.SetRow(slice, 1);
            graph.Children.Add(slice);

            slice = new Border { Background = barColor };
            Grid.SetRow(slice, 2);
            graph.Children.Add(slice);

            slice = new Border { Background = BackgroundColor };
            Grid.SetRow(slice, 3);
            graph.Children.Add(slice);

            AddFlyoutToElement(graph, toolTip);
        }

        private void AddFlyoutToElement(FrameworkElement element, Image toolTip)
        {
            if (toolTip != null)
            {
                FlyoutBase.SetAttachedFlyout(element, new Flyout { Content = toolTip });

                element.PointerReleased += (s, e) =>
                {
                    FlyoutBase.ShowAttachedFlyout(element);
                };
            }
        }
    }
}
