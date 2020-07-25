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
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace SocialEbola.Lib.Animation
{
    public abstract class ManagedAnimation
    {
        public static readonly EasingFunctionBase DefaultOut = new QuadraticEase() { EasingMode = EasingMode.EaseOut };
        public static readonly EasingFunctionBase DefaultIn = new QuadraticEase() { EasingMode = EasingMode.EaseIn };
        public static readonly EasingFunctionBase DefaultInOut = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
        public static readonly EasingFunctionBase Elastic1Out = new ElasticEase() { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 3 };
        public static readonly EasingFunctionBase Elastic1In = new ElasticEase() { EasingMode = EasingMode.EaseIn, Oscillations = 1, Springiness = 3 };
        public static readonly EasingFunctionBase Elastic3Out = new ElasticEase() { EasingMode = EasingMode.EaseOut, Oscillations = 3, Springiness = 3 };
        public static readonly EasingFunctionBase Elastic3In = new ElasticEase() { EasingMode = EasingMode.EaseIn, Oscillations = 3, Springiness = 3 };
        public static readonly EasingFunctionBase Bounce1Out = new BounceEase() { EasingMode = EasingMode.EaseOut, Bounces = 1, Bounciness = 3 };
        public static readonly EasingFunctionBase Bounce1In = new BounceEase() { EasingMode = EasingMode.EaseIn, Bounces = 1, Bounciness = 3 };

        private Storyboard m_storyboard;
        private ManagedAnimationCompleteEventArgs m_completeResult = new ManagedAnimationCompleteEventArgs() { NaturallyCompleted = true };

        public bool ForceFinalValueRetained { get; set; }
        public FrameworkElement Element { get; protected set; }

        public Task<ManagedAnimationCompleteEventArgs> Activate(FrameworkElement element)
        {
            return Activate(element, false);
        }

        public async Task<ManagedAnimationCompleteEventArgs> Activate(FrameworkElement element, bool forceFinalValueRetained)
        {
            if (Element != null)
            {
                throw new InvalidOperationException();
            }
            ForceFinalValueRetained = forceFinalValueRetained;
            Element = element;
            m_storyboard = CreateStoryboard();
            Orchestration.AddManagedAnimation(this);
            await m_storyboard.BeginAsync();

            m_storyboard = null;
            Orchestration.RemoveManagedAnimation(this);
            Element = null;

            var result = m_completeResult;

            return result;
        }

        public void Stop()
        {
            if (m_storyboard != null)
            {
                m_storyboard.Stop();
                m_completeResult.NaturallyCompleted = false;
            }
        }

        protected abstract Storyboard CreateStoryboard();

        public virtual bool Equivalent(ManagedAnimation managedAnimation)
        {
            return managedAnimation.GetType() == GetType();
        }

        public abstract void Retain();
    }

    public abstract class ManagedAnimation<E> : ManagedAnimation where E : FrameworkElement
    {
        public new E Element
        {
            get { return (E)base.Element; }
            protected set { base.Element = value; }
        }

        public Task<ManagedAnimationCompleteEventArgs> Activate(E element)
        {
            return base.Activate(element, false);
        }

        public Task<ManagedAnimationCompleteEventArgs> Activate(E element, bool forceFinalValueRetained)
        {
            return base.Activate(element, forceFinalValueRetained);
        }
    }
}
