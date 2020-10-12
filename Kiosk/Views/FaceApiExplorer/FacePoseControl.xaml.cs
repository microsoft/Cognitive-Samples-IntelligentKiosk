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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Views.FaceApiExplorer
{
    public sealed partial class FacePoseControl : UserControl
    {
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
            "IconSource",
            typeof(string),
            typeof(FacePoseControl),
            new PropertyMetadata(""));

        public string IconSource
        {
            get { return (string)GetValue(IconSourceProperty); }
            set { SetValue(IconSourceProperty, value); }
        }

        public static readonly DependencyProperty MaxProgressBarProperty =
            DependencyProperty.Register(
            "MaxProgressBar",
            typeof(int),
            typeof(FacePoseControl),
            new PropertyMetadata(100));

        public int MaxProgressBar
        {
            get { return (int)GetValue(MaxProgressBarProperty); }
            set { SetValue(MaxProgressBarProperty, value); }
        }

        public FacePoseControl()
        {
            this.InitializeComponent();
        }

        public void DrawFacePoseData(double value, double[] angleArr)
        {
            if (value >= 0)
            {
                this.positiveProgressBar.Value = value;
                this.negativeProgressBar.Value = 0;
            }
            else
            {
                this.positiveProgressBar.Value = 0;
                this.negativeProgressBar.Value = Math.Abs(value);
            }

            for (int i = 0; i < iconGrid.Children.Count; i++)
            {
                if (iconGrid.Children[i] is Image image && i < angleArr.Length)
                {
                    image.RenderTransform = new RotateTransform { Angle = angleArr[i] };
                }
            }
        }
    }
}
