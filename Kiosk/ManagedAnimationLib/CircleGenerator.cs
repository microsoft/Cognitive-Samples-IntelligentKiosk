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
using Windows.Foundation;
using Windows.UI.Xaml;

namespace SocialEbola.Lib.Animation
{
    public class CircleGenerator : DependencyObject
    {
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point), typeof(CircleGenerator), new PropertyMetadata(new Point(), OnPropertyChanged));
        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(CircleGenerator), new PropertyMetadata(0.0, OnPropertyChanged));
        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(CircleGenerator), new PropertyMetadata(0.0, OnPropertyChanged));
        public static readonly DependencyProperty PointProperty = DependencyProperty.Register("Point", typeof(Point), typeof(CircleGenerator), new PropertyMetadata(new Point()));
        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(double), typeof(CircleGenerator), new PropertyMetadata(0));
        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(double), typeof(CircleGenerator), new PropertyMetadata(0));

        public double X
        {
            get { return (double)GetValue(XProperty); }
            private set { SetValue(XProperty, value); }
        }

        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public Point Center
        {
            set { SetValue(CenterProperty, value); }
            get { return (Point)GetValue(CenterProperty); }
        }

        public double Radius
        {
            set { SetValue(RadiusProperty, value); }
            get { return (double)GetValue(RadiusProperty); }
        }

        public double Angle
        {
            set { SetValue(AngleProperty, value); }
            get { return (double)GetValue(AngleProperty); }
        }

        public Point Point
        {
            protected set { SetValue(PointProperty, value); }
            get { return (Point)GetValue(PointProperty); }
        }

        static void OnPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((CircleGenerator)obj).OnPropertyChanged(args);
        }

        void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            double x = this.Center.X + this.Radius * Math.Sin(Math.PI * this.Angle / 180);
            double y = this.Center.Y - this.Radius * Math.Cos(Math.PI * this.Angle / 180);
            this.Point = new Point(x, y);
            X = x;
            Y = y;
        }
    }
}
