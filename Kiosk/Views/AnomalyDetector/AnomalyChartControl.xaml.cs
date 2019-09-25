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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Views.AnomalyDetector
{
    public sealed partial class AnomalyChartControl : UserControl
    {
        private static readonly int MaxVolumeValue = 120;
        private static readonly int TooltipBottomMargin = 15;
        private static readonly int BaseVolume = 50;
        private static readonly int DefaultDurationOfLiveDemoInSecond = 480;

        private static readonly string StopDetectButton = "Stop Processing";
        private static readonly string StartDetectButton = "Detect Anomalies";
        private static readonly string ShortDateFormat = "MM/dd";
        private static readonly string ShortDateWithTimeFormat = "MM/dd HH:mm";

        private Visual xamlRoot;
        private Compositor compositor;
        private ContainerVisual containerRoot;
        private SpriteVisual progressIndicator;

        private bool isProcessing = false;
        private bool shouldStopCurrentRun = false;
        private float maxVolumeInSampleBuffer = 0;

        private AnomalyDetectionScenario curScenario;
        private AnomalyEntireDetectResult anomalyEntireDetectResult;
        private List<Tuple<SpriteVisual, AnomalyInfo>> allAnomalyIndicators = new List<Tuple<SpriteVisual, AnomalyInfo>>();

        public AnomalyDetectorServiceType SelectedDetectionMode { get; private set; }

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

        public event EventHandler StartLiveAudio;
        public event EventHandler StopLiveAudio;

        public AnomalyChartControl()
        {
            this.InitializeComponent();
            PrepareUIElements();
        }

        public void InitializeChart(AnomalyDetectionScenarioType scenarioType, AnomalyDetectorServiceType detectionMode, double sensitivy)
        {
            if (AnomalyDetectorScenarioLoader.AllModelData.ContainsKey(scenarioType))
            {
                curScenario = AnomalyDetectorScenarioLoader.AllModelData[scenarioType];
                if (scenarioType == AnomalyDetectionScenarioType.Live)
                {
                    curScenario.AllData.Clear();
                }

                DisplayBasicData(curScenario);

                // set default value: slider and radio buttons
                this.sensitivitySlider.Value = sensitivy;
                SelectedDetectionMode = detectionMode;
            }
        }

        public void SetVolumeValue(float value)
        {
            maxVolumeInSampleBuffer = value;
        }

        private void OnChartGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (curScenario == null)
            {
                return;
            }

            ResetState();
            DisplayBasicData(curScenario);
            shouldStopCurrentRun = true;

            // update anomaly data
            if (allAnomalyIndicators != null && allAnomalyIndicators.Count > 0)
            {
                if (curScenario.ScenarioType == AnomalyDetectionScenarioType.Live)
                {
                    ClearAnomalyPoints();
                }
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
            progressIndicator.Brush = compositor.CreateColorBrush(Color.FromArgb(179, 0, 120, 215));

            containerRoot.Children.InsertAtTop(progressIndicator);
            progressIndicator.Offset = new Vector3(0, (float)resultGrid.ActualHeight, resultGrid.CenterPoint.Z);

            progressLine.X1 = progressIndicator.CenterPoint.X;
            progressLine.X2 = progressIndicator.CenterPoint.X;

            // pointer moved event for tooltip
            resultGrid.PointerMoved += OnChartPointer;
            detectWindowPolyline.PointerMoved += OnChartPointer;
        }

        private void OnChartPointer(object sender, PointerRoutedEventArgs e)
        {
            Point point = e.GetCurrentPoint(resultGrid).Position;
            var anomaly = allAnomalyIndicators.FirstOrDefault(x => Util.IsPointInsideVisualElement(x.Item1, point) &&
                (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse || e.Pointer.IsInContact)); // test for touch exit event);
            if (anomaly != null)
            {
                Visual visualElement = anomaly.Item1;
                AnomalyInfo anomalyInfo = anomaly.Item2;

                double popupWidth = this.tooltipPopup.ActualWidth;
                double popupHeight = this.tooltipPopup.ActualHeight;

                Point absolutePoint = e.GetCurrentPoint(Window.Current.Content).Position;
                double pageWidth = ((AppShell)Window.Current.Content)?.ActualWidth ?? 0;

                double xOffset = absolutePoint.X + popupWidth / 2.0 >= pageWidth ? visualElement.Offset.X - popupWidth : visualElement.Offset.X - popupWidth / 2.0;
                double yOffset = visualElement.Offset.Y - popupHeight - TooltipBottomMargin;

                // set tooltip offset
                this.tooltipPopup.HorizontalOffset = xOffset;
                this.tooltipPopup.VerticalOffset = yOffset;

                // set tooltip data
                this.timestampTextBlock.Text = anomalyInfo.Text;
                this.delayValueTextBlock.Text = anomalyInfo.Value;
                this.expectedTextBlock.Text = anomalyInfo.ExpectedValue;

                // show tooltip
                this.tooltipPopup.IsOpen = true;
            }
            else
            {
                // hide tooltip
                this.tooltipPopup.IsOpen = false;
            }
        }

        private async void OnStartDetectionButtonClicked(object sender, RoutedEventArgs e)
        {
            if (curScenario == null)
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
                    switch (curScenario.ScenarioType)
                    {
                        case AnomalyDetectionScenarioType.Live:
                            this.StartLiveAudio?.Invoke(this, EventArgs.Empty);
                            await StartLiveDemoProcessAsync();
                            break;

                        default:
                            await StartStreamingModeProcessAsync();
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
                double yScale = resultGrid.ActualHeight / MaxVolumeValue;
                double xOffset = resultGrid.ActualWidth / DefaultDurationOfLiveDemoInSecond;

                DateTime currentTime = DateTime.Now;
                DateTime startTime = currentTime.AddMinutes((double)DefaultDurationOfLiveDemoInSecond * -1);

                progressLine.X1 = progressIndicator.CenterPoint.X;
                progressLine.X2 = progressIndicator.CenterPoint.X;

                dataPolyline.Points.Clear();

                int startIndex = AnomalyDetectionScenario.DefaultRequiredPoints;
                for (int i = 0; i < startIndex; i++)
                {
                    float volume = GetCurrentVolumeValue();

                    curScenario.AllData.Insert(i, new TimeSeriesData(startTime.ToString(), volume));
                    startTime = startTime.AddMinutes(AnomalyDetectorScenarioLoader.GetTimeOffsetInMinute(curScenario.Granularity));

                    double yOffset = yScale * (curScenario.AllData[i].Value - BaseVolume);
                    Point point = new Point(xOffset * i, resultGrid.ActualHeight - yOffset);
                    dataPolyline.Points.Add(point);

                    Point newUpperPoint = new Point(dataPolyline.Points[i].X, 0);
                    Point newLowerPoint = new Point(dataPolyline.Points[i].X, resultGrid.ActualHeight);

                    int endOfUpper = detectWindowPolyline.Points.Count / 2;
                    int endOfLower = detectWindowPolyline.Points.Count / 2 + 1;

                    detectWindowPolyline.Points.Insert(endOfUpper, newUpperPoint);
                    detectWindowPolyline.Points.Insert(endOfLower, newLowerPoint);
                }

                for (int i = startIndex; i < DefaultDurationOfLiveDemoInSecond; i++)
                {
                    if (shouldStopCurrentRun)
                    {
                        break;
                    }

                    float volume = GetCurrentVolumeValue();

                    curScenario.AllData.Insert(i, new TimeSeriesData(startTime.ToString(), volume));
                    startTime = startTime.AddMinutes(AnomalyDetectorScenarioLoader.GetTimeOffsetInMinute(curScenario.Granularity));

                    double yOffset = yScale * (curScenario.AllData[i].Value - BaseVolume);
                    Point point = new Point(xOffset * i, resultGrid.ActualHeight - yOffset);
                    dataPolyline.Points.Add(point);

                    AnomalyLastDetectResult result = await GetLiveDemoAnomalyDetectionResultAsync(i);
                    if (result != null)
                    {
                        result.ExpectedValue -= BaseVolume;
                        AnomalyInfo anomalyInfo = new AnomalyInfo
                        {
                            Text = i.ToString(),
                            Value = (volume - BaseVolume).ToString("F2"),
                            ExpectedValue = result.ExpectedValue.ToString("F2")
                        };
                        DrawProgressByDetectionResult(dataPolyline.Points[i], result, anomalyInfo, yScale);
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

                string timestampFormat = curScenario.ScenarioType == AnomalyDetectionScenarioType.Telecom ? ShortDateFormat : ShortDateWithTimeFormat;
                double dataRange = curScenario.MaxValue - curScenario.MinValue;
                double yScale = (resultGrid.ActualHeight / dataRange);
                double yZeroLine = yScale * curScenario.MinValue;

                for (int i = 0; i < dataPolyline.Points.Count; i++)
                {
                    progressIndicator.Offset = new Vector3(float.Parse(dataPolyline.Points[i].X.ToString()), float.Parse(resultGrid.ActualHeight.ToString()), resultGrid.CenterPoint.Z);

                    progressLine.X1 = dataPolyline.Points[i].X;
                    progressLine.X2 = dataPolyline.Points[i].X;

                    if (anomalyEntireDetectResult != null)
                    {
                        Point newUpperPoint = new Point(dataPolyline.Points[i].X, yZeroLine + resultGrid.ActualHeight - (yScale * (anomalyEntireDetectResult.ExpectedValues[i] + anomalyEntireDetectResult.UpperMargins[i])));
                        Point newLowerPoint = new Point(dataPolyline.Points[i].X, yZeroLine + resultGrid.ActualHeight - (yScale * (anomalyEntireDetectResult.ExpectedValues[i] - anomalyEntireDetectResult.LowerMargins[i])));

                        if (anomalyEntireDetectResult.IsAnomaly[i])
                        {
                            SpriteVisual anomalyIndicator = GetNewAnomalyIndicator(dataPolyline.Points[i]);
                            TimeSeriesData timeSeriesData = curScenario.AllData[i];
                            AnomalyInfo anomalyInfo = new AnomalyInfo
                            {
                                Text = Util.StringToDateFormat(timeSeriesData.Timestamp, timestampFormat),
                                Value = timeSeriesData.Value.ToString("F2"),
                                ExpectedValue = anomalyEntireDetectResult.ExpectedValues[i].ToString("F2")
                            };

                            containerRoot.Children.InsertAtTop(anomalyIndicator);
                            allAnomalyIndicators.Add(new Tuple<SpriteVisual, AnomalyInfo>(anomalyIndicator, anomalyInfo));
                        }

                        int endOfUpper = progressPolyline.Points.Count / 2;
                        int endOfLower = progressPolyline.Points.Count / 2 + 1;

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
                if (dataPolyline.Points != null)
                {
                    dataPolyline.Points.Clear();
                }
                else
                {
                    dataPolyline.Points = new PointCollection();
                }

                double dataRange = curScenario.MaxValue - curScenario.MinValue;
                double yScale = resultGrid.ActualHeight / dataRange;
                double xOffset = resultGrid.ActualWidth / (curScenario.AllData.Count - 1);
                double yZeroLine = yScale * curScenario.MinValue;

                string timestampFormat = curScenario.ScenarioType == AnomalyDetectionScenarioType.Telecom ? ShortDateFormat : ShortDateWithTimeFormat;
                int startIndex = curScenario.MinIndexOfRequiredPoints;

                for (int i = 0; i < startIndex; i++)
                {
                    Point point = new Point(xOffset * i, yZeroLine + resultGrid.ActualHeight - (yScale * curScenario.AllData[i].Value));
                    dataPolyline.Points.Add(point);

                    Point newUpperPoint = new Point(dataPolyline.Points[i].X, 0);
                    Point newLowerPoint = new Point(dataPolyline.Points[i].X, resultGrid.ActualHeight);

                    int endOfUpper = detectWindowPolyline.Points.Count / 2;
                    int endOfLower = detectWindowPolyline.Points.Count / 2 + 1;

                    detectWindowPolyline.Points.Insert(endOfUpper, newUpperPoint);
                    detectWindowPolyline.Points.Insert(endOfLower, newLowerPoint);
                }

                for (int i = startIndex; i < curScenario.AllData.Count; i++)
                {
                    if (shouldStopCurrentRun)
                    {
                        break;
                    }

                    AnomalyLastDetectResult result = await GetStreamingAnomalyDetectionResultAsync(i);

                    Point point = new Point(xOffset * i, yZeroLine + resultGrid.ActualHeight - (yScale * curScenario.AllData[i].Value));
                    dataPolyline.Points.Add(point);

                    if (result != null)
                    {
                        TimeSeriesData timeSeriesData = curScenario.AllData[i];
                        AnomalyInfo anomalyInfo = new AnomalyInfo
                        {
                            Text = Util.StringToDateFormat(timeSeriesData.Timestamp, timestampFormat),
                            Value = timeSeriesData.Value.ToString("F2"),
                            ExpectedValue = result.ExpectedValue.ToString("F2")
                        };

                        DrawProgressByDetectionResult(dataPolyline.Points[i], result, anomalyInfo, yScale, yZeroLine);
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure during streaming detection.");
            }
        }

        private async Task<AnomalyLastDetectResult> GetLiveDemoAnomalyDetectionResultAsync(int dataPointIndex)
        {
            int requiredPoints = Math.Min(AnomalyDetectionScenario.DefaultRequiredPoints, curScenario.MinIndexOfRequiredPoints);

            if (dataPointIndex >= requiredPoints)
            {
                AnomalyDetectionRequest dataRequest = new AnomalyDetectionRequest
                {
                    Sensitivity = (int)sensitivitySlider.Value,
                    MaxAnomalyRatio = curScenario.MaxAnomalyRatio,
                    Granularity = curScenario.Granularity.ToString(),
                    CustomInterval = curScenario.CustomInterval,
                    Period = curScenario.Period,
                    Series = curScenario.AllData.GetRange(dataPointIndex - requiredPoints, (requiredPoints + 1))
                };

                return await AnomalyDetectorHelper.GetStreamingDetectionResult(dataRequest);
            }

            return null;
        }

        private async Task<AnomalyLastDetectResult> GetStreamingAnomalyDetectionResultAsync(int dataPointIndex)
        {
            if (dataPointIndex >= curScenario.MinIndexOfRequiredPoints)
            {
                AnomalyDetectionRequest dataRequest = new AnomalyDetectionRequest
                {
                    Sensitivity = (int)sensitivitySlider.Value,
                    MaxAnomalyRatio = curScenario.MaxAnomalyRatio,
                    Granularity = curScenario.Granularity.ToString(),
                    CustomInterval = curScenario.CustomInterval,
                    Period = curScenario.Period,
                    Series = curScenario.AllData.GetRange(dataPointIndex - curScenario.MinIndexOfRequiredPoints, (curScenario.MinIndexOfRequiredPoints + 1))
                };

                return await AnomalyDetectorHelper.GetStreamingDetectionResult(dataRequest);
            }

            return null;
        }

        private async Task<AnomalyEntireDetectResult> GetBatchAnomalyDetectionResultAsync()
        {
            AnomalyDetectionRequest dataRequest = new AnomalyDetectionRequest
            {
                Sensitivity = (int)sensitivitySlider.Value,
                MaxAnomalyRatio = curScenario.MaxAnomalyRatio,
                Granularity = curScenario.Granularity.ToString(),
                CustomInterval = curScenario.CustomInterval,
                Period = curScenario.Period,
                Series = curScenario.AllData
            };
            return await AnomalyDetectorHelper.GetBatchDetectionResult(dataRequest);
        }

        private SpriteVisual GetNewAnomalyIndicator(Point anomalyPoint)
        {
            SpriteVisual anomaly;
            anomaly = compositor.CreateSpriteVisual();
            anomaly.Brush = compositor.CreateColorBrush(Colors.Yellow);
            anomaly.Size = new Vector2(10, 10);
            anomaly.Offset = new Vector3((float)(anomalyPoint.X), (float)(anomalyPoint.Y), resultGrid.CenterPoint.Z);

            anomaly.AnchorPoint = new Vector2()
            {
                X = 0.5f,
                Y = anomaly.Offset.Y + anomaly.Size.Y > resultGrid.ActualHeight ? 1f :
                    anomaly.Offset.Y - anomaly.Size.Y < 0 ? 0f : 0.5f
            };
            
            return anomaly;
        }

        private void SetYAxisLabels(double max, double min, AnomalyDetectionScenarioType scenarioType)
        {
            string format = "F2";
            if (scenarioType == AnomalyDetectionScenarioType.Live)
            {
                min = 0;
                max = MaxVolumeValue;
                format = "F0";
            }

            double step = (max - min) / 4;
            this.y1_Lbl.Text = min.ToString(format);
            this.y2_Lbl.Text = (min + step).ToString(format);
            this.y3_Lbl.Text = (min + 2 * step).ToString(format);
            this.y4_Lbl.Text = (min + 3 * step).ToString(format);
            this.y5_Lbl.Text = max.ToString(format);
        }

        private void SetXAxisLabels(AnomalyDetectionScenario scenario)
        {
            if (scenario != null)
            {
                int step = 0;
                List<TimeSeriesData> data = scenario.AllData;

                switch (scenario.ScenarioType)
                {
                    case AnomalyDetectionScenarioType.Live:
                        step = DefaultDurationOfLiveDemoInSecond / 4;

                        this.x1_Lbl.Text = $"{0 * step}";
                        this.x2_Lbl.Text = $"{1 * step}";
                        this.x3_Lbl.Text = $"{2 * step}";
                        this.x4_Lbl.Text = $"{3 * step}";
                        this.x5_Lbl.Text = $"{4 * step}";
                        break;

                    default:
                        if (data.Any())
                        {
                            step = data.Count % 4 != 0 ? (int)Math.Floor(data.Count / 4.0) : data.Count / 4;

                            this.x1_Lbl.Text = Util.StringToDateFormat(data[0 * step].Timestamp, ShortDateFormat);
                            this.x2_Lbl.Text = Util.StringToDateFormat(data[1 * step].Timestamp, ShortDateFormat);
                            this.x3_Lbl.Text = Util.StringToDateFormat(data[2 * step].Timestamp, ShortDateFormat);
                            this.x4_Lbl.Text = Util.StringToDateFormat(data[3 * step].Timestamp, ShortDateFormat);
                            this.x5_Lbl.Text = Util.StringToDateFormat(data[data.Count - 1].Timestamp, ShortDateFormat);
                        }
                        break;
                }
            }
        }

        private void DrawProgressByDetectionResult(Point detectionPoint, AnomalyLastDetectResult detectionResult, AnomalyInfo anomalyInfo, double yScale, double yZeroLine = 0)
        {
            double upperMarginOnUI = detectionResult.ExpectedValue + detectionResult.UpperMargin;
            double lowerMarginOnUI = detectionResult.ExpectedValue - detectionResult.LowerMargin;
            double offsetY1 = yScale * upperMarginOnUI;
            double offsetY2 = lowerMarginOnUI > 0 ? yScale * lowerMarginOnUI : 0;
            int indexOfFirstValidPoint = curScenario.MinIndexOfRequiredPoints;

            Point newUpperPoint = new Point(detectionPoint.X, (yZeroLine + resultGrid.ActualHeight - offsetY1) > 0 ? (yZeroLine + resultGrid.ActualHeight - offsetY1) : 0);
            Point newLowerPoint = new Point(detectionPoint.X, yZeroLine + resultGrid.ActualHeight - offsetY2);

            if (detectionResult.IsAnomaly)
            {
                SpriteVisual anomalyIndicator = GetNewAnomalyIndicator(detectionPoint);
                containerRoot.Children.InsertAtTop(anomalyIndicator);
                allAnomalyIndicators.Add(new Tuple<SpriteVisual, AnomalyInfo>(anomalyIndicator, anomalyInfo));
            }

            int endOfUpper = progressPolyline.Points.Count / 2;
            int endOfLower = progressPolyline.Points.Count / 2 + 1;

            progressPolyline.Points.Insert(endOfUpper, newUpperPoint);
            progressPolyline.Points.Insert(endOfLower, newLowerPoint);

            detectWindowPolyline.Points.Insert((detectWindowPolyline.Points.Count / 2), new Point(detectionPoint.X, 0));
            detectWindowPolyline.Points.Insert((detectWindowPolyline.Points.Count / 2 + 1), new Point(detectionPoint.X, resultGrid.ActualHeight));
            if ((detectWindowPolyline.Points.Count / 2) >= indexOfFirstValidPoint)
            {
                detectWindowPolyline.Points.RemoveAt(0);
                detectWindowPolyline.Points.RemoveAt(detectWindowPolyline.Points.Count - 1);
            }

            progressIndicator.Offset = new Vector3((float)(detectionPoint.X), (float)(resultGrid.ActualHeight), resultGrid.CenterPoint.Z);

            progressLine.X1 = detectionPoint.X;
            progressLine.X2 = detectionPoint.X;
        }

        private float GetCurrentVolumeValue()
        {
            return maxVolumeInSampleBuffer * 100 + BaseVolume;
        }

        public void ResetState()
        {
            shouldStopCurrentRun = true;
            anomalyEntireDetectResult = null;
            this.StopLiveAudio?.Invoke(this, EventArgs.Empty);

            ClearAnomalyPoints();
        }

        private void DisplayBasicData(AnomalyDetectionScenario scenario)
        {
            // update axis lines
            SetXAxisLabels(scenario);
            SetYAxisLabels(scenario.MaxValue, scenario.MinValue, scenario.ScenarioType);

            // update progress line
            if (progressIndicator != null)
            {
                progressIndicator.Offset = new Vector3(0, (int)resultGrid.ActualHeight, resultGrid.CenterPoint.Z);
                progressLine.X1 = progressIndicator.CenterPoint.X;
                progressLine.X2 = progressIndicator.CenterPoint.X;
                progressLine.Y1 = 0;
                progressLine.Y2 = resultGrid.ActualHeight;
            }

            // update data points
            dataPolyline.Points?.Clear();
        }

        private PointCollection GetPointCollectionByScenarioData(AnomalyDetectionScenario scenario)
        {
            PointCollection dataPoints = new PointCollection();

            if (scenario.AllData != null && scenario.AllData.Any())
            {
                double dataRange = scenario.MaxValue - scenario.MinValue;
                double yScale = (resultGrid.ActualHeight / dataRange);
                double xOffset = resultGrid.ActualWidth / (scenario.AllData.Count - 1);
                double yZeroLine = yScale * scenario.MinValue;

                for (int i = 0; i < scenario.AllData.Count; i++)
                {
                    Point point = new Point(xOffset * i, yZeroLine + resultGrid.ActualHeight - (yScale * scenario.AllData[i].Value));
                    dataPoints.Add(point);
                }
            }

            return dataPoints;
        }

        private void ClearAnomalyPoints()
        {
            progressPolyline.Points?.Clear();
            detectWindowPolyline.Points?.Clear();

            if (allAnomalyIndicators != null && allAnomalyIndicators.Count > 0)
            {
                foreach (SpriteVisual anomaly in allAnomalyIndicators.Select(x => x.Item1))
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
}
