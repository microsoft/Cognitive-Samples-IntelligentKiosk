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

using IntelligentKioskSample.Controls.Animation;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    /// <summary>
    /// supports zooming in on an image using an IOverlayInfo
    /// </summary>
    public class ZoomEffectProcessor : OverlayEffectProcessor
    {
        BaseAnimation[] _animations = new BaseAnimation[4];

        public ZoomEffectProcessor(FrameworkElement overlayPresenter, FrameworkElement overlayControl) : base(overlayPresenter, overlayControl)
        {
        }

        public override bool? ShouldApply(IOverlayInfo overlayInfo, Size? overlaySize, OverlayInfoState state, OverlayEffectConfiguration config, bool presenterSelected)
        {
            //only works when not selected from the image
            if (!presenterSelected)
            {
                return state == OverlayInfoState.Selected;
            }
            return null;
        }

        protected override void StartEffect(IOverlayInfo overlayInfo, Size overlaySize, OverlayEffectConfiguration config)
        {
            //validate
            var settings = config as ZoomEffect;
            if (settings == null)
            {
                return;
            }

            //start new movement
            var zoom = 1d;
            var focus = new Point();
            if (overlayInfo != null)
            {
                var bounds = overlayInfo.Rect;

                //calculate zoom
                var viewerRatio = OverlayPresenter.RenderSize.Width / OverlayPresenter.RenderSize.Height;
                var boundRatio = bounds.Width / bounds.Height;
                if (viewerRatio < boundRatio)
                {
                    //zoom by width
                    zoom = OverlayPresenter.RenderSize.Width / bounds.Width;
                }
                else
                {
                    //zoom by height
                    zoom = OverlayPresenter.RenderSize.Height / bounds.Height;
                }
                zoom = Math.Min(zoom, settings.MaxZoom); //max zoom
                zoom = Math.Max(zoom, settings.MinZoom); //min zoom

                //focus on center
                var boundCenter = new Point(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
                var target = TransformPoint(boundCenter, overlaySize);
                var viewerCenter = new Point(OverlayPresenter.RenderSize.Width / 2, OverlayPresenter.RenderSize.Height / 2);
                focus = new Point(-(target.X * zoom) + viewerCenter.X, -(target.Y * zoom) + viewerCenter.Y);

            }
            _animations[2] = _animations[0];
            _animations[3] = _animations[1];
            _animations[0] = new ScaleTransformAnimation(zoom, zoom, settings.Duration, BaseAnimation.DefaultInOut);
            _animations[1] = new TranslateTransformAnimation(focus.X, focus.Y, settings.Duration, BaseAnimation.DefaultInOut);
            _animations[0].Activate(OverlayPresenter);
            _animations[1].Activate(OverlayPresenter);
        }

        protected override void EndEffect(IOverlayInfo overlayInfo, Size? overlaySize, OverlayEffectConfiguration config)
        {
            StartEffect(null, overlaySize.GetValueOrDefault(), config);
        }
    }


    /// <summary>
    /// enables and configures a zoom effect on an overlay control
    /// </summary>
    public class ZoomEffect : OverlayEffectConfiguration
    {
        public double MaxZoom { get; set; } = 2.5;
        public double MinZoom { get; set; } = 1.0;
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(.5);
    }
}
