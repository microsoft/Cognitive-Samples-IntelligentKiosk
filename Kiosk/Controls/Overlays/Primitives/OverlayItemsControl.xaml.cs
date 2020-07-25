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
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    public class OverlayItemsControl : ItemsControl
    {
        public static readonly DependencyProperty DefaultOverlaySizeProperty = DependencyProperty.Register("DefaultOverlaySize", typeof(object), typeof(OverlayItemsControl), new PropertyMetadata(default(object)));

        public OverlayItemsControl()
        {
            DefaultStyleKey = typeof(OverlayItemsControl);

            //Bug fix: the designer isn't applying the style template correctly.  this fixes an exception it causes on mouseover in the designer.
            ItemsPanel = CreateItemsPanel(typeof(Grid));
        }

        ItemsPanelTemplate CreateItemsPanel(Type panelType)
        {
            var xaml =
                $@"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                   <{panelType.Name} />
               </ItemsPanelTemplate>";

            return (ItemsPanelTemplate)XamlReader.Load(xaml);
        }

        public Size? DefaultOverlaySize
        {
            get { return (Size?)GetValue(DefaultOverlaySizeProperty); }
            set { SetValue(DefaultOverlaySizeProperty, value); }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //inherit defaultOverlaySize
            var parent = GetParent<OverlayItemsControl>(this);
            if (parent != null && DefaultOverlaySize == null && GetBindingExpression(DefaultOverlaySizeProperty) == null)
            {
                SetBinding(DefaultOverlaySizeProperty, new Binding() { Path = new PropertyPath("DefaultOverlaySize"), Source = parent });
            }
        }

        public static T GetParent<T>(DependencyObject d, int maxDepth = 10, bool firstTime = true)
            where T : DependencyObject
        {
            //return result
            var result = d as T;
            if (result != null && !firstTime)
            {
                return result;
            }
            if (maxDepth == 0 || d == null)
            {
                return default(T);
            }

            //recursivly find the parent
            var parent = VisualTreeHelper.GetParent(d);
            return GetParent<T>(parent, maxDepth--, false);
        }
    }
}
