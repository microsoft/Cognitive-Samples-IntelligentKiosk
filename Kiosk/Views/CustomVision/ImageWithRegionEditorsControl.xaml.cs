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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace IntelligentKioskSample.Views.CustomVision
{
    public sealed partial class ImageWithRegionEditorsControl : UserControl
    {
        private ImageViewModel imageViewModel;
        private bool isThumbnailOpened;
        private bool needsSaving = false;

        public event EventHandler<ImageViewModel> RegionsChanged;

        public ImageWithRegionEditorsControl()
        {
            this.InitializeComponent();
        }

        private void OnTaggedImageOpened(object sender, RoutedEventArgs e)
        {
            this.isThumbnailOpened = true;

            AddImageRegionsToUI();
        }

        private void AddImageRegionsToUI()
        {
            imageRegionsCanvas.Children.Clear();
            if (imageViewModel.Image.Regions != null)
            {
                foreach (var region in imageViewModel.Image.Regions)
                {
                    AddRegionToUI(region);
                }
            }
        }

        private void AddRegionToUI(ImageRegion region)
        {
            var editor = new RegionEditorControl
            {
                Width = imageControl.ActualWidth * region.Width,
                Height = imageControl.ActualHeight * region.Height,
                Margin = new Thickness(region.Left * imageControl.ActualWidth, region.Top * imageControl.ActualHeight, 0, 0),
                DataContext = new RegionEditorViewModel
                {
                    Region = region,
                    AvailableTags = this.imageViewModel.AvailableTags,
                    TagHintForNewRegions = this.imageViewModel.TagHintForNewRegions
                }
            };

            editor.RegionChanged += OnRegionChanged;
            editor.RegionDeleted += OnRegionDeleted;

            imageRegionsCanvas.Children.Add(editor);
        }

        private void OnRegionDeleted(object sender, EventArgs e)
        {
            RegionEditorControl regionControl = (RegionEditorControl)sender;
            this.imageRegionsCanvas.Children.Remove(regionControl);

            var regionBeingDeleted = ((RegionEditorViewModel)regionControl.DataContext).Region;

            ImageViewModel imgViewModel = (ImageViewModel)this.DataContext;
            if (imgViewModel.Image.Regions != null && imgViewModel.Image.Regions.Contains(regionBeingDeleted))
            {
                imgViewModel.DeletedImageRegions.Add(regionBeingDeleted);
            }
            else if (imgViewModel.AddedImageRegions.Contains(regionBeingDeleted))
            {
                imgViewModel.AddedImageRegions.Remove(regionBeingDeleted);
            }

            needsSaving = true;
        }

        private void OnRegionChanged(object sender, EventArgs e)
        {
            RegionEditorControl regionControl = (RegionEditorControl)sender;

            RegionEditorViewModel regionEditorViewModel = (RegionEditorViewModel)regionControl.DataContext;

            ImageRegion regionObject = regionEditorViewModel.Region;

            // Update size in case is changed
            regionObject.Left = EnsureValidNormalizedValue(regionControl.Margin.Left / imageControl.ActualWidth);
            regionObject.Top = EnsureValidNormalizedValue(regionControl.Margin.Top / imageControl.ActualHeight);
            regionObject.Width = EnsureValidNormalizedValue(regionControl.ActualWidth / imageControl.ActualWidth);
            regionObject.Height = EnsureValidNormalizedValue(regionControl.ActualHeight / imageControl.ActualHeight);

            if (regionObject.Width + regionObject.Left > 1)
            {
                regionObject.Width = 1 - regionObject.Left;
            }

            if (regionObject.Height + regionObject.Top > 1)
            {
                regionObject.Height = 1 - regionObject.Top;
            }

            needsSaving = true;
        }

        private static double EnsureValidNormalizedValue(double value)
        {
            // ensure [0,1]
            return Math.Min(1, Math.Max(0, Math.Round(value, 2)));
        }

        private void OnEditorFlyoutOpened(object sender, object e)
        {
            needsSaving = false;
        }

        private void OnEditorFlyoutClosed(object sender, object e)
        {
            if (needsSaving)
            {
                this.RegionsChanged?.Invoke(this, (ImageViewModel)this.DataContext);
            }
        }

        private void OnControlDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (this.imageViewModel != null)
            {
                this.imageViewModel.ImageDataChanged -= ImageViewModel_ImageRegionsChanged;
            }

            this.imageViewModel = (ImageViewModel)args.NewValue;
            if (imageViewModel != null)
            {
                this.imageViewModel.ImageDataChanged += ImageViewModel_ImageRegionsChanged;
            }

            imageRegionsCanvas.Children.Clear();
        }

        private void ImageViewModel_ImageRegionsChanged(object sender, EventArgs e)
        {
            if (this.isThumbnailOpened)
            {
                this.AddImageRegionsToUI();
            }
        }

        private void OnPointerReleasedOverImage(object sender, PointerRoutedEventArgs e)
        {
            var clickPosition = e.GetCurrentPoint(this.imageRegionsCanvas);

            double normalizedPosX = clickPosition.Position.X / imageRegionsCanvas.ActualWidth;
            double normalizedPosY = clickPosition.Position.Y / imageRegionsCanvas.ActualHeight;
            double normalizedWidth = 50 / imageRegionsCanvas.ActualWidth;
            double normalizedHeight = 50 / imageRegionsCanvas.ActualHeight;

            ImageRegion newRegion = new ImageRegion(
                tagId: default(Guid),
                left: normalizedPosX,
                top: normalizedPosY,
                width: normalizedWidth + normalizedPosX > 1 ? 1 - normalizedPosX : normalizedWidth,
                height: normalizedHeight + normalizedPosY > 1 ? 1 - normalizedPosY : normalizedHeight,
                tagName: this.imageViewModel.TagHintForNewRegions.Name,
                created: default(DateTime),
                regionId: this.imageViewModel.TagHintForNewRegions.Id);

            ImageViewModel imageViewModel = (ImageViewModel)this.DataContext;
            imageViewModel.AddedImageRegions.Add(newRegion);

            this.AddRegionToUI(newRegion);

            needsSaving = true;
        }
    }
}
