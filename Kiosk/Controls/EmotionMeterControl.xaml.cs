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
    public sealed partial class EmotionMeterControl : UserControl
    {
        public EmotionMeterControl()
        {
            this.InitializeComponent();
            //this.DataContext = this;
        }

        public static readonly DependencyProperty EmotionNameProperty =
            DependencyProperty.Register(
            "EmotionName",
            typeof(string),
            typeof(EmotionMeterControl),
            new PropertyMetadata("")
            );

        public static readonly DependencyProperty EmotionValueProperty =
            DependencyProperty.Register(
            "EmotionValue",
            typeof(float),
            typeof(EmotionMeterControl),
            new PropertyMetadata(0)
            );

        public static readonly DependencyProperty MeterForegroundProperty =
            DependencyProperty.Register(
            "MeterForeground",
            typeof(SolidColorBrush),
            typeof(EmotionMeterControl),
            new PropertyMetadata(new SolidColorBrush(Colors.White))
            );

        public SolidColorBrush MeterForeground
        {
            get { return (SolidColorBrush)GetValue(MeterForegroundProperty); }
            set { SetValue(MeterForegroundProperty, (SolidColorBrush)value); }
        }

        public string EmotionName
        {
            get { return (string)GetValue(EmotionNameProperty); }
            set { SetValue(EmotionNameProperty, (string)value); }
        }
        public float EmotionValue
        {
            get { return (float)GetValue(EmotionValueProperty); }
            set { SetValue(EmotionValueProperty, (float)value); }
        }
    }
}
