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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Views.CustomVision
{
    public sealed partial class ThumbnailWithRegionsControl : UserControl
    {
        private ImageViewModel imageViewModel;
        private bool isThumbnailOpened;

        public ThumbnailWithRegionsControl()
        {
            this.InitializeComponent();
        }

        private void OnTaggedThubnailImageOpened(object sender, RoutedEventArgs e)
        {
            this.isThumbnailOpened = true;

            AddImageRegionsToUI();
        }

        private void AddImageRegionsToUI()
        {
            imageRegionsCanvas.Children.Clear();
            if (imageViewModel?.Image?.Regions != null)
            {
                foreach (var region in imageViewModel.Image.Regions)
                {
                    imageRegionsCanvas.Children.Add(new Windows.UI.Xaml.Shapes.Rectangle
                    {
                        Width = thumbnailImage.ActualWidth * region.Width,
                        Height = thumbnailImage.ActualHeight * region.Height,
                        Margin = new Thickness(region.Left * thumbnailImage.ActualWidth, region.Top * thumbnailImage.ActualHeight, 0, 0),
                        StrokeThickness = 1,
                        Stroke = new SolidColorBrush(Colors.Lime)
                    });
                }
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
    }
}
