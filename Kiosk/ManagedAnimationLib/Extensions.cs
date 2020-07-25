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
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace SocialEbola.Lib.Animation
{
    public static class Extensions
    {
        public static int IndexOfItem<T>(this IEnumerable<T> that, Func<T, bool> pred)
        {
            int result = -1;
            int counter = 0;
            foreach (var item in that)
            {
                if (pred(item))
                {
                    result = counter;
                    break;
                }

                counter++;
            }

            return result;
        }

        public static Point GetTopLeftForCenter(this FrameworkElement element, Point pt)
        {
            return GetTopLeftForCenter(element, pt.X, pt.Y);
        }

        public static Point GetTopLeftForCenter(this FrameworkElement element, double x, double y)
        {
            return new Point(x - element.ActualWidth / 2, y - element.ActualHeight / 2);
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

        public static Task<A> EventToTaskAsync<S, A>(Action<Action<S, A>> setup, Action adder, Action remover)
        {
            TaskCompletionSource<A> tcs = new TaskCompletionSource<A>();
            Action<S, A> onComplete = null;
            onComplete = (s, e) =>
            {
                remover();
                tcs.SetResult(e);
            };
            setup(onComplete);
            adder();
            return tcs.Task;
        }

        public static T EnsureRenderTransform<T>(this FrameworkElement element, Action<T> init = null) where T : Transform, new()
        {
            var t = element.RenderTransform as T;
            if (t == null)
            {
                element.RenderTransform = t = new T();
                if (init != null)
                {
                    init(t);
                }
            }

            return t;
        }

        public static T EnsureProjection<T>(this FrameworkElement element, Action<T> init = null) where T : Projection, new()
        {
            var t = element.Projection as T;
            if (t == null)
            {
                element.Projection = t = new T();
                if (init != null)
                {
                    init(t);
                }
            }

            return t;
        }

        public static Point GetCanvasLocation(this FrameworkElement element)
        {
            return new Point(Canvas.GetLeft(element), Canvas.GetTop(element));
        }

        public static void SetCanvasLocation(this FrameworkElement element, Point pt)
        {
            Canvas.SetLeft(element, pt.X);
            Canvas.SetTop(element, pt.Y);
        }

        public static void SetCanvasBounds(this FrameworkElement element, Rect rect)
        {
            element.SetCanvasLocation(rect.GetTopLeft());
            element.SetSize(rect.GetSize());
        }

        public static Size GetSize(this Rect rect)
        {
            return new Size(rect.Width, rect.Height);
        }

        public static Point GetTopLeft(this Rect rect)
        {
            return new Point(rect.Left, rect.Top);
        }

        public static Point GetOffset(this Point point, Point size)
        {
            return new Point(point.X + size.X, point.Y + size.Y);
        }

        public static Size GetSize(this FrameworkElement element)
        {
            return new Size(element.ActualWidth, element.ActualHeight);
        }

        public static void SetSize(this FrameworkElement element, Size size)
        {
            SetSize(element, size.Width, size.Height);
        }

        public static void SetSize(this FrameworkElement element, double width, double height)
        {
            element.Width = width;
            element.Height = height;
        }
    }
}
