// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: 
// http://www.microsoft.com/cognitive
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

using IntelligentKioskSample.Models.InkRecognizerExplorer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.InkRecognizerExplorer
{
    public sealed partial class FormFiller : Page
    {
        private readonly DispatcherTimer inkRecoTimer;
        private readonly DispatcherTimer textToggleTimer;

        private string subscriptionKey = SettingsHelper.Instance.InkRecognizerApiKey;
        ServiceHelpers.InkRecognizer inkRecognizer;
        InkResponse inkResponse;
        InkCanvas currentCanvas;

        Dictionary<string, Stack<InkStroke>> redoStacks;
        Dictionary<string, List<InkStroke>> clearedStrokesLists;
        InkToolbarToolButton activeTool;
        bool inkCleared = false;

        // Each form field and their contents have a "prefix" associated with them in their names to allow easy targeting of those elements
        private string[] prefixes = new string[]
        {
            "year",
            "make",
            "model",
            "license",
            "date",
            "time",
            "damage"
        };

        private Symbol TouchWriting = (Symbol)0xED5F;
        private Symbol Accept = (Symbol)0xE8FB;
        private Symbol Undo = (Symbol)0xE7A7;
        private Symbol Redo = (Symbol)0xE7A6;
        private Symbol ClearAll = (Symbol)0xE74D;
        private Symbol Car = (Symbol)0xE804;

        public FormFiller()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            inkRecognizer = new ServiceHelpers.InkRecognizer(subscriptionKey);

            redoStacks = new Dictionary<string, Stack<InkStroke>>();
            clearedStrokesLists = new Dictionary<string, List<InkStroke>>();
            currentCanvas = yearCanvas;
            inkToolbar.TargetInkCanvas = currentCanvas;
            activeTool = ballpointPen;

            // Set default ink color to blue
            ballpointPen.SelectedBrushIndex = 16;
            pencil.SelectedBrushIndex = 16;

            // Add event handlers and create redo stacks for each form field's ink canvases
            foreach (string prefix in prefixes)
            {
                var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;
                canvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
                canvas.InkPresenter.StrokeInput.StrokeStarted += InkPresenter_StrokeInputStarted;
                canvas.InkPresenter.StrokeInput.StrokeEnded += InkPresenter_StrokeInputEnded;
                canvas.InkPresenter.StrokesErased += InkPresenter_StrokeErased;

                redoStacks.Add(prefix, new Stack<InkStroke>());
                clearedStrokesLists.Add(prefix, new List<InkStroke>());
            }

            // Timer created for ink recognition to happen after a set time period once a stroke ends
            inkRecoTimer = new DispatcherTimer();
            inkRecoTimer.Tick += InkRecoTimer_Tick;
            inkRecoTimer.Interval = TimeSpan.FromMilliseconds(350);

            // Timer created to switch from ink to text when user is idle in form field after a set time
            textToggleTimer = new DispatcherTimer();
            textToggleTimer.Tick += TextToggleTimer_Tick;
            textToggleTimer.Interval = TimeSpan.FromSeconds(3);
        }

        #region Event Handlers - Canvas, Timer, Form Field
        private void InkPresenter_StrokeInputStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            inkRecoTimer.Stop();
            textToggleTimer.Stop();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            clearedStrokesLists[prefix].Clear();
            inkCleared = false;

            activeTool = inkToolbar.ActiveTool;
        }

        private void InkPresenter_StrokeInputEnded(InkStrokeInput sender, PointerEventArgs args)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();
        }

        private void InkPresenter_StrokeErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            foreach (var stroke in args.Strokes)
            {
                redoStacks[prefix].Push(stroke);
            }

            var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count == 0)
            {
                var result = this.FindName($"{prefix}Result") as TextBlock;
                result.Text = string.Empty;
            }
        }

        private async void InkRecoTimer_Tick(object sender, object e)
        {
            inkRecoTimer.Stop();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            // Get strokes of the currently selected ink canvas and convert them to JSON for the request
            var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
            inkRecognizer.ClearStrokes();
            inkRecognizer.AddStrokes(strokes);
            JObject json = inkRecognizer.ConvertInkToJson();

            // Recognize the strokes of the current ink canvas and convert the response JSON into an InkResponse
            var response = await inkRecognizer.RecognizeAsync(json);
            string responseString = await response.Content.ReadAsStringAsync();
            inkResponse = JsonConvert.DeserializeObject<InkResponse>(responseString);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = this.FindName($"{prefix}Result") as TextBlock;
                result.Text = string.Empty;

                foreach (var recoUnit in inkResponse.RecognitionUnits)
                {
                    if (recoUnit.category == "line")
                    {
                        if (prefix == "damage")
                        {
                            result.Text += $"{recoUnit.recognizedText}\n";
                        }
                        else
                        {
                            result.Text += $"{recoUnit.recognizedText} ";
                        }
                    }
                }
            }
            else
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound)
                {
                    await new MessageDialog("Access denied due to invalid subscription key or wrong API endpoint. Make sure to provide a valid key for an active subscription and use a correct API endpoint in the Settings page.", $"Response Code: {inkResponse.Error.code}").ShowAsync();
                }
                else
                {
                    await new MessageDialog(inkResponse.Error.message, $"Response Code: {inkResponse.Error.code}").ShowAsync();
                }
            }
        }

        private void TextToggleTimer_Tick(object sender, object e)
        {
            textToggleTimer.Stop();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            ToggleFormFieldText(prefix);
        }

        private void FormField_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var formField = sender as Grid;
            string canvasPrefix = formField.Name;

            var canvas = this.FindName($"{canvasPrefix}Canvas") as InkCanvas;
            inkToolbar.TargetInkCanvas = canvas;
            currentCanvas = canvas;

            foreach (string prefix in prefixes)
            {
                ToggleFormFieldText(prefix);
            }

            ToggleFormFieldCanvas(canvasPrefix);
        }
        #endregion

        #region Event Handlers - Canvas and Toolbar Butttons
        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            textToggleTimer.Stop();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            ToggleFormFieldText(prefix);
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            if (inkCleared)
            {
                foreach (var stroke in clearedStrokesLists[prefix])
                {
                    currentCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
                }

                clearedStrokesLists[prefix].Clear();
                inkCleared = false;
            }
            else if (activeTool is InkToolbarEraserButton)
            {
                RedoButton_Click(null, null);
            }
            else
            {
                var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
                if (strokes.Count > 0)
                {
                    var stroke = strokes[strokes.Count - 1];

                    redoStacks[prefix].Push(stroke);

                    stroke.Selected = true;
                    currentCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                }
            }
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            if (redoStacks[prefix].Count > 0)
            {
                var stroke = redoStacks[prefix].Pop();

                currentCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                clearedStrokesLists[prefix].Add(stroke);
            }

            inkCleared = true;
            currentCanvas.InkPresenter.StrokeContainer.Clear();

            var result = this.FindName($"{prefix}Result") as TextBlock;
            result.Text = string.Empty;
        }

        private void TouchButton_Click(object sender, RoutedEventArgs e)
        {
            if (touchButton.IsChecked == true)
            {
                foreach (string prefix in prefixes)
                {
                    var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;
                    canvas.InkPresenter.InputDeviceTypes |= CoreInputDeviceTypes.Touch;
                }
            }
            else
            {
                foreach (string prefix in prefixes)
                {
                    var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;
                    canvas.InkPresenter.InputDeviceTypes &= ~CoreInputDeviceTypes.Touch;
                }
            }
        }
        #endregion

        #region Helpers
        private void ToggleFormFieldText(string prefix)
        {
            var canvasGrid = this.FindName($"{prefix}CanvasGrid") as Grid;
            canvasGrid.Visibility = Visibility.Collapsed;

            var textResult = this.FindName($"{prefix}Result") as TextBlock;
            textResult.Visibility = Visibility.Visible;

            var formfield = this.FindName(prefix) as Grid;
            var color = GetColorFromHex("#1F1F1F");
            formfield.Background = new SolidColorBrush(color);
        }

        private void ToggleFormFieldCanvas(string prefix)
        {
            var textResult = this.FindName($"{prefix}Result") as TextBlock;
            textResult.Visibility = Visibility.Collapsed;

            var canvasGrid = this.FindName($"{prefix}CanvasGrid") as Grid;
            canvasGrid.Visibility = Visibility.Visible;

            var formfield = this.FindName(prefix) as Grid;
            var color = GetColorFromHex("#2B2B2B");
            formfield.Background = new SolidColorBrush(color);
        }

        private Color GetColorFromHex(string hexCode)
        {
            byte a = 255;
            byte r = Convert.ToByte(hexCode.Substring(1, 2), 16);
            byte g = Convert.ToByte(hexCode.Substring(3, 2), 16);
            byte b = Convert.ToByte(hexCode.Substring(5, 2), 16);

            return Color.FromArgb(a, r, g, b);
        }
        #endregion
    }
}
