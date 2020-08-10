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
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    /// <summary>
    /// supports a tooltip using an IOverlayInfo
    /// </summary>
    public class ToolTipEffectProcessor : OverlayEffectProcessor
    {
        Popup _popup;
        Image _image;
        Viewbox _imageView;
        Viewbox _view;
        PolygonInfo _overlayPolygon;

        IOverlayInfo _currentOverlayInfo;
        Point _offset;
        Point _prevPosition;
        Rect? _overlayRect;
        Size _overlaySize;
        ToolTipMode _toolTipMode = ToolTipMode.Standard;
        bool _presenterSelected;

        public ToolTipEffectProcessor(FrameworkElement overlayPresenter, FrameworkElement overlayControl) : base(overlayPresenter, overlayControl)
        {
            //set fields
            _popup = new Popup();
            OverlayPresenter.PointerMoved += RenderTarget_PointerMoved;
            OverlayPresenter.PointerPressed += RenderTarget_PointerMoved;
        }

        void RenderTarget_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            //set popup position above mouse
            UpdatePosition(e.GetCurrentPoint(null).Position);
        }

        void UpdatePosition(Point? position = null)
        {
            if (position == null)
            {
                position = _prevPosition;
            }
            _popup.HorizontalOffset = position.Value.X - _offset.X;
            _popup.VerticalOffset = position.Value.Y - _offset.Y;
            _prevPosition = position.Value;
        }

        public override bool? ShouldApply(IOverlayInfo overlayInfo, Size? overlaySize, OverlayInfoState state, OverlayEffectConfiguration config, bool presenterSelected)
        {
            _presenterSelected = presenterSelected;
            _toolTipMode = (config as ToolTipEffect)?.ToolTipMode ?? ToolTipMode.Standard;
            
            return state == OverlayInfoState.Selected && (presenterSelected || _toolTipMode == ToolTipMode.Extended);
        }

        protected override void StartEffect(IOverlayInfo overlayInfo, Size overlaySize, OverlayEffectConfiguration config)
        {
            //validate
            var settings = config as ToolTipEffect;
            if (settings == null)
            {
                return;
            }
            if (settings.Template == null)
            {
                return;
            }

            _overlaySize = overlaySize;
            _overlayRect = overlayInfo?.Rect;
            _overlayPolygon = overlayInfo?.Entity as PolygonInfo;

            var tooltip = overlayInfo?.ToolTip;
            if (tooltip != null)
            {
                //end current tooltip
                if (_currentOverlayInfo != null)
                {
                    EndEffect(_currentOverlayInfo, overlaySize, config);
                }

                //create template
                var toolTipControl = settings.Template.LoadContent() as FrameworkElement;
                toolTipControl.DataContext = tooltip;
                toolTipControl.Loaded += ToolTipControl_Loaded;

                if (settings.ToolTipMode == ToolTipMode.Extended)
                {
                    var panel = new StackPanel()
                    {
                        Background = Application.Current.Resources["SystemControlBackgroundChromeMediumLowBrush"] as Brush,
                        BorderBrush = Application.Current.Resources["SystemControlForegroundChromeHighBrush"] as Brush,
                        BorderThickness = new Thickness(1)
                    };

                    ImageSource imageSource = (OverlayPresenter as OverlayPresenter)?.Source;
                    if (imageSource != null)
                    {
                        _image = new Image() { Source = imageSource, Width = (double)(imageSource as BitmapImage)?.PixelWidth, Height = (double)(imageSource as BitmapImage)?.PixelHeight };
                        _imageView = new Viewbox() { Child = _image, Stretch = Stretch.None };
                        _view = new Viewbox { Child = _imageView };
                        panel.Children.Add(_view);
                    }
                    panel.Children.Add(toolTipControl);
                    _popup.Child = panel;
                }
                else
                {
                    _popup.Child = toolTipControl;
                }

                //show popup
                _popup.IsOpen = true;

                _currentOverlayInfo = overlayInfo;
            }
        }

        private void ToolTipControl_Loaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            double feWidth = fe.DesiredSize.Width;
            double feHeight = fe.DesiredSize.Height;

            Point? position = null;
            _offset = new Point(feWidth / 2, feHeight + 10);

            if (_toolTipMode == ToolTipMode.Extended && _overlayRect.HasValue)
            {
                // tooltip panel
                if (_view != null)
                {
                    double x = _overlayRect.Value.X;
                    double y = _overlayRect.Value.Y;
                    double width = _overlayRect.Value.Width;
                    double height = _overlayRect.Value.Height;
                    double padding = ((height < width) ? height : width) * .1;

                    x -= padding;
                    y -= padding;
                    width += padding * 2;
                    height += padding * 2;

                    _view.Width = feWidth;
                    _view.Height = (height / width) * feWidth;
                    _imageView.Width = width;
                    _imageView.Height = height;
                    _image.RenderTransform = new TranslateTransform() { X = -x, Y = -y };
                    _offset.Y += _view.Height;
                }

                // tooltip position
                if (!_presenterSelected)
                {
                    Point positionOffset = new Point(0, 0);
                    if (_overlayPolygon?.Points != null && _overlayPolygon.Points.Any())
                    {
                        double renderedImageXTransform = OverlayControl.ActualWidth / _overlaySize.Width;
                        double renderedImageYTransform = OverlayControl.ActualHeight / _overlaySize.Height;
                        double polygonMinX = _overlayPolygon.Points.Select(p => p.X).Min();
                        double polygonMinY = _overlayPolygon.Points.Select(p => p.Y).Min();
                        double polygonMiddleX = Math.Abs(_overlayPolygon.Points[1].X - _overlayPolygon.Points[0].X) / 2;
                        double polygonMiddleY = Math.Abs(_overlayPolygon.Points[1].Y - _overlayPolygon.Points[0].Y) / 2;
                        double xDelta = _overlayPolygon.Points[0].X - polygonMinX;

                        // get middle position in the top line of polygon
                        positionOffset.X += (polygonMinX + xDelta + polygonMiddleX) * renderedImageXTransform;
                        positionOffset.Y += (polygonMinY + polygonMiddleY) * renderedImageYTransform;
                    }

                    Point positionPoint = OverlayControl.TransformToVisual(Window.Current.Content).TransformPoint(new Point(0, 0));
                    positionPoint.X += positionOffset.X;
                    positionPoint.Y += positionOffset.Y;
                    position = positionPoint;
                }
            }

            UpdatePosition(!_presenterSelected ? position : null);
        }

        protected override void EndEffect(IOverlayInfo overlayInfo, Size? overlaySize, OverlayEffectConfiguration config)
        {
            //remove popup
            if (_currentOverlayInfo == overlayInfo)
            {
                if (_popup.IsOpen)
                {
                    _popup.IsOpen = false;
                }
                _currentOverlayInfo = null;
            }
        }
    }

    /// <summary>
    /// enables and configures a tooltip effect on an overlay control
    /// </summary>
    public class ToolTipEffect : OverlayEffectConfiguration
    {
        public DataTemplate Template { get; set; }

        public ToolTipMode ToolTipMode { get; set; } = ToolTipMode.Standard;
    }

    public enum ToolTipMode
    {
        Standard,
        Extended
    }
}
