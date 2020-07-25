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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace SocialEbola.Lib.Animation
{
    public class CanvasMoveManagedAnimation : ManagedAnimation, ICanvasMoveAnimation
    {
        public double X { get; set; }
        public double Y { get; set; }
        public TimeSpan Duration { get; set; }
        public EasingFunctionBase EasingFunctionX { get; set; }
        public EasingFunctionBase EasingFunctionY { get; set; }

        public CanvasMoveManagedAnimation(double x, double y, TimeSpan duration, EasingFunctionBase easingFunctionX = null, EasingFunctionBase easingFunctionY = null)
        {
            X = x;
            Y = y;
            Duration = duration;
            EasingFunctionX = easingFunctionX ?? DefaultOut;
            EasingFunctionY = easingFunctionY ?? DefaultOut;
        }

        protected override Storyboard CreateStoryboard()
        {
            Storyboard board = new Storyboard();
            var timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, Element);
            Storyboard.SetTargetProperty(timeline, "(Canvas.Left)");
            var frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = X, EasingFunction = EasingFunctionX };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, Element);
            Storyboard.SetTargetProperty(timeline, "(Canvas.Top)");
            frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = Y, EasingFunction = EasingFunctionY };
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
            Canvas.SetTop(Element, ForceFinalValueRetained ? X : Canvas.GetTop(Element));
            Canvas.SetLeft(Element, ForceFinalValueRetained ? Y : Canvas.GetLeft(Element));
        }
    }
}
