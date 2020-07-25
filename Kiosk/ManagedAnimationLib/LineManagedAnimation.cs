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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace SocialEbola.Lib.Animation
{
    public class LineManagedAnimation : ManagedAnimation<Line>
    {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
        public TimeSpan Duration { get; set; }
        public EasingFunctionBase EasingFunction { get; set; }

        public LineManagedAnimation(Point pt1, Point pt2, TimeSpan duration, EasingFunctionBase easingFunction = null)
        {
            Point1 = pt1;
            Point2 = pt2;
            Duration = duration;
            EasingFunction = easingFunction ?? DefaultOut;
        }

        protected override Storyboard CreateStoryboard()
        {
            Storyboard board = new Storyboard();
            var timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, Element);
            Storyboard.SetTargetProperty(timeline, "X1");
            var frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = Point1.X, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, Element);
            Storyboard.SetTargetProperty(timeline, "Y1");
            frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = Point1.Y, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, Element);
            Storyboard.SetTargetProperty(timeline, "X2");
            frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = Point2.X, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, Element);
            Storyboard.SetTargetProperty(timeline, "Y2");
            frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = Point2.Y, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            return board;
        }

        public override bool Equivalent(ManagedAnimation managedAnimation)
        {
            return managedAnimation is ICanvasMoveAnimation;
        }

        public override void Retain()
        {
            Element.X1 = Point1.X;
            Element.Y1 = Point1.Y;
            Element.X2 = Point2.X;
            Element.Y2 = Point2.Y;
        }
    }
}
