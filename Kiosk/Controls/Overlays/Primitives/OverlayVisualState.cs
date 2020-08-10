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

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    public class OverlayVisualState : ContentControl
    {
        public static readonly DependencyProperty OverlayInfoProperty = DependencyProperty.Register("OverlayInfo", typeof(IOverlayInfo), typeof(OverlayVisualState), new PropertyMetadata(default(IOverlayInfo), OnOverlayInfoChanged));

        public OverlayVisualState()
        {
            DefaultStyleKey = typeof(OverlayVisualState);

            //set overlay from datacontext by default if not already set
            if (OverlayInfo == null && GetBindingExpression(OverlayControl.OverlayInfoProperty) == null)
            {
                DataContextChanged += OnDataContextChanged;
            }
        }

        public IOverlayInfo OverlayInfo
        {
            get { return (IOverlayInfo)GetValue(OverlayInfoProperty); }
            set { SetValue(OverlayInfoProperty, value); }
        }

        private static void OnOverlayInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
           var control = d as OverlayVisualState;
            if (control == null)
            {
                return;
            }

            //connect to events
            var overlayInfo = e.OldValue as IOverlayInfo;
            if (overlayInfo != null)
            {
                overlayInfo.VisualStateChanged -= control.OnVisualStateChanged;
            }
            overlayInfo = e.NewValue as IOverlayInfo;
            if (overlayInfo != null)
            {
                overlayInfo.VisualStateChanged += control.OnVisualStateChanged;
            }

            //set initial state
            if (overlayInfo != null)
            {
                control.OnVisualStateChanged(overlayInfo, overlayInfo.VisualState);
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //inherit defaultOverlaySize
            var parent = OverlayItemsControl.GetParent<OverlayLabel>(this);
            if (parent != null && OverlayInfo == null && GetBindingExpression(OverlayInfoProperty) == null)
            {
                SetBinding(OverlayInfoProperty, new Binding() { Path = new PropertyPath("OverlayInfo"), Source = parent });
            }

            //set initial state
            var overlayInfo = OverlayInfo;
            if (overlayInfo != null)
            {
                OnVisualStateChanged(overlayInfo, overlayInfo.VisualState);
            }
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
        {
            //set overlay from datacontext
            var overlayInfo = e.NewValue as IOverlayInfo;
            if (overlayInfo != null)
            {
                OverlayInfo = overlayInfo;
            }
        }

        void OnVisualStateChanged(object sender, OverlayInfoState e)
        {
            //set the visual state
            VisualStateManager.GoToState(this, e.ToString(), true);
        }
    }
}
