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
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace IntelligentKioskSample.Controls.Animation
{
    public class AnimationHelper
    {
        public static readonly DependencyProperty BaseAnimationsProperty = DependencyProperty.RegisterAttached("BaseAnimations", typeof(List<BaseAnimation>), typeof(AnimationHelper), new PropertyMetadata(null));

        public static List<BaseAnimation> GetAnimations(DependencyObject obj)
        {
            return (List<BaseAnimation>)obj.GetValue(BaseAnimationsProperty);
        }

        public static void SetAnimations(DependencyObject obj, List<BaseAnimation> value)
        {
            obj.SetValue(BaseAnimationsProperty, value);
        }

        internal static void AddAnimation(BaseAnimation animation)
        {
            var list = VerifyList(animation);
            int index = list.IndexOfItem(x => x.Equivalent(animation));
            if (index != -1)
            {
                var item = list[index];
                item.Retain();
                item.Stop();
                list.RemoveAt(index);
            }

            list.Add(animation);
        }

        internal static void RemoveAnimation(BaseAnimation animation)
        {
            var list = GetAnimations(animation.Element);
            if (list != null)
            {
                animation.Retain();
                animation.Stop();
                list.Remove(animation);
            }
        }

        private static List<BaseAnimation> VerifyList(BaseAnimation animation)
        {
            var list = GetAnimations(animation.Element);
            if (list == null)
            {
                SetAnimations(animation.Element, list = new List<BaseAnimation>());
            }
            return list;
        }
    }
}
