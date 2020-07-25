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
    public class CanvasCircleMoveManagedAnimation : ManagedAnimation, ICanvasMoveAnimation
    {
        public double X { get; set; }
        public double Y { get; set; }
        public TimeSpan Duration { get; set; }
        public EasingFunctionBase EasingFunction { get; set; }
        public double Radius { get; set; }

        public CanvasCircleMoveManagedAnimation(double x, double y, double radius, TimeSpan duration, EasingFunctionBase easingFunction = null)
        {
            X = x;
            Y = y;
            Radius = radius;
            Duration = duration;
            EasingFunction = easingFunction ?? DefaultOut;
        }

        protected override Storyboard CreateStoryboard()
        {
            Storyboard board = new Storyboard();
            var timeline = new DoubleAnimationUsingKeyFrames();
            timeline.EnableDependentAnimation = true;
            var pathGenerator = new CircleGenerator() { Angle = 0, Center = new Point(X, Y), Radius = Radius };

            Storyboard.SetTarget(timeline, pathGenerator);
            Storyboard.SetTargetProperty(timeline, "Angle");

            var frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = 360, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            // add to retain as well. 
            Element.SetBinding(Canvas.LeftProperty, new Binding() { Path = new PropertyPath("X"), Source = pathGenerator });
            Element.SetBinding(Canvas.TopProperty, new Binding() { Path = new PropertyPath("Y"), Source = pathGenerator });

            return board;
        }

        public override void Retain()
        {
            Canvas.SetTop(Element, ForceFinalValueRetained ? X : Canvas.GetTop(Element));
            Canvas.SetLeft(Element, ForceFinalValueRetained ? Y : Canvas.GetLeft(Element));

            Element.ClearValue(Canvas.LeftProperty);
            Element.ClearValue(Canvas.TopProperty);
        }
    }
}
