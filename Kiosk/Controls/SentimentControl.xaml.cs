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

namespace IntelligentKioskSample.Controls
{
    /// <summary>
    /// Interaction logic for SentimentControl.xaml
    /// </summary>
    public partial class SentimentControl : UserControl
    {
        public static readonly DependencyProperty SentimentProperty =
            DependencyProperty.Register(
            "Sentiment",
            typeof(double),
            typeof(SentimentControl),
            new PropertyMetadata(0.5, SentimentPropertyChangedCallback)
            );

        public double Sentiment
        {
            get { return (double)GetValue(SentimentProperty); }
            set { SetValue(SentimentProperty, (double)value); }
        }

        static void SentimentPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SentimentControl control = (SentimentControl)d;
            control.UpdateSentimentPointer();
        }

        public SentimentControl()
        {
            InitializeComponent();
        }

        private void OnSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            this.UpdateSentimentPointer();
        }

        private void UpdateSentimentPointer()
        {
            double totalLength = this.GuideLine.ActualWidth;
            this.Pointer.SetValue(Canvas.LeftProperty, totalLength * this.Sentiment);
            this.PointerText.Text = String.Format("{0:N2}", this.Sentiment);
        }
    }
}
