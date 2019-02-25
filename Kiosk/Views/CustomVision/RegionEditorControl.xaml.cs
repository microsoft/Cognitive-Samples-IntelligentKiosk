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

using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace IntelligentKioskSample.Views.CustomVision
{
    public class RegionEditorViewModel
    {
        public IEnumerable<Tag> AvailableTags { get; set; }
        public Tag TagHintForNewRegions { get; set; }
        public ImageRegion Region { get; set; }
    }

    public sealed partial class RegionEditorControl : UserControl
    {
        public event EventHandler RegionChanged;
        public event EventHandler RegionDeleted;

        public RegionEditorControl()
        {
            this.InitializeComponent();
        }

        private void OnTopLeftDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(e.HorizontalChange, e.VerticalChange, 0, 0);
        }

        private void OnBottomRightDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(0, 0, e.HorizontalChange, e.VerticalChange);
        }

        private void OnTopRightDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(0, e.VerticalChange, e.HorizontalChange, 0);
        }

        private void OnBottomLeftDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(e.HorizontalChange, 0, 0, e.VerticalChange);
        }

        private void AdjustBoundingBoxSize(double leftOffset, double topOffset, double widthOffset, double heightOffset)
        {
            double newWidth = this.ActualWidth + widthOffset - leftOffset;
            double newHeight = this.ActualHeight + heightOffset - topOffset;

            if ((newWidth >= 0) && (newHeight >= 0))
            {
                this.Width = newWidth;
                this.Height = newHeight;

                this.Margin = new Thickness(
                    this.Margin.Left + leftOffset,
                    this.Margin.Top + topOffset,
                    this.Margin.Right,
                    this.Margin.Bottom);
            }
        }

        private void OnThumbReleased(object sender, PointerRoutedEventArgs e)
        {
            this.RegionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DeleteRegionClicked(object sender, RoutedEventArgs e)
        {
            this.RegionDeleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnTagComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Any())
            {
                this.RegionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
