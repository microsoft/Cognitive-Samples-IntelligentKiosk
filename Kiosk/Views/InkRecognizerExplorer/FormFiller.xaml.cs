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
        // API key and endpoint information for ink recognition request
        string subscriptionKey = SettingsHelper.Instance.InkRecognizerApiKey;
        string endpoint = SettingsHelper.Instance.InkRecognizerApiKeyEndpoint;
        const string inkRecognitionUrl = "/inkrecognizer/v1.0-preview/recognize";

        private readonly DispatcherTimer dispatcherTimer;

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
            "mileage",
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
        
        public FormFiller()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            redoStacks = new Dictionary<string, Stack<InkStroke>>();
            clearedStrokesLists = new Dictionary<string, List<InkStroke>>();
            activeTool = ballpointPen;

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
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(350);
        }

        #region Event Handlers - Page
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.InkRecognizerApiKey))
            {
                await new MessageDialog("Missing Ink Recognizer API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }

            // When the page is Unloaded, InkRecognizer is disposed. To preserve the state of the page when navigating back to it, we need to re-instantiate the object.
            inkRecognizer = new ServiceHelpers.InkRecognizer(subscriptionKey, endpoint, inkRecognitionUrl);

            base.OnNavigatedTo(e);
        }

        void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Calling Dispose() on InkRecognizer to dispose of resources being used by HttpClient
            inkRecognizer.Dispose();
        }
        #endregion

        #region Event Handlers - Canvas, Timer, Form Field
        private void InkPresenter_StrokeInputStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            dispatcherTimer.Stop();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            var formField = this.FindName($"{prefix}") as Grid;
            if (formField.Tag.ToString() == "accepted")
            {
                formField.BorderBrush = new SolidColorBrush(Colors.Yellow);
                formField.Tag = "pending";
            }

            clearedStrokesLists[prefix].Clear();
            inkCleared = false;

            activeTool = inkToolbar.ActiveTool;
        }

        private void InkPresenter_StrokeInputEnded(InkStrokeInput sender, PointerEventArgs args)
        {
            dispatcherTimer.Start();
        }

        private void InkPresenter_StrokeErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            dispatcherTimer.Start();

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

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            dispatcherTimer.Stop();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            // Get strokes of the currently selected ink canvas and convert them to JSON for the request
            var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
            inkRecognizer.ClearStrokes();
            inkRecognizer.AddStrokes(strokes);
            JsonObject json = inkRecognizer.ConvertInkToJson();

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
                        result.Text += $"{recoUnit.recognizedText} ";
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

        private void FormField_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var formField = sender as Grid;
            string prefix = formField.Name;
            var canvasGrid = this.FindName($"{prefix}Grid") as Grid;

            if (canvasGrid.Visibility == Visibility.Collapsed)
            {
                CollapseAllFormFields();
                canvasGrid.Visibility = Visibility.Visible;

                if (formField.Tag.ToString() == "pending")
                {
                    formField.BorderBrush = new SolidColorBrush(Colors.Yellow);
                }

                var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;
                inkToolbar.TargetInkCanvas = canvas;
                currentCanvas = canvas;
            }
            else
            {
                if (formField.Tag.ToString() == "pending")
                {
                    formField.BorderBrush = new SolidColorBrush(Colors.White);
                }

                canvasGrid.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region Event Handlers - Canvas and Toolbar Butttons
        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            var formField = this.FindName($"{prefix}") as Grid;

            formField.BorderBrush = new SolidColorBrush(Colors.LightGreen);
            formField.Tag = "accepted";
            CollapseAllFormFields();
            NavigateToNextField(prefix);
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Start();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            var formField = this.FindName($"{prefix}") as Grid;
            if (formField.Tag.ToString() == "accepted")
            {
                formField.BorderBrush = new SolidColorBrush(Colors.Yellow);
                formField.Tag = "pending";
            }

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
            dispatcherTimer.Start();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            if (redoStacks[prefix].Count > 0)
            {
                var stroke = redoStacks[prefix].Pop();

                currentCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());

                var formField = this.FindName($"{prefix}") as Grid;
                if (formField.Tag.ToString() == "accepted")
                {
                    formField.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    formField.Tag = "pending";
                }
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            var formField = this.FindName($"{prefix}") as Grid;
            if (formField.Tag.ToString() == "accepted")
            {
                formField.BorderBrush = new SolidColorBrush(Colors.Yellow);
                formField.Tag = "pending";
            }

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
        private void CollapseAllFormFields()
        {
            foreach (string prefix in prefixes)
            {
                var formField = this.FindName($"{prefix}") as Grid;
                var canvasGrid = this.FindName($"{prefix}Grid") as Grid;

                canvasGrid.Visibility = Visibility.Collapsed;

                if (formField.Tag.ToString() == "pending")
                {
                    formField.BorderBrush = new SolidColorBrush(Colors.White);
                }
            }
        }

        private void NavigateToNextField(string prefix)
        {
            // Iterate form fields to find the field that was just "accepted" and find the first field after it that isn't "accepted"
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (prefixes[i] == prefix && i != (prefixes.Length - 1))
                {
                    string nextFieldPrefix = prefixes[i + 1];
                    var nextFormField = this.FindName($"{nextFieldPrefix}") as Grid;

                    if (nextFormField.Tag.ToString() == "pending")
                    {
                        ActivateFormField(nextFieldPrefix);
                    }
                    else if (nextFormField.Tag.ToString() == "accepted")
                    {
                        prefix = nextFieldPrefix;
                    }
                }
                else if (prefixes[i] == prefix && i == (prefixes.Length - 1))
                {
                    // If the last form field is "accepted" then find the first form field that it isn't from the beginning of the form and activate it
                    var nextFormField = this.FindName($"{prefix}") as Grid;
                    if (nextFormField.Tag.ToString() == "accepted")
                    {
                        foreach (string fieldPrefix in prefixes)
                        {
                            var formField = this.FindName($"{fieldPrefix}") as Grid;
                            if (formField.Tag.ToString() == "pending")
                            {
                                ActivateFormField(fieldPrefix);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void ActivateFormField(string prefix)
        {
            var formField = this.FindName($"{prefix}") as Grid;
            var canvasGrid = this.FindName($"{prefix}Grid") as Grid;
            var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;

            formField.BorderBrush = new SolidColorBrush(Colors.Yellow);
            canvasGrid.Visibility = Visibility.Visible;
            inkToolbar.TargetInkCanvas = canvas;
            currentCanvas = canvas;
        }
        #endregion
    }
}
