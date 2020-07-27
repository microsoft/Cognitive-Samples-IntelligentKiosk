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

namespace IntelligentKioskSample.Controls.Overlays.Primitives
{
    public class OverlayInfo : IOverlayInfo
    {
        OverlayInfoState _visualState;
        bool _isSelected;
        bool _isMuted;
        bool _isVisible = true;

        public event EventHandler<OverlayInfoState> VisualStateChanged;

        public Size? OverlaySize { get; set; }
        public Rect Rect { get; set; }
        public object Entity {get;set;}
        public IList<object> Labels { get; set; }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; UpdateVisualState(); } }
        public bool IsMuted { get => _isMuted; set { _isMuted = value; UpdateVisualState(); } }
        public bool IsVisible { get => _isVisible; set { _isVisible = value; UpdateVisualState(); } }
        public object ToolTip { get; set; }
        public OverlayInfoState VisualState { get => _visualState; }

        void UpdateVisualState()
        {
            //update value
            var newVisualState = OverlayInfoState.Normal;
            if (!IsVisible)
            {
                newVisualState = OverlayInfoState.Hidden;
            }
            else if (IsSelected)
            {
                newVisualState = OverlayInfoState.Selected;
            }
            else if (IsMuted)
            {
                newVisualState = OverlayInfoState.Muted;
            }
            var hasChanged = _visualState != newVisualState;
            _visualState = newVisualState;

            //send event
            if (hasChanged)
            {
                VisualStateChanged?.Invoke(this, _visualState);
            }
        }
    }

    public class OverlayInfo<TEntity> : OverlayInfo
    {
        public TEntity EntityExt { get => (TEntity)base.Entity; set => base.Entity = value; }
    }

    public class OverlayInfo<TEntity, TLabel> : OverlayInfo
    {
        public TEntity EntityExt { get => (TEntity)base.Entity; set => base.Entity = value; }
        public IList<TLabel> LabelsExt { get => (IList<TLabel>)base.Labels; set => base.Labels = (IList<object>)value; }
    }

    public class OverlayInfo<TEntity, TLabel, TToolTip> : OverlayInfo
    {
        public TEntity EntityExt { get => (TEntity)base.Entity; set => base.Entity = value; }
        public IList<TLabel> LabelsExt { get => (IList<TLabel>)base.Labels; set => base.Labels = (IList<object>)value; }
        public TToolTip ToolTipExt { get => (TToolTip)base.ToolTip; set => base.ToolTip = value; }
    }

    public interface IOverlayInfo
    {
        event EventHandler<OverlayInfoState> VisualStateChanged;
        Size? OverlaySize { get; }
        Rect Rect { get; }
        object Entity { get; }
        IList<object> Labels { get; }
        bool IsSelected { get; set; }
        bool IsMuted { get; set; }
        bool IsVisible { get; set; }
        object ToolTip { get; set; }
        OverlayInfoState VisualState { get; }
    }

    public enum OverlayInfoState
    {
        Normal,
        Selected,
        Muted,
        Hidden
    }
}
