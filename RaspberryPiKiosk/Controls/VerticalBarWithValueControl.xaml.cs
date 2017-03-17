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

using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public sealed partial class VerticalBarWithValueControl : UserControl
    {
        public VerticalBarWithValueControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty BarValueProperty =
            DependencyProperty.Register(
            "BarValue",
            typeof(int?),
            typeof(VerticalBarWithValueControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty ShowBarValueProperty =
            DependencyProperty.Register(
            "ShowBarValue",
            typeof(bool),
            typeof(VerticalBarWithValueControl),
            new PropertyMetadata(true)
        );

        public static readonly DependencyProperty BarPercentageProperty =
            DependencyProperty.Register(
            "BarPercentange",
            typeof(double),
            typeof(VerticalBarWithValueControl),
            new PropertyMetadata(0.01)
            );

        public static readonly DependencyProperty BarColor1Property =
            DependencyProperty.Register(
            "BarColor1",
            typeof(SolidColorBrush),
            typeof(VerticalBarWithValueControl),
            new PropertyMetadata(new SolidColorBrush(Colors.Red))
            );

        public static readonly DependencyProperty BarColor2Property =
            DependencyProperty.Register(
            "BarColor2",
            typeof(SolidColorBrush),
            typeof(VerticalBarWithValueControl),
            new PropertyMetadata(new SolidColorBrush(Colors.Yellow))
            );

        public SolidColorBrush BarColor1
        {
            get { return (SolidColorBrush)GetValue(BarColor1Property); }
            set { SetValue(BarColor1Property, (SolidColorBrush)value); }
        }

        public SolidColorBrush BarColor2
        {
            get { return (SolidColorBrush)GetValue(BarColor2Property); }
            set { SetValue(BarColor2Property, (SolidColorBrush)value); }
        }

        public int? BarValue
        {
            get { return (int?)GetValue(BarValueProperty); }
            set { SetValue(BarValueProperty, (int?)value); }
        }

        public bool ShowBarValue
        {
            get { return (bool)GetValue(ShowBarValueProperty); }
            set { SetValue(ShowBarValueProperty, (bool)value); }
        }

        public double BarPercentage
        {
            get { return (double)GetValue(BarPercentageProperty); }
            set { SetValue(BarPercentageProperty, (double)value); }
        }

        public void Update(int barValue1, int barValue2, double barPercentage)
        {
            if (double.IsNaN(barPercentage))
            {
                barPercentage = 0;
            }

            this.BarValue = !this.ShowBarValue || (barValue1 == 0 && barValue2 == 0) ? null : (int?)barValue1 + barValue2;
            this.BarPercentage = Math.Min(Math.Max(0.005, barPercentage), 0.80);

            this.barRowDefinition.Height = new GridLength(this.BarPercentage, GridUnitType.Star);
            this.valueRowDefinition.Height = new GridLength(1 - this.BarPercentage, GridUnitType.Star);

            if (barValue1 != 0 || barValue2 != 0)
            {
                double firstBarProportion = (double)barValue1 / (barValue1 + barValue2);
                this.bar1RowDefinition.Height = new GridLength(firstBarProportion, GridUnitType.Star);
                this.bar2RowDefinition.Height = new GridLength(1 - firstBarProportion, GridUnitType.Star);
            }
        }
    }
}