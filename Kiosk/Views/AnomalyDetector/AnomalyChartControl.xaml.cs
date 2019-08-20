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

using ServiceHelpers;
using ServiceHelpers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Views.AnomalyDetector
{
    public sealed partial class AnomalyChartControl : UserControl
    {
        private static readonly string StopDetectButton = "Stop Detect";
        private static readonly string StartDetectButton = "Detect Anomalies";

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(ScenarioInfoControl),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        private Visual xamlRoot;
        private Compositor compositor;
        private ContainerVisual containerRoot;
        private SpriteVisual progressIndicator;

        private bool isProcessing = false;
        private bool shouldStopCurrentRun = false;
        private float maxVolumeInSampleBuffer = 0;
        private int selectedDetectionSensitivity = 90;

        private AnomalyDetectorModelData selectedDemoData;
        private AnomalyEntireDetectResult anomalyEntireDetectResult;
        private List<SpriteVisual> allAnomalyIndicators = new List<SpriteVisual>();
        private AnomalyDetectorServiceType selectedDetectionMode = AnomalyDetectorServiceType.Streaming;

        public event EventHandler StartLiveAudio;
        public event EventHandler StopLiveAudio;

        public AnomalyChartControl()
        {
            this.InitializeComponent();
            PrepareUIElements();
        }

        public void InitializeChart(UserStoryType scenarioType, AnomalyDetectorServiceType detectionMode, double sensitivy)
        {
            if (AnomalyDetectorScenarioLoader.AllModelData.ContainsKey(scenarioType))
            {
                selectedDemoData = AnomalyDetectorScenarioLoader.AllModelData[scenarioType];

                // clear state
                ResetState();

                // set default value: slider and radio buttons
                this.sensitivitySlider.Value = sensitivy;
                selectedDetectionMode = detectionMode;
                
                batchOption.IsChecked = selectedDetectionMode == AnomalyDetectorServiceType.Batch;
                streamingOption.IsChecked = selectedDetectionMode == AnomalyDetectorServiceType.Streaming;
            }
        }

        public void SetVolumeValue(float value)
        {
            maxVolumeInSampleBuffer = value;
        }

        private async void OnChartGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (selectedDemoData == null)
            {
                return;
            }

            dataPolyline.Points?.Clear();

            // update data points
            if (selectedDemoData.UserStory.StoryType != UserStoryType.Live)
            {
                dataPolyline.Points = GetPointCollectionByUserStoryData(selectedDemoData);
            }

            // update anomaly data
            switch (selectedDetectionMode)
            {
                case AnomalyDetectorServiceType.Batch:
                    CleanAndRestoreUI();
                    await StartBatchModeProcessAsync();
                    break;

                case AnomalyDetectorServiceType.Streaming:
                    shouldStopCurrentRun = true;
                    break;
            }
        }

        private void PrepareUIElements()
        {
            xamlRoot = ElementCompositionPreview.GetElementVisual(ResultPanel);
            compositor = xamlRoot.Compositor;
            containerRoot = compositor.CreateContainerVisual();

            ElementCompositionPreview.SetElementChildVisual(resultGrid, containerRoot);

            progressIndicator = compositor.CreateSpriteVisual();
            progressIndicator.Size = new Vector2(10, 10);
            progressIndicator.AnchorPoint = new Vector2(0.5f, 0.5f);
            progressIndicator.Brush = compositor.CreateColorBrush(Colors.SlateGray);

            containerRoot.Children.InsertAtTop(progressIndicator);
            progressIndicator.Offset = new Vector3(0, (int)resultGrid.ActualHeight, resultGrid.CenterPoint.Z);

            progressLine.X1 = progressIndicator.CenterPoint.X;
            progressLine.X2 = progressIndicator.CenterPoint.X;
            progressLine.Y1 = 0;
            progressLine.Y2 = resultGrid.ActualHeight;
        }

        private void OnSensitivitySliderChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider slider)
            {
                selectedDetectionSensitivity = (int)slider.Value;
            }
        }

        private async void OnStartDetectionButtonClicked(object sender, RoutedEventArgs e)
        {
            if (selectedDemoData == null)
            {
                return;
            }

            try
            {
                if (isProcessing)
                {
                    shouldStopCurrentRun = true;
                    detectingAnomalyBtn.Content = StartDetectButton;
                }
                else
                {
                    ResetState();
                    isProcessing = true;
                    shouldStopCurrentRun = false;

                    detectingAnomalyBtn.Content = StopDetectButton;
                    switch (selectedDemoData.UserStory.StoryType)
                    {
                        case UserStoryType.Live:
                            this.StartLiveAudio?.Invoke(this, EventArgs.Empty);
                            await StartLiveDemoProcessAsync();
                            break;

                        default:
                            switch (selectedDetectionMode)
                            {
                                case AnomalyDetectorServiceType.Batch:
                                    this.anomalyEntireDetectResult = await AnomalyDetectorHelper.GetBatchDetectionResult(selectedDemoData, (int)sensitivitySlider.Value);
                                    await StartBatchModeProcessAsync();
                                    break;

                                case AnomalyDetectorServiceType.Streaming:
                                default:
                                    await StartStreamingModeProcessAsync();
                                    break;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure after click Anomaly Detect button.");
            }
            finally
            {
                isProcessing = false;
                detectingAnomalyBtn.Content = StartDetectButton;
            }
        }

        private async Task StartLiveDemoProcessAsync()
        {
            try
            {
                double yScale = (resultGrid.ActualHeight / (200 * 1.1));
                double x_Offset = resultGrid.ActualWidth / AnomalyDetectorScenarioLoader.DefaultDurationOfLiveDemoInSecond;

                DateTime currentTime = DateTime.Now;
                DateTime startTime = currentTime.AddMinutes((double)AnomalyDetectorScenarioLoader.DefaultDurationOfLiveDemoInSecond * -1);

                progressLine.X1 = progressIndicator.CenterPoint.X;
                progressLine.X2 = progressIndicator.CenterPoint.X;
                progressLine.Y1 = 0;
                progressLine.Y2 = resultGrid.ActualHeight;

                int startIndex = 14;

                dataPolyline.Points.Clear();

                for (int i = 0; i < startIndex; i++)
                {
                    selectedDemoData.AllData.Insert(i, new TimeSeriesData(startTime.ToString(), GetVolumeValue()));
                    startTime = startTime.AddMinutes(GetTimeOffsetInMinute(selectedDemoData.UserStory.Granuarity));

                    Point point = new Point((x_Offset * i), resultGrid.ActualHeight - (yScale * selectedDemoData.AllData[i].Value));
                    dataPolyline.Points.Add(point);

                    Point newUpperPoint = new Point(dataPolyline.Points[i].X, 0);
                    Point newLowerPoint = new Point(dataPolyline.Points[i].X, resultGrid.ActualHeight);

                    int endOfUpper = (detectWindowPolyline.Points.Count / 2);
                    int endOfLower = (detectWindowPolyline.Points.Count / 2 + 1);

                    detectWindowPolyline.Points.Insert(endOfUpper, newUpperPoint);
                    detectWindowPolyline.Points.Insert(endOfLower, newLowerPoint);
                }

                for (int i = startIndex; i < AnomalyDetectorScenarioLoader.DefaultDurationOfLiveDemoInSecond; i++)
                {
                    if (shouldStopCurrentRun)
                    {
                        break;
                    }

                    selectedDemoData.AllData.Insert(i, new TimeSeriesData(startTime.ToString(), GetVolumeValue()));
                    startTime = startTime.AddMinutes(GetTimeOffsetInMinute(selectedDemoData.UserStory.Granuarity));

                    AnomalyLastDetectResult result = await AnomalyDetectorHelper.GetStreamingDetectionResult(selectedDemoData, i, (int)sensitivitySlider.Value);

                    Point point = new Point((x_Offset * i), resultGrid.ActualHeight - (yScale * selectedDemoData.AllData[i].Value));
                    dataPolyline.Points.Add(point);

                    if (result != null)
                    {
                        DrawProgressByDetectionResult(dataPolyline.Points[i], result, yScale);
                    }
                }

                this.StopLiveAudio?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure during streaming detection.");
            }
        }

        private async Task StartBatchModeProcessAsync()
        {
            try
            {
                detectingAnomalyBtn.IsEnabled = false;

                double yScale = (resultGrid.ActualHeight / (selectedDemoData.MaxValue * 1.1));

                for (int i = 0; i < dataPolyline.Points.Count; i++)
                {
                    progressIndicator.Offset = new Vector3(float.Parse(dataPolyline.Points[i].X.ToString()), float.Parse(resultGrid.ActualHeight.ToString()), resultGrid.CenterPoint.Z);

                    progressLine.X1 = dataPolyline.Points[i].X;
                    progressLine.X2 = dataPolyline.Points[i].X;

                    if (anomalyEntireDetectResult != null)
                    {
                        Point newUpperPoint = new Point(dataPolyline.Points[i].X, resultGrid.ActualHeight - (yScale * (anomalyEntireDetectResult.ExpectedValues[i] + anomalyEntireDetectResult.UpperMargins[i])));
                        Point newLowerPoint = new Point(dataPolyline.Points[i].X, resultGrid.ActualHeight - (yScale * (anomalyEntireDetectResult.ExpectedValues[i] - anomalyEntireDetectResult.LowerMargins[i])));

                        Point actualPoint = dataPolyline.Points[i];

                        if (anomalyEntireDetectResult.IsAnomaly[i])
                        {
                            SpriteVisual anomalyIndicator = GetNewAnomalyIndicator(dataPolyline.Points[i]);

                            containerRoot.Children.InsertAtTop(anomalyIndicator);

                            allAnomalyIndicators.Add(anomalyIndicator);
                        }

                        int endOfUpper = (progressPolyline.Points.Count / 2);
                        int endOfLower = (progressPolyline.Points.Count / 2 + 1);

                        progressPolyline.Points.Insert(endOfUpper, newUpperPoint);
                        progressPolyline.Points.Insert(endOfLower, newLowerPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure during batch detection.");
            }
            finally
            {
                detectingAnomalyBtn.IsEnabled = true;
            }
        }

        private async Task StartStreamingModeProcessAsync()
        {
            try
            {
                double yScale = (resultGrid.ActualHeight / (selectedDemoData.MaxValue * 1.1));
                int startIndex = AnomalyDetectorModelData.DefaultMinimumStartIndex < selectedDemoData.IndexOfFirstValidPoint ? AnomalyDetectorModelData.DefaultMinimumStartIndex : selectedDemoData.IndexOfFirstValidPoint;

                for (int i = 0; i < startIndex; i++)
                {
                    Point newUpperPoint = new Point(dataPolyline.Points[i].X, 0);
                    Point newLowerPoint = new Point(dataPolyline.Points[i].X, resultGrid.ActualHeight);

                    int endOfUpper = (detectWindowPolyline.Points.Count / 2);
                    int endOfLower = (detectWindowPolyline.Points.Count / 2 + 1);

                    detectWindowPolyline.Points.Insert(endOfUpper, newUpperPoint);
                    detectWindowPolyline.Points.Insert(endOfLower, newLowerPoint);
                }

                for (int i = startIndex; i < dataPolyline.Points.Count; i++)
                {
                    if (shouldStopCurrentRun)
                    {
                        break;
                    }

                    AnomalyLastDetectResult result = await AnomalyDetectorHelper.GetStreamingDetectionResult(selectedDemoData, i, (int)sensitivitySlider.Value);

                    if (result != null)
                    {
                        DrawProgressByDetectionResult(dataPolyline.Points[i], result, yScale);
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure during streaming detection.");
            }
        }

        private void OnDetectionModeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                Enum.TryParse(rb.Tag.ToString(), out AnomalyDetectorServiceType detectionMode);
                selectedDetectionMode = detectionMode;
            }
        }

        private SpriteVisual GetNewAnomalyIndicator(Point anomalyPoint)
        {
            SpriteVisual anomaly;
            anomaly = compositor.CreateSpriteVisual();
            anomaly.Size = new Vector2(10, 10);
            anomaly.AnchorPoint = new Vector2(0.5f, 0.5f);
            anomaly.Brush = compositor.CreateColorBrush(Colors.Yellow);
            anomaly.Offset = new Vector3((float)(anomalyPoint.X), (float)(anomalyPoint.Y), resultGrid.CenterPoint.Z);
            return anomaly;
        }

        private PointCollection GetPointCollectionByUserStoryData(AnomalyDetectorModelData userStoryData)
        {
            PointCollection dataPoints = new PointCollection();

            if (userStoryData.AllData != null && userStoryData.AllData.Any())
            {
                double yScale = (resultGrid.ActualHeight / (userStoryData.MaxValue * 1.1));
                double x_Offset = resultGrid.ActualWidth / userStoryData.AllData.Count;

                for (int i = 0; i < userStoryData.AllData.Count; i++)
                {
                    Point point = new Point((x_Offset * i), resultGrid.ActualHeight - (yScale * userStoryData.AllData[i].Value));
                    dataPoints.Add(point);
                }
            }

            return dataPoints;
        }

        private void DrawProgressByDetectionResult(Point detectionPoint, AnomalyLastDetectResult detectionResult, double yScale)
        {
            double upperMarginOnUI = detectionResult.ExpectedValue + detectionResult.UpperMargin;
            double lowerMarginOnUI = detectionResult.ExpectedValue - detectionResult.LowerMargin;

            Point newUpperPoint = new Point(detectionPoint.X, resultGrid.ActualHeight - (yScale * (upperMarginOnUI < resultGrid.ActualHeight ? upperMarginOnUI : resultGrid.ActualHeight)));
            Point newLowerPoint = new Point(detectionPoint.X, resultGrid.ActualHeight - (yScale * (lowerMarginOnUI > 0 ? lowerMarginOnUI : 0)));

            Point actualPoint = detectionPoint;

            if (detectionResult.IsAnomaly)
            {
                SpriteVisual anomalyIndicator = GetNewAnomalyIndicator(detectionPoint);
                containerRoot.Children.InsertAtTop(anomalyIndicator);

                allAnomalyIndicators.Add(anomalyIndicator);
            }

            int endOfUpper = (progressPolyline.Points.Count / 2);
            int endOfLower = (progressPolyline.Points.Count / 2 + 1);

            progressPolyline.Points.Insert(endOfUpper, newUpperPoint);
            progressPolyline.Points.Insert(endOfLower, newLowerPoint);

            detectWindowPolyline.Points.Insert((detectWindowPolyline.Points.Count / 2), new Point(detectionPoint.X, 0));
            detectWindowPolyline.Points.Insert((detectWindowPolyline.Points.Count / 2 + 1), new Point(detectionPoint.X, resultGrid.ActualHeight));
            if ((detectWindowPolyline.Points.Count / 2) >= selectedDemoData.IndexOfFirstValidPoint)
            {
                detectWindowPolyline.Points.RemoveAt(0);
                detectWindowPolyline.Points.RemoveAt(detectWindowPolyline.Points.Count - 1);
            }

            progressIndicator.Offset = new Vector3((float)(detectionPoint.X), (float)(resultGrid.ActualHeight), resultGrid.CenterPoint.Z);

            progressLine.X1 = detectionPoint.X;
            progressLine.X2 = detectionPoint.X;
            progressLine.Y1 = 0;
            progressLine.Y2 = resultGrid.ActualHeight;
        }

        private float GetVolumeValue()
        {
            return maxVolumeInSampleBuffer * 100 + 50;
        }

        private int GetTimeOffsetInMinute(GranType granType)
        {
            switch (granType)
            {
                case GranType.hourly:
                    return 60;
                case GranType.minutely:
                    return 1;
                default:
                    return 1;
            }
        }

        private void ResetState()
        {
            anomalyEntireDetectResult = null;
            CleanAndRestoreUI();
            DisplayBasicData(selectedDemoData);

            if (ChangeDetectModeIsNotAllowed(selectedDemoData))
            {
                streamingOption.IsEnabled = false;
                batchOption.IsEnabled = false;
            }

            if (selectedDemoData.UserStory.StoryType == UserStoryType.Live)
            {
                this.StopLiveAudio?.Invoke(this, EventArgs.Empty);
            }
        }

        private void DisplayBasicData(AnomalyDetectorModelData demoData)
        {
            if (demoData == null)
            {
                return;
            }

            dataPolyline.Points?.Clear();

            // update data points
            if (demoData.UserStory.StoryType != UserStoryType.Live)
            {
                dataPolyline.Points = GetPointCollectionByUserStoryData(demoData);
            }
        }

        private void CleanAndRestoreUI()
        {
            if (progressIndicator != null)
            {
                progressIndicator.Offset = new Vector3(0, (int)resultGrid.ActualHeight, resultGrid.CenterPoint.Z);

                progressLine.X1 = progressIndicator.CenterPoint.X;
                progressLine.X2 = progressIndicator.CenterPoint.X;
                progressLine.Y1 = 0;
                progressLine.Y2 = resultGrid.ActualHeight;

                progressPolyline.Points?.Clear();
                detectWindowPolyline.Points?.Clear();

                if (allAnomalyIndicators != null && allAnomalyIndicators.Count > 0)
                {
                    foreach (SpriteVisual anomaly in allAnomalyIndicators)
                    {
                        if (containerRoot.Children.Contains(anomaly))
                        {
                            containerRoot.Children.Remove(anomaly);
                        }
                    }

                    allAnomalyIndicators.Clear();
                }
            }
        }

        private bool ChangeDetectModeIsNotAllowed(AnomalyDetectorModelData data)
        {
            return (data.UserStory.StoryType == UserStoryType.Live || data.UserStory.StoryType == UserStoryType.Manufacturing);
        }
    }
}
