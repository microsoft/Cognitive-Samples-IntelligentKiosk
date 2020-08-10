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

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    [TemplatePart(Name = MainPresenterPart, Type = typeof(ContentPresenter))]
    public class OverlayLabel : Control
    {
        private const string MainPresenterPart = "PART_MainPresenter";

        ContentPresenter _mainPresenter;

        public static readonly DependencyProperty LabelTemplateProperty = DependencyProperty.Register("LabelTemplate", typeof(DataTemplate), typeof(OverlayLabel), new PropertyMetadata(default(DataTemplate)));
        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register("Offset", typeof(double), typeof(OverlayLabel), new PropertyMetadata(default(double), OnOffsetChanged));
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(OverlayLabelPosition), typeof(OverlayLabel), new PropertyMetadata(default(OverlayLabelPosition), OnPositionChanged));
        public static readonly DependencyProperty OverlayInfoProperty = DependencyProperty.Register("OverlayInfo", typeof(IOverlayInfo), typeof(OverlayLabel), new PropertyMetadata(default(IOverlayInfo)));
        public static readonly DependencyProperty AutoHideProperty = DependencyProperty.Register("AutoHide", typeof(bool), typeof(OverlayLabel), new PropertyMetadata(default(bool), OnAutoHideChanged));

        public OverlayLabel()
        {
            DefaultStyleKey = typeof(OverlayLabel);
            DataContextChanged += OnDataContextChanged;
        }

        public DataTemplate LabelTemplate
        {
            get { return (DataTemplate)GetValue(LabelTemplateProperty); }
            set { SetValue(LabelTemplateProperty, value); }
        }

        public double Offset
        {
            get { return (double)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public OverlayLabelPosition Position
        {
            get { return (OverlayLabelPosition)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public IOverlayInfo OverlayInfo
        {
            get { return (IOverlayInfo)GetValue(OverlayInfoProperty); }
            set { SetValue(OverlayInfoProperty, value); }
        }

        public bool AutoHide
        {
            get { return (bool)GetValue(AutoHideProperty); }
            set { SetValue(AutoHideProperty, value); }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //get required parts
            _mainPresenter = GetTemplateChild(MainPresenterPart) as ContentPresenter;
            if (_mainPresenter == null)
            {
                throw new NullReferenceException($"{MainPresenterPart} is missing in the control template");
            }

            //connect to events
            _mainPresenter.SizeChanged += OnSizeChanged;

            SetOffset();
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            SetPosition();
            SetOffset();
            UpdateAutoHide();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetOffset();
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayLabel;
            if (control == null)
            {
                return;
            }

            control.SetPosition();
            control.SetOffset();
        }

        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayLabel;
            if (control == null)
            {
                return;
            }

            control.SetOffset();
        }

        private static void OnAutoHideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayLabel;
            if (control == null)
            {
                return;
            }

            control.UpdateAutoHide();
        }

        void UpdateAutoHide()
        {
            Visibility = !(AutoHide && DataContext == null) ? Visibility.Visible : Visibility.Collapsed;
        }

        void SetPosition()
        {
            var position = Position;
            var column = 3;
            var row = 1;
            switch (position)
            {
                case OverlayLabelPosition.TopLeft:
                    column = 1;
                    row = 1;
                    break;
                case OverlayLabelPosition.TopCenter:
                    column = 2;
                    row = 1;
                    break;
                case OverlayLabelPosition.TopRight:
                    column = 3;
                    row = 1;
                    break;
                case OverlayLabelPosition.BottomLeft:
                    column = 1;
                    row = 3;
                    break;
                case OverlayLabelPosition.BottomCenter:
                    column = 2;
                    row = 3;
                    break;
                case OverlayLabelPosition.BottomRight:
                    column = 3;
                    row = 3;
                    break;
                case OverlayLabelPosition.LeftTop:
                    column = 1;
                    row = 1;
                    break;
                case OverlayLabelPosition.LeftCenter:
                    column = 1;
                    row = 2;
                    break;
                case OverlayLabelPosition.LeftBottom:
                    column = 1;
                    row = 3;
                    break;
                case OverlayLabelPosition.RightTop:
                    column = 3;
                    row = 1;
                    break;
                case OverlayLabelPosition.RightCenter:
                    column = 3;
                    row = 2;
                    break;
                case OverlayLabelPosition.RightBottom:
                    column = 3;
                    row = 3;
                    break;
                case OverlayLabelPosition.Center:
                    column = 2;
                    row = 2;
                    break;
            }
            Grid.SetColumn(this, column);
            Grid.SetRow(this, row);
        }

        void SetOffset()
        {
            //validate
            if (_mainPresenter == null)
            {
                return;
            }

            var position = Position;
            var offset = Offset;
            var size = _mainPresenter.RenderSize;
            var x = 0d;
            var y = 0d;
            switch (position)
            {
                case OverlayLabelPosition.TopLeft:
                    y = -size.Height - offset;
                    x = 0;
                    break;
                case OverlayLabelPosition.TopCenter:
                    y = -size.Height - offset;
                    x = -size.Width / 2;
                    break;
                case OverlayLabelPosition.TopRight:
                    y = -size.Height - offset;
                    x = -size.Width;
                    break;
                case OverlayLabelPosition.BottomLeft:
                    y = offset;
                    x = 0;
                    break;
                case OverlayLabelPosition.BottomCenter:
                    y = offset;
                    x = -size.Width / 2;
                    break;
                case OverlayLabelPosition.BottomRight:
                    y = offset;
                    x = -size.Width;
                    break;
                case OverlayLabelPosition.LeftTop:
                    y = 0;
                    x = -size.Width - offset;
                    break;
                case OverlayLabelPosition.LeftCenter:
                    y = -size.Height / 2;
                    x = -size.Width - offset;
                    break;
                case OverlayLabelPosition.LeftBottom:
                    y = -size.Height;
                    x = -size.Width - offset;
                    break;
                case OverlayLabelPosition.RightTop:
                    y = 0;
                    x = offset;
                    break;
                case OverlayLabelPosition.RightCenter:
                    y = -size.Height / 2;
                    x = offset;
                    break;
                case OverlayLabelPosition.RightBottom:
                    y = -size.Height;
                    x = offset;
                    break;
                case OverlayLabelPosition.Center:
                    y = -size.Height / 2 - offset;
                    x = -size.Width / 2;
                    break;
            }
            Canvas.SetLeft(_mainPresenter, x);
            Canvas.SetTop(_mainPresenter, y);
        }
    }

    public enum OverlayLabelPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        LeftTop,
        LeftCenter,
        LeftBottom,
        RightTop,
        RightCenter,
        RightBottom,
        Center
    }
}
