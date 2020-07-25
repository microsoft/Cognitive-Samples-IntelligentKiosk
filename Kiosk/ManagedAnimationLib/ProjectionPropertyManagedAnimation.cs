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
using System.Reflection;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace SocialEbola.Lib.Animation
{
    public class ProjectionPropertyManagedAnimation : ManagedAnimation
    {
        public double PropertyValue { get; set; }
        public EasingFunctionBase Easing { get; set; }
        public TimeSpan Duration { get; set; }
        public string ProjectionPropertyName { get; set; }

        public ProjectionPropertyManagedAnimation(string name, double propertyValue, TimeSpan duration, EasingFunctionBase easing = null)
        {
            PropertyValue = propertyValue;
            Easing = easing ?? DefaultOut;
            Duration = duration;
            ProjectionPropertyName = name;
        }

        public override bool Equivalent(ManagedAnimation managedAnimation)
        {
            var other = managedAnimation as ProjectionPropertyManagedAnimation;
            bool result = false;
            if (other != null)
            {
                if (other.ProjectionPropertyName == ProjectionPropertyName)
                {
                    result = true;
                }
            }
            return result;
        }

        protected override Storyboard CreateStoryboard()
        {
            var projection = Projection;

            Storyboard board = new Storyboard();
            var timeline = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(timeline, projection);
            Storyboard.SetTargetProperty(timeline, ProjectionPropertyName);
            var frame = new EasingDoubleKeyFrame() { KeyTime = Duration, Value = PropertyValue, EasingFunction = Easing };
            timeline.KeyFrames.Add(frame);
            board.Children.Add(timeline);

            return board;
        }

        private PlaneProjection Projection
        {
            get
            {
                var projection = Element.Projection as PlaneProjection;
                if (projection == null)
                {
                    Element.Projection = projection = new PlaneProjection();
                }
                return projection;
            }
        }

        public override void Retain()
        {
            var prop = Projection.GetType().GetRuntimeProperty(ProjectionPropertyName);
            prop.SetValue(Projection, ForceFinalValueRetained ? PropertyValue : prop.GetValue(Projection));
        }
    }
}
