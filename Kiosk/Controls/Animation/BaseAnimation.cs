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
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace IntelligentKioskSample.Controls.Animation
{
    public class BaseAnimationCompleteEventArgs
    {
        public bool NaturallyCompleted { get; set; }
    }

    public abstract class BaseAnimation
    {
        public static readonly EasingFunctionBase DefaultOut = new QuadraticEase() { EasingMode = EasingMode.EaseOut };
        public static readonly EasingFunctionBase DefaultIn = new QuadraticEase() { EasingMode = EasingMode.EaseIn };
        public static readonly EasingFunctionBase DefaultInOut = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };

        private Storyboard storyboard;
        private BaseAnimationCompleteEventArgs completeResult = new BaseAnimationCompleteEventArgs() { NaturallyCompleted = true };

        public bool ForceFinalValueRetained { get; set; }
        public FrameworkElement Element { get; protected set; }

        public Task<BaseAnimationCompleteEventArgs> Activate(FrameworkElement element)
        {
            return Activate(element, false);
        }

        public async Task<BaseAnimationCompleteEventArgs> Activate(FrameworkElement element, bool forceFinalValueRetained)
        {
            if (Element != null)
            {
                throw new InvalidOperationException();
            }
            ForceFinalValueRetained = forceFinalValueRetained;
            Element = element;
            storyboard = CreateStoryboard();
            AnimationHelper.AddAnimation(this);
            await storyboard.BeginAsync();

            storyboard = null;
            AnimationHelper.RemoveAnimation(this);
            Element = null;

            var result = completeResult;
            return result;
        }

        public void Stop()
        {
            if (storyboard != null)
            {
                storyboard.Stop();
                completeResult.NaturallyCompleted = false;
            }
        }

        public virtual bool Equivalent(BaseAnimation animation)
        {
            return animation.GetType() == GetType();
        }

        protected abstract Storyboard CreateStoryboard();

        public abstract void Retain();
    }
}
