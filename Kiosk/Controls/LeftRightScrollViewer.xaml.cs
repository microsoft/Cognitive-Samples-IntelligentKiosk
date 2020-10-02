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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace IntelligentKioskSample.Controls
{
    [TemplatePart(Name = ScrollViewerPart, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = LeftScrollPart, Type = typeof(Grid))]
    [TemplatePart(Name = RightScrollPart, Type = typeof(Grid))]
    public class LeftRightScrollViewer : ContentControl
    {
        private const string ScrollViewerPart = "PART_ScrollViewer";
        private const string LeftScrollPart = "PART_LeftScroll";
        private const string RightScrollPart = "PART_RightScroll";
        ScrollViewer _scrollViewer;
        Button _leftScroll;
        Button _rightScroll;
        LeftRightScrollViewerVisualState _visualState;
        bool _scrollable;

        public static readonly DependencyProperty ScrollAmountProperty = DependencyProperty.Register("ScrollAmount", typeof(double), typeof(LeftRightScrollViewer), new PropertyMetadata(1d));
        public static readonly DependencyProperty ScrollModeProperty = DependencyProperty.Register("ScrollMode", typeof(ScrollMode), typeof(LeftRightScrollViewer), new PropertyMetadata(ScrollMode.Auto));
        public static readonly DependencyProperty ScrollBarVisibilityProperty = DependencyProperty.Register("ScrollBarVisibility", typeof(ScrollBarVisibility), typeof(LeftRightScrollViewer), new PropertyMetadata(ScrollBarVisibility.Hidden));

        public double ScrollAmount
        {
            get { return (double)GetValue(ScrollAmountProperty); }
            set { SetValue(ScrollAmountProperty, value); }
        }

        public ScrollMode ScrollMode
        {
            get { return (ScrollMode)GetValue(ScrollModeProperty); }
            set { SetValue(ScrollModeProperty, value); }
        }

        public ScrollBarVisibility ScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(ScrollBarVisibilityProperty); }
            set { SetValue(ScrollBarVisibilityProperty, value); }
        }

        public LeftRightScrollViewer()
        {
            DefaultStyleKey = typeof(LeftRightScrollViewer);
        }

        /// <summary>
        /// will reset the scrolling position to the start
        /// </summary>
        public void ResetScroll()
        {
            //reset scrolling
            _scrollViewer?.ChangeView(0, 0, null, true);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //get required parts
            _scrollViewer = GetTemplateChild(ScrollViewerPart) as ScrollViewer;
            if (_scrollViewer == null)
            {
                throw new NullReferenceException($"{ScrollViewerPart} is missing in the control template");
            }
            _leftScroll = GetTemplateChild(LeftScrollPart) as Button;
            if (_leftScroll == null)
            {
                throw new NullReferenceException($"{LeftScrollPart} is missing in the control template");
            }
            _rightScroll = GetTemplateChild(RightScrollPart) as Button;
            if (_rightScroll == null)
            {
                throw new NullReferenceException($"{RightScrollPart} is missing in the control template");
            }

            //get changes to ExtendWidth property
            _scrollViewer.RegisterPropertyChangedCallback(ScrollViewer.ExtentWidthProperty, OnScrollExtentChanged);
            //connect to events
            _scrollViewer.SizeChanged += OnSizeChanged;
            _scrollViewer.ViewChanged += OnScrollViewChanged;
            _leftScroll.Click += OnLeftScrollButtonClicked;
            _rightScroll.Click += OnRightScrollButtonClicked;
            //bind properties
            _scrollViewer.SetBinding(ScrollViewer.HorizontalScrollModeProperty, new Binding() { Source = this, Path = new PropertyPath(nameof(ScrollMode)) });
            _scrollViewer.SetBinding(ScrollViewer.HorizontalScrollBarVisibilityProperty, new Binding() { Source = this, Path = new PropertyPath(nameof(ScrollBarVisibility)) });
        }

        private void OnLeftScrollButtonClicked(object sender, RoutedEventArgs e)
        {
            Scroll(-ScrollAmount);
        }

        private void OnRightScrollButtonClicked(object sender, RoutedEventArgs e)
        {
            Scroll(ScrollAmount);
        }

        void Scroll(double amount)
        {
            var offset = _scrollViewer.HorizontalOffset + (_scrollViewer.RenderSize.Width * amount);
            _scrollViewer.ChangeView(offset, null, 1.0f, false);
        }

        private void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateScroll();
        }

        void UpdateScroll()
        {
            //set visual state for start end end of scrolling
            if (_scrollable)
            {
                //at start
                if (_scrollViewer.HorizontalOffset < 1)
                {
                    OnVisualStateChanged(LeftRightScrollViewerVisualState.ScrollStart);
                }
                //at end
                else if (_scrollViewer.HorizontalOffset + _scrollViewer.RenderSize.Width == _scrollViewer.ExtentWidth)
                {
                    OnVisualStateChanged(LeftRightScrollViewerVisualState.ScrollEnd);
                }
                //in the middle
                else
                {
                    OnVisualStateChanged(LeftRightScrollViewerVisualState.ScrollMiddle);
                }
            }
        }

        public void OnScrollExtentChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateScrollable();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollable();
        }

        void UpdateScrollable()
        {
            //determine if its able to scroll
            _scrollable = _scrollViewer.ExtentWidth > _scrollViewer.RenderSize.Width;

            //update visual state
            if (!_scrollable && _visualState != LeftRightScrollViewerVisualState.ScrollNone)
            {
                OnVisualStateChanged(LeftRightScrollViewerVisualState.ScrollNone);
            }

            UpdateScroll();
        }

        void OnVisualStateChanged(LeftRightScrollViewerVisualState e)
        {
            //set the visual state
            if (e != _visualState)
            {
                _visualState = e;
                VisualStateManager.GoToState(this, e.ToString(), true);
            }
        }
    }

    public enum LeftRightScrollViewerVisualState
    {
        ScrollNone,
        ScrollStart,
        ScrollMiddle,
        ScrollEnd
    }
}