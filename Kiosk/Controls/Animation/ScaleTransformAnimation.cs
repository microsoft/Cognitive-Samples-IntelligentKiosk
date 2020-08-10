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

using IntelligentKioskSample.Extensions;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace IntelligentKioskSample.Controls.Animation
{
    public class ScaleTransformAnimation : BaseAnimation
    {
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public EasingFunctionBase Easing { get; set; }
        public TimeSpan Duration { get; set; }

        public ScaleTransformAnimation(double scaleX, double scaleY, TimeSpan duration, EasingFunctionBase easing = null)
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
            Easing = easing ?? DefaultOut;
            Duration = duration;
        }

        protected override Storyboard CreateStoryboard()
        {
            var transform = Transform;

            Storyboard board = new Storyboard();
            var timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, transform);
            Storyboard.SetTargetProperty(timeline, "ScaleX");
            var frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = ScaleX, EasingFunction = Easing };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, transform);
            Storyboard.SetTargetProperty(timeline, "ScaleY");
            frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = ScaleY, EasingFunction = Easing };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            return board;
        }

        private CompositeTransform Transform
        {
            get
            {
                return Element.EnsureRenderTransform<CompositeTransform>();
            }
        }

        public override void Retain()
        {
            var transform = Transform;
            transform.ScaleX = ForceFinalValueRetained ? ScaleX : transform.ScaleX;
            transform.ScaleY = ForceFinalValueRetained ? ScaleY : transform.ScaleY;
        }
    }
}
