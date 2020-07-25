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
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    [TemplatePart(Name = ContentGridPart, Type = typeof(Grid))]
    [TemplatePart(Name = MainGridPart, Type = typeof(Grid))]
    public class OverlayPresenter : OverlayItemsControl
    {
        private const string MainGridPart = "PART_MainGrid";
        private const string ContentGridPart = "Part_ContentGrid";

        Grid _mainGrid;
        Grid _contentGrid;
        OverlayEffectManager _effectManager;
        bool _hasPresenterSelected;
        List<IOverlayInfo> _presenterSelected = new List<IOverlayInfo>();

        public static readonly DependencyProperty OverlayVisibilityProperty = DependencyProperty.Register("OverlayVisibility", typeof(Visibility), typeof(OverlayPresenter), new PropertyMetadata(default(Visibility)));
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(UIElement), typeof(OverlayPresenter), new PropertyMetadata(default(UIElement), OnContentChanged));
        public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register("ContentTemplate", typeof(DataTemplate), typeof(OverlayPresenter), new PropertyMetadata(default(DataTemplate)));
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ImageSource), typeof(OverlayPresenter), new PropertyMetadata(default(ImageSource), OnImageSourceChanged));
        public static readonly DependencyProperty ClipLabelsToProperty = DependencyProperty.Register("ClipLabelsTo", typeof(OverlayClipBounds), typeof(OverlayPresenter), new PropertyMetadata(default(OverlayClipBounds), OnClipBoundsChanged));
        public static readonly DependencyProperty EnableHoverSelectionProperty = DependencyProperty.Register("EnableHoverSelection", typeof(bool), typeof(OverlayPresenter), new PropertyMetadata(true));

        public OverlayPresenter()
        {
            DefaultStyleKey = typeof(OverlayPresenter);

            //Bug fix: the designer isn't applying the style template correctly.  this fixes an exception it causes on mouseover in the designer.
            Content = new Image();
        }

        public Visibility OverlayVisibility
        {
            get { return (Visibility)GetValue(OverlayVisibilityProperty); }
            set { SetValue(OverlayVisibilityProperty, value); }
        }

        public UIElement Content
        {
            get { return (UIElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public OverlayClipBounds ClipLabelsTo
        {
            get { return (OverlayClipBounds)GetValue(ClipLabelsToProperty); }
            set { SetValue(ClipLabelsToProperty, value); }
        }

        public bool EnableHoverSelection
        {
            get { return (bool)GetValue(EnableHoverSelectionProperty); }
            set { SetValue(EnableHoverSelectionProperty, value); }
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayPresenter;
            if (control == null)
            {
                return;
            }

            //remove events
            var image = e.OldValue as Image;
            if (image != null)
            {
                image.ImageOpened -= control.OnImageOpened;
                image.ClearValue(Image.SourceProperty);
            }

            //add events
            image = e.NewValue as Image;
            if (image != null)
            {
                image.ImageOpened += control.OnImageOpened;
                //bind to source
                if (image.Source == null && image.GetBindingExpression(Image.SourceProperty) == null)
                {
                    image.SetBinding(Image.SourceProperty, new Binding() { Path = new PropertyPath("Source"), Source = control, Mode = BindingMode.OneWay });
                }
            }
        }

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bitmap = e.NewValue as BitmapImage;
            if (bitmap != null && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
            {
                //bitmap is already opened
                ((OverlayPresenter)d).OnImageOpened(bitmap, null);
            }
        }

        protected virtual void OnImageOpened(object sender, RoutedEventArgs e)
        {
            //set the DefaultSize after the image loads
            var image = sender as Image;
            var bitmap = image != null ? image.Source as BitmapImage : sender as BitmapImage;
            if (bitmap != null)
            {
                var size = new Size(bitmap.PixelWidth, bitmap.PixelHeight);
                DefaultOverlaySize = size;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //get required parts
            _mainGrid = GetTemplateChild(MainGridPart) as Grid;
            _contentGrid = GetTemplateChild(ContentGridPart) as Grid;
            if (_mainGrid == null)
            {
                throw new NullReferenceException($"{MainGridPart} is missing in the control template");
            }
            if (_contentGrid == null)
            {
                throw new NullReferenceException($"{ContentGridPart} is missing in the control template");
            }

            //connect events
            _contentGrid.PointerEntered += OnPointerEnteredMoved;
            _contentGrid.PointerMoved += OnPointerEnteredMoved;
            _contentGrid.PointerExited += OnPointerExited;

            //init effects
            _effectManager = new OverlayEffectManager(this, _mainGrid);

            //set clipbounds
            UpdateClipBounds();
        }

        void UpdateClipBounds()
        {
            //validate
            if (_contentGrid == null)
            {
                return;
            }

            var clipBounds = ClipLabelsTo;
            switch (clipBounds)
            {
                case OverlayClipBounds.None:
                    ClipHelper.SetToBounds(this, false);
                    ClipHelper.SetToBounds(_contentGrid, false);
                    break;
                case OverlayClipBounds.Control:
                    ClipHelper.SetToBounds(_contentGrid, false);
                    ClipHelper.SetToBounds(this, true);
                    break;
                case OverlayClipBounds.Content:
                    ClipHelper.SetToBounds(this, false);
                    ClipHelper.SetToBounds(_contentGrid, true);
                    break;
                default:
                    break;
            }
        }

        private static void OnClipBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //validate
            var control = d as OverlayPresenter;
            if (control == null)
            {
                return;
            }

            control.UpdateClipBounds();
        }

        public void OverlayVisualStateChanged(OverlayControl overlayControl, OverlayInfoState state, IList<OverlayEffectConfiguration> effects)
        {
            //validate
            var overlayInfo = overlayControl?.OverlayInfo;
            if (overlayInfo == null || effects == null || effects.Count == 0)
            {
                return;
            }

            //update effects
            _effectManager.UpdateEffects(overlayInfo, overlayInfo.OverlaySize ?? overlayControl.DefaultOverlaySize, state, _hasPresenterSelected, effects);
        }

        void OnPointerEnteredMoved(object sender, PointerRoutedEventArgs e)
        {
            //validate
            if (!EnableHoverSelection)
            {
                return;
            }

            //find all highlightables under the cursor
            var overlayInfos = VisualTreeHelper.FindElementsInHostCoordinates(e.GetCurrentPoint(null).Position, _contentGrid).OfType<OverlayControl>().Select(i => i.OverlayInfo).Where(i => i != null &&
                (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse || e.Pointer.IsInContact)); //test for touch exit event

            _hasPresenterSelected = true;

            //highlight new items
            foreach (var highlightable in overlayInfos.Except(_presenterSelected))
            {
                _presenterSelected.Add(highlightable);
                highlightable.IsSelected = true;
            }

            //unhighlight old items
            foreach (var highlightable in _presenterSelected.Except(overlayInfos).ToArray())
            {
                _presenterSelected.Remove(highlightable);
                highlightable.IsSelected = false;
            }

            _hasPresenterSelected = false;
        }

        void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            //validate
            if (!EnableHoverSelection)
            {
                return;
            }

            _hasPresenterSelected = true;

            //remove all highlights
            foreach (var highlightable in _presenterSelected)
            {
                highlightable.IsSelected = false;
            }
            _presenterSelected.Clear();

            _hasPresenterSelected = false;
        }
    }

    public enum OverlayClipBounds
    {
        None,
        Control,
        Content
    }
}
