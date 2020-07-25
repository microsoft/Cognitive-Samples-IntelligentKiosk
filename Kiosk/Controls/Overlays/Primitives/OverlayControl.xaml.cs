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
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    [TemplatePart(Name = MainGridPart, Type = typeof(Grid))]
    [ContentProperty(Name = nameof(Labels))]
    public class OverlayControl : Control
    {
        private const string MainGridPart = "PART_MainGrid";

        bool _hasAddedLabels;
        Grid _mainGrid;
        OverlayPresenter _presenter;

        public static readonly DependencyProperty OverlayInfoProperty = DependencyProperty.Register("OverlayInfo", typeof(IOverlayInfo), typeof(OverlayControl), new PropertyMetadata(default(IOverlayInfo), OnOverlayInfoChanged));
        public static readonly DependencyProperty OverlayTemplateProperty = DependencyProperty.Register("OverlayTemplate", typeof(DataTemplate), typeof(OverlayControl), new PropertyMetadata(default(DataTemplate)));
        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register("Labels", typeof(IList<OverlayLabel>), typeof(OverlayControl), new PropertyMetadata(new List<OverlayLabel>()));
        public static readonly DependencyProperty LabelVisibilityProperty = DependencyProperty.Register("LabelVisibility", typeof(Visibility), typeof(OverlayControl), new PropertyMetadata(default(Visibility), OnLabelVisibilityChanged));
        public static readonly DependencyProperty DefaultOverlaySizeProperty = DependencyProperty.Register("DefaultOverlaySize", typeof(object), typeof(OverlayControl), new PropertyMetadata(default(object), OnDefaultOverlaySizeChanged));
        public static readonly DependencyProperty EffectsProperty = DependencyProperty.Register("Effects", typeof(IList<OverlayEffectConfiguration>), typeof(OverlayControl), new PropertyMetadata(new List<OverlayEffectConfiguration>()));


        public IOverlayInfo OverlayInfo
        {
            get { return (IOverlayInfo)GetValue(OverlayInfoProperty); }
            set { SetValue(OverlayInfoProperty, value); }
        }

        public DataTemplate OverlayTemplate
        {
            get { return (DataTemplate)GetValue(OverlayTemplateProperty); }
            set { SetValue(OverlayTemplateProperty, value); }
        }

        public IList<OverlayLabel> Labels
        {
            get { return (IList<OverlayLabel>)GetValue(LabelsProperty); }
            set { SetValue(LabelsProperty, value); }
        }

        public Visibility LabelVisibility
        {
            get { return (Visibility)GetValue(LabelVisibilityProperty); }
            set { SetValue(LabelVisibilityProperty, value); }
        }

        public Size? DefaultOverlaySize
        {
            get { return (Size?)GetValue(DefaultOverlaySizeProperty); }
            set { SetValue(DefaultOverlaySizeProperty, value); }
        }

        public IList<OverlayEffectConfiguration> Effects
        {
            get { return (IList<OverlayEffectConfiguration>)GetValue(EffectsProperty); }
            set { SetValue(EffectsProperty, value); }
        }

        public OverlayControl()
        {
            DefaultStyleKey = typeof(OverlayControl);

            //bug fix for collection-type dependency properties
            SetValue(LabelsProperty, new List<OverlayLabel>());
            SetValue(EffectsProperty, new List<OverlayEffectConfiguration>());

            //set overlay from datacontext by default if not already set
            if (OverlayInfo == null && GetBindingExpression(OverlayControl.OverlayInfoProperty) == null)
            {
                DataContextChanged += OnDataContextChanged;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //get required parts
            _mainGrid = GetTemplateChild(MainGridPart) as Grid;
            if (_mainGrid == null)
            {
                throw new NullReferenceException($"{MainGridPart} is missing in the control template");
            }

            //inherit defaultOverlaySize
            var parent = OverlayItemsControl.GetParent<OverlayItemsControl>(this);
            if (parent != null && DefaultOverlaySize == null && GetBindingExpression(DefaultOverlaySizeProperty) == null)
            {
                SetBinding(DefaultOverlaySizeProperty, new Binding() { Path = new PropertyPath("DefaultOverlaySize"), Source = parent });
            }

            //hide the grid by default - if nothing databound to it
            _mainGrid.Visibility = Visibility.Collapsed;

            UpdateOverlays();
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            //set overlay from datacontext
            OverlayInfo = args.NewValue as IOverlayInfo;
        }

        private static void OnOverlayInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayControl;
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

            //update the overlay layout
            control.UpdateOverlays();
        }

        private static void OnDefaultOverlaySizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayControl;
            if (control == null)
            {
                return;
            }

            control.UpdateOverlays();
        }

        void OnVisualStateChanged(object sender, OverlayInfoState e)
        {
            //update effects
            if (_presenter == null)
            {
                _presenter = OverlayItemsControl.GetParent<OverlayPresenter>(this);
            }
            if (_presenter != null)
            {
                _presenter.OverlayVisualStateChanged(this, e, Effects);
            }
        }

        void UpdateOverlays()
        {
            //validate
            var overlayInfo = OverlayInfo;
            var defaultSize = DefaultOverlaySize;
            if (_mainGrid == null)
            {
                return;
            }
            if (overlayInfo == null || (overlayInfo.OverlaySize == null && defaultSize == null))
            {
                _mainGrid.Visibility = Visibility.Collapsed;
                return;
            }

            //show the grid
            _mainGrid.Visibility = Visibility.Visible;

            //set grid widths
            var size = overlayInfo.OverlaySize ?? defaultSize.GetValueOrDefault();
            var width = overlayInfo.Rect.Left;
            _mainGrid.ColumnDefinitions[0].Width = new GridLength(width, GridUnitType.Star);
            width = overlayInfo.Rect.Width / 2;
            _mainGrid.ColumnDefinitions[1].Width = new GridLength(width, GridUnitType.Star);
            _mainGrid.ColumnDefinitions[2].Width = new GridLength(width, GridUnitType.Star);
            width = size.Width - overlayInfo.Rect.Right;
            width = width < 0 ? 0 : width;
            _mainGrid.ColumnDefinitions[3].Width = new GridLength(width, GridUnitType.Star);
            //set grid heights
            var height = overlayInfo.Rect.Top;
            _mainGrid.RowDefinitions[0].Height = new GridLength(height, GridUnitType.Star);
            height = overlayInfo.Rect.Height / 2;
            _mainGrid.RowDefinitions[1].Height = new GridLength(height, GridUnitType.Star);
            _mainGrid.RowDefinitions[2].Height = new GridLength(height, GridUnitType.Star);
            height = size.Height - overlayInfo.Rect.Bottom;
            height = height < 0 ? 0 : height;
            _mainGrid.RowDefinitions[3].Height = new GridLength(height, GridUnitType.Star);

            //set data source of labels
            var labels = Labels;
            if (labels != null)
            {
                for (int i = 0; i < labels.Count; i++)
                {
                    var label = labels[i];
                    if (overlayInfo.Labels != null && overlayInfo.Labels.Count > i)
                    {
                        label.DataContext = overlayInfo.Labels[i];
                        label.OverlayInfo = overlayInfo;
                    }
                    else
                    {
                        label.DataContext = null;
                    }
                }
            }

            //place labels on grid - if havn't
            if (labels != null && !_hasAddedLabels)
            {
                _hasAddedLabels = true;
                foreach (var label in labels)
                {
                    _mainGrid.Children.Add(label);
                }
            }
        }

        private static void OnLabelVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayControl;
            if (control == null)
            {
                return;
            }

            //set the labels visibility
            var labels = control.Labels;
            var visibiltiy = control.LabelVisibility;
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    label.Visibility = visibiltiy;
                }
            }
        }

    }
}
