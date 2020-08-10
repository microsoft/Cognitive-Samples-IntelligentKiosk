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
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace IntelligentKioskSample.Extensions
{
    public static class AnimationExtensions
    {
        public static int IndexOfItem<T>(this IEnumerable<T> that, Func<T, bool> pred)
        {
            int index = -1;
            int count = 0;
            foreach (var item in that)
            {
                if (pred(item))
                {
                    index = count;
                    break;
                }

                count++;
            }

            return index;
        }

        public static Task BeginAsync(this Storyboard storyboard)
        {
            return EventToTaskAsync<object>(
                    e => { storyboard.Completed += e; storyboard.Begin(); },
                    e => storyboard.Completed -= e);
        }

        public static Task<A> EventToTaskAsync<A>(Action<EventHandler<A>> adder, Action<EventHandler<A>> remover)
        {
            TaskCompletionSource<A> tcs = new TaskCompletionSource<A>();
            EventHandler<A> onComplete = null;
            onComplete = (s, e) =>
            {
                remover(onComplete);
                tcs.SetResult(e);
            };
            adder(onComplete);
            return tcs.Task;
        }

        public static T EnsureRenderTransform<T>(this FrameworkElement element, Action<T> init = null) where T : Transform, new()
        {
            var t = element.RenderTransform as T;
            if (t == null)
            {
                element.RenderTransform = t = new T();
                init?.Invoke(t);
            }

            return t;
        }
    }
}
