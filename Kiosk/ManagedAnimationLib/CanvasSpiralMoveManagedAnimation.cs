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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Animation;

namespace SocialEbola.Lib.Animation
{
    public class CanvasSpiralMoveManagedAnimation : ManagedAnimation, ICanvasMoveAnimation
    {
        private CircleGenerator m_generator;
        public TimeSpan Duration { get; set; }
        public EasingFunctionBase EasingFunction { get; set; }
        public SpiralData Data { get; set; }
        public bool CalculateForCenter { get; set; }

        public CanvasSpiralMoveManagedAnimation(SpiralData data, bool calculateForCenter, TimeSpan duration, EasingFunctionBase easingFunction = null)
        {
            Duration = duration;
            EasingFunction = easingFunction ?? DefaultOut;
            Data = data;
            CalculateForCenter = calculateForCenter;
        }

        protected override Storyboard CreateStoryboard()
        {
            Storyboard board = new Storyboard();
            Point center = new Point(Data.X, Data.Y);
            if (CalculateForCenter)
            {
                center = Element.GetTopLeftForCenter(center);
            }
            var pathGenerator = new CircleGenerator() { Angle = Data.AngleFrom, Center = center, Radius = Data.RadiusFrom };
            m_generator = pathGenerator;

            var timeline = new DoubleAnimationUsingKeyFrames();
            timeline.EnableDependentAnimation = true;
            Storyboard.SetTarget(timeline, pathGenerator);
            Storyboard.SetTargetProperty(timeline, "Angle");
            var frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = Data.AngleTo, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);

            board.Children.Add(timeline);

            timeline = new DoubleAnimationUsingKeyFrames();
            timeline.EnableDependentAnimation = true;
            Storyboard.SetTarget(timeline, pathGenerator);
            Storyboard.SetTargetProperty(timeline, "Radius");
            frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = Data.RadiusTo, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);

            board.Children.Add(timeline);

            // add to retain as well. 
            var conv = new ElementCenterToCanvas(Element);
            Element.SetBinding(Canvas.LeftProperty, new Binding() { Path = new PropertyPath("X"), Source = pathGenerator, Converter = conv, ConverterParameter = "X" });
            Element.SetBinding(Canvas.TopProperty, new Binding() { Path = new PropertyPath("Y"), Source = pathGenerator, Converter = conv, ConverterParameter = "Y" });

            return board;
        }

        public override void Retain()
        {
            var location = Element.GetCanvasLocation();
            Element.ClearValue(Canvas.LeftProperty);
            Element.ClearValue(Canvas.TopProperty);

            Canvas.SetLeft(Element, ForceFinalValueRetained ? m_generator.X : location.X);
            Canvas.SetTop(Element, ForceFinalValueRetained ? m_generator.Y : location.Y);
        }

        public class SpiralData
        {
            public SpiralData(double x, double y, double angleFrom, double angleTo, double radiusFrom, double radiusTo)
            {
                AngleFrom = angleFrom;
                AngleTo = angleTo;
                RadiusFrom = radiusFrom;
                RadiusTo = radiusTo;
                X = x;
                Y = y;
            }

            public SpiralData()
            {
            }

            public double AngleFrom { get; set; }
            public double AngleTo { get; set; }
            public double RadiusFrom { get; set; }
            public double RadiusTo { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class ElementCenterToCanvas : IValueConverter
        {
            public FrameworkElement Element { get; set; }
            public ElementCenterToCanvas(FrameworkElement element)
            {
                Element = element;
            }

            public object Convert(object value, Type targetType, object parameter, string language)
            {
                double loc = (double)value;
                string p = parameter.ToString();
                var pt = Element.GetTopLeftForCenter(loc, loc);
                return p == "X" ? pt.X : pt.Y;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }
    }
}
