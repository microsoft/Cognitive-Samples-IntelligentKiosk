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

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    /// <summary>
    /// base class to create an overlay effect
    /// </summary>
    public abstract class OverlayEffectProcessor
    {
        protected FrameworkElement OverlayPresenter;
        protected FrameworkElement OverlayControl;

        public OverlayEffectProcessor(FrameworkElement overlayPresenter, FrameworkElement overlayControl)
        {
            //set fields
            OverlayPresenter = overlayPresenter;
            OverlayControl = overlayControl;
        }

        public void ApplyEffect(IOverlayInfo overlayInfo, Size? defaultOverlaySize, OverlayEffectConfiguration config)
        {
            //validate
            var overlaySize = overlayInfo?.OverlaySize;
            overlaySize = overlaySize ?? defaultOverlaySize;
            if (overlaySize == null)
            {
                return;
            }

            StartEffect(overlayInfo, overlaySize.Value, config);
        }

        public void RemoveEffect(IOverlayInfo overlayInfo, Size? defaultOverlaySize, OverlayEffectConfiguration config)
        {
            var overlaySize = overlayInfo?.OverlaySize;
            overlaySize = overlaySize ?? defaultOverlaySize;

            EndEffect(overlayInfo, overlaySize.Value, config);
        }

        protected Point TransformPoint(Point point, Size overlaySize)
        {
            point = new Point((OverlayControl.RenderSize.Width / overlaySize.Width) * point.X, (OverlayControl.RenderSize.Height / overlaySize.Height) * point.Y);
            return OverlayControl.TransformToVisual(OverlayPresenter).TransformPoint(point);
        }

        protected abstract void StartEffect(IOverlayInfo overlayInfo, Size overlaySize, OverlayEffectConfiguration config);

        protected abstract void EndEffect(IOverlayInfo overlayInfo, Size? overlaySize, OverlayEffectConfiguration config);

        public abstract bool? ShouldApply(IOverlayInfo overlayInfo, Size? overlaySize, OverlayInfoState state, OverlayEffectConfiguration config, bool presenterSelected);
    }

    /// <summary>
    /// helper class to manage applying and removing effects
    /// </summary>
    public class OverlayEffectManager
    {
        Dictionary<Type, OverlayEffectProcessor> _effects;

        public OverlayEffectManager(FrameworkElement overlayPresenter, FrameworkElement overlayControl)
        {
            //set fields
            _effects = new Dictionary<Type, OverlayEffectProcessor>()
            {
                //register effects
                { typeof(ZoomEffect), new ZoomEffectProcessor(overlayPresenter, overlayControl) },
                { typeof(ToolTipEffect), new ToolTipEffectProcessor(overlayPresenter, overlayControl) },
            };
        }

        public void UpdateEffects(IOverlayInfo overlayInfo, Size? overlaySize, OverlayInfoState state, bool presenterSelected, IList<OverlayEffectConfiguration> effects)
        {
            foreach (var effect in effects)
            {
                if (_effects.ContainsKey(effect.GetType()))
                {
                    //get processor
                    var processor = _effects[effect.GetType()];

                    //determine if it should run
                    var shouldApply = processor.ShouldApply(overlayInfo, overlaySize, state, effect, presenterSelected);

                    if (shouldApply != null)
                    {
                        if (shouldApply.Value)
                        {
                            //apply effect
                            processor.ApplyEffect(overlayInfo, overlaySize, effect);
                        }
                        else
                        {
                            //remove effect
                            processor.RemoveEffect(overlayInfo, overlaySize, effect);
                        }
                    }

                }
            }
        }

    }

    public abstract class OverlayEffectConfiguration { }
}
