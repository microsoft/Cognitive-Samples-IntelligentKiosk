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

using IntelligentKioskSample.Models.InsuranceClaimAutomation;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Views.InsuranceClaimAutomation
{
    public sealed partial class DetailsViewControl : UserControl
    {
        private DataGridViewModel currentDataGridVM;

        public event EventHandler OnViewClosed;

        public DetailsViewControl()
        {
            this.InitializeComponent();
        }

        public void OpenDetailsView(DataGridViewModel data)
        {
            if (data != null)
            {
                currentDataGridVM = data;

                this.claimIdTextBlock.Text = data.ClaimId.ToString();
                this.customerNameTextBlock.Text = data.CustomName.Text;
                this.dateTextBlock.Text = data.Date.Text;
                this.warrantyIdTextBlock.Text = data.WarrantyId.Text;
                this.warrantyAmountTextBlock.Text = data.Warranty.Text;
                this.totalTextBlock.Text = data.InvoiceTotal.Text;

                this.productImage.Source = data.ProductImage;

                if (data.IsFormImage)
                {
                    this.formImage.Source = data.FormImage;
                    ShowFormFieldDetectionBoxes(data);
                }
                else if (data.FormFile != null)
                {
                    this.formVisualizationCanvas.Children.Clear();
                    this.pdfViewer.Source = data.FormFile;
                }
                this.formImage.Visibility = data.IsFormImage ? Visibility.Visible : Visibility.Collapsed;

                ShowObjectDetectionBoxes(data.ObjectDetectionMatches);
            }
        }

        private void OnCloseDetailsViewButtonClicked(object sender, RoutedEventArgs e)
        {
            this.currentDataGridVM = null;
            this.OnViewClosed?.Invoke(this, EventArgs.Empty);
        }

        private void ShowObjectDetectionBoxes(IEnumerable<PredictionModel> detectedObjects)
        {
            this.objectDetectionVisualizationCanvas.Children.Clear();

            if (detectedObjects != null && detectedObjects.Any())
            {
                double canvasWidth = objectDetectionVisualizationCanvas.ActualWidth;
                double canvasHeight = objectDetectionVisualizationCanvas.ActualHeight;

                foreach (PredictionModel prediction in detectedObjects)
                {
                    objectDetectionVisualizationCanvas.Children.Add(
                        new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.Lime),
                            BorderThickness = new Thickness(2),
                            Margin = new Thickness(prediction.BoundingBox.Left * canvasWidth, prediction.BoundingBox.Top * canvasHeight, 0, 0),
                            Width = prediction.BoundingBox.Width * canvasWidth,
                            Height = prediction.BoundingBox.Height * canvasHeight,
                        });

                    objectDetectionVisualizationCanvas.Children.Add(
                        new Border
                        {
                            Height = 40,
                            FlowDirection = FlowDirection.LeftToRight,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(prediction.BoundingBox.Left * canvasWidth, prediction.BoundingBox.Top * canvasHeight - 40, 0, 0),
                            Child = new Border
                            {
                                Background = new SolidColorBrush(Colors.Lime),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Bottom,
                                Child = new TextBlock
                                {
                                    Foreground = new SolidColorBrush(Colors.Black),
                                    Text = $"{prediction.TagName} ({Math.Round(prediction.Probability * 100)}%)",
                                    FontSize = 16,
                                    Margin = new Thickness(6, 0, 6, 0)
                                }
                            }
                        });
                }
            }
        }

        private void ShowFormFieldDetectionBoxes(DataGridViewModel data)
        {
            this.formVisualizationCanvas.Children.Clear();

            if (data != null)
            {
                AddFormFieldBoxes(data.CustomName);
                AddFormFieldBoxes(data.Date);
                AddFormFieldBoxes(data.WarrantyId);
                AddFormFieldBoxes(data.Warranty);
                AddFormFieldBoxes(data.InvoiceTotal);
            }
        }

        private void AddFormFieldBoxes(TokenOverlayInfo boxInfo)
        {
            if (boxInfo?.RectList != null && boxInfo.RectList.Any())
            {
                double canvasWidth = formVisualizationCanvas.ActualWidth;
                double canvasHeight = formVisualizationCanvas.ActualHeight;

                double pageWidth = boxInfo.PageWidth;
                double pageHeight = boxInfo.PageHeight;

                double renderedImageXTransform = pageWidth > 0 ? canvasWidth / pageWidth : 0;
                double renderedImageYTransform = pageHeight > 0 ? canvasHeight / pageHeight : 0;

                foreach (Rect rect in boxInfo.RectList)
                {
                    formVisualizationCanvas.Children.Add(new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Lime),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(rect.Left * renderedImageXTransform, rect.Top * renderedImageYTransform, 0, 0),
                        Width = rect.Width * renderedImageXTransform,
                        Height = rect.Height * renderedImageYTransform,
                    });
                }
            }
        }

        private void OnObjectDetectionVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentDataGridVM != null && this.objectDetectionVisualizationCanvas.Children.Any())
            {
                this.ShowObjectDetectionBoxes(this.currentDataGridVM.ObjectDetectionMatches);
            }
        }

        private void OnFormVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentDataGridVM != null && this.formVisualizationCanvas.Children.Any())
            {
                this.ShowFormFieldDetectionBoxes(this.currentDataGridVM);
            }
        }
    }
}
