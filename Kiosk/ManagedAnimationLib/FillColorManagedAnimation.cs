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
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace SocialEbola.Lib.Animation
{
    public class FillColorManagedAnimation : ManagedAnimation
    {
        public Color Color { get; set; }
        public TimeSpan Duration { get; set; }
        public EasingFunctionBase EasingFunction { get; set; }

        public FillColorManagedAnimation(Color color, TimeSpan duration, EasingFunctionBase easingFunction = null)
        {
            Color = color;
            Duration = duration;
            EasingFunction = easingFunction;
        }

        protected override Storyboard CreateStoryboard()
        {
            Storyboard board = new Storyboard();
            var timeline = new ColorAnimationUsingKeyFrames();
            var shape = (Shape)Element;
            var brush = (SolidColorBrush)shape.Fill;
            Storyboard.SetTarget(timeline, brush);
            Storyboard.SetTargetProperty(timeline, "Color");
            var frame = new EasingColorKeyFrame() { KeyTime = Duration, Value = Color, EasingFunction = EasingFunction };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            return board;
        }

        public override void Retain()
        {
            var shape = (Shape)Element;
            var brush = (SolidColorBrush)shape.Fill;
            brush.Color = brush.Color;
        }
    }
}
