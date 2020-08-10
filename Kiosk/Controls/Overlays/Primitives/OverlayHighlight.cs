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
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    public class OverlayHighlight : Grid
    {
        IOverlayInfo _overlayInfo;
        Brush _origColor;
        bool _origColorSet;
        UIElement _highlightTemplate;
        bool _setHighlightable;

        public Brush HighlightColor { get; set; }
        public static readonly DependencyProperty OverlayInfoProperty = DependencyProperty.RegisterAttached("OverlayInfo", typeof(IOverlayInfo), typeof(OverlayHighlight), new PropertyMetadata(true, OnOverlayInfoChanged));
        public static readonly DependencyProperty HighlightEnabledProperty = DependencyProperty.RegisterAttached("HighlightEnabled", typeof(bool), typeof(OverlayHighlight), new PropertyMetadata(true, OnHighlightEnabledChanged));
        public static readonly DependencyProperty HighlightTemplateProperty = DependencyProperty.RegisterAttached("HighlightTemplate", typeof(DataTemplate), typeof(OverlayHighlight), new PropertyMetadata(null));

        public bool HighlightEnabled
        {
            get { return (bool)GetValue(HighlightEnabledProperty); }
            set { SetValue(HighlightEnabledProperty, value); }
        }

        public IOverlayInfo OverlayInfo
        {
            get { return (IOverlayInfo)GetValue(OverlayInfoProperty); }
            set { SetValue(OverlayInfoProperty, value); }
        }

        public DataTemplate HighlightTemplate
        {
            get { return (DataTemplate)GetValue(HighlightTemplateProperty); }
            set { SetValue(HighlightTemplateProperty, value); }
        }

        public OverlayHighlight()
        {
            //connect to events
            DataContextChanged += OnDataContextChanged;
        }

        static void OnOverlayInfoChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as OverlayHighlight;

            //directly set highlightable - override DataContext
            control._setHighlightable = true;
            control.UpdateOverlayInfo(e.NewValue);
        }

        static void OnHighlightEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as OverlayHighlight;

            //unhighlight the item
            if (!control.HighlightEnabled && control._origColorSet)
            {
                control.Background = control._origColor;
            }
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (!_setHighlightable)
            {
                UpdateOverlayInfo(args.NewValue);
            }
        }

        void UpdateOverlayInfo(object value)
        {
            if (_overlayInfo != null)
            {
                _overlayInfo.VisualStateChanged -= OnVisualStateChanged;
                _overlayInfo = null;
            }
            _overlayInfo = value as IOverlayInfo;
            if (_overlayInfo != null)
            {
                _overlayInfo.VisualStateChanged += OnVisualStateChanged;

                PointerEntered += (i, arg) =>
                {
                    if (HighlightEnabled)
                    {
                        _overlayInfo.IsSelected = true;
                    }
                };
                PointerExited += (i, arg) =>
                {
                    if (HighlightEnabled)
                    {
                        _overlayInfo.IsSelected = false;
                    }
                };
            }
        }

        void OnVisualStateChanged(object sender, OverlayInfoState e)
        {
            if (_overlayInfo != null && HighlightEnabled)
            {
                VisualizeHighlight(_overlayInfo.IsSelected);
            }
        }

        void VisualizeHighlight(bool highlightOn)
        {
            //turn on or off data template highlight
            if (HighlightTemplate != null)
            {
                if (highlightOn)
                {
                    //on
                    _highlightTemplate = HighlightTemplate.LoadContent() as UIElement;
                    Children.Add(_highlightTemplate);
                }
                else
                {
                    //off
                    Children.Remove(_highlightTemplate);
                    _highlightTemplate = null;
                }
            }

            //turn on or off the background highlight - default
            else
            {
                if (highlightOn)
                {
                    //on
                    if (!_origColorSet)
                    {
                        _origColor = Background;
                        _origColorSet = true;
                    }
                    Background = HighlightColor;
                }
                else
                {
                    //off
                    Background = _origColor;
                }
            }
        }
    }
}
