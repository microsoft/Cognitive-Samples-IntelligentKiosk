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

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls
{
    public class ClipHelper
    {
        public static readonly DependencyProperty ToBoundsProperty = DependencyProperty.RegisterAttached("ToBounds", typeof(bool), typeof(ClipHelper), new PropertyMetadata(false, OnToBoundsPropertyChanged));

        public static bool GetToBounds(DependencyObject depObj)
        {
            return (bool)depObj.GetValue(ToBoundsProperty);
        }

        public static void SetToBounds(DependencyObject depObj, bool clipToBounds)
        {
            depObj.SetValue(ToBoundsProperty, clipToBounds);
        }

        private static void OnToBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement fe = d as FrameworkElement;
            if (fe != null)
            {
                if (GetToBounds(fe))
                {
                    ClipToBounds(fe);

                    // whenever the element which this property is attached to is loaded
                    // or re-sizes, we need to update its clipping geometry
                    fe.Loaded += new RoutedEventHandler(Loaded);
                    fe.SizeChanged += new SizeChangedEventHandler(SizeChanged);
                }
                else
                {
                    fe.Loaded -= new RoutedEventHandler(Loaded);
                    fe.SizeChanged -= new SizeChangedEventHandler(SizeChanged);
                    fe.Clip = null;
                }
            }
        }

        /// <summary>
        /// Creates a rectangular clipping geometry which matches the geometry of the
        /// passed element
        /// </summary>
        public static void ClipToBounds(FrameworkElement fe)
        {
            if (GetToBounds(fe))
            {
                fe.Clip = new RectangleGeometry()
                {
                    Rect = new Rect(0, 0, fe.ActualWidth, fe.ActualHeight)
                };
            }
            else
            {
                fe.Clip = null;
            }
        }

        static void SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipToBounds(sender as FrameworkElement);
        }

        static void Loaded(object sender, RoutedEventArgs e)
        {
            ClipToBounds(sender as FrameworkElement);
        }
    }
}
