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
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Graphics.Display;
using Windows.UI.Input.Inking;

namespace ServiceHelpers
{
    public class InkRecognizer
    {
        private string inkRecognitionUrl;
        private HttpClient httpClient;

        private IDictionary<uint, InkStroke> StrokeMap { get; set; }
        private string LanguageCode;

        public InkRecognizer(string subscriptionKey, string endpoint, string inkRecognitionUrl)
        {
            this.httpClient = new HttpClient() { BaseAddress = new Uri(endpoint) };
            this.httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            this.inkRecognitionUrl = inkRecognitionUrl;

            this.StrokeMap = new Dictionary<uint, InkStroke>();
            this.LanguageCode = "en-US";
        }

        public void AddStrokes(IReadOnlyList<InkStroke> strokes)
        {
            foreach (var stroke in strokes)
            {
                this.StrokeMap[stroke.Id] = stroke;
            }
        }

        public void ClearStrokes()
        {
            this.StrokeMap.Clear();
        }

        public void SetLanguage(string languageCode)
        {
            this.LanguageCode = languageCode;
        }

        public float GetDipsPerMm(float dpi)
        {
            float dipsPerMm = dpi / 25.4f;

            return dipsPerMm;
        }

        public JsonObject ConvertInkToJson()
        {
            // For demo purposes and keeping the initially loaded ink consistent a value of 96 for DPI was used
            // For production, it is most likely better to use the device's DPI when generating the request JSON and an example of that is below
            // var displayInformation = DisplayInformation.GetForCurrentView();
            // float dpi = displayInformation.LogicalDpi;
            // float dipsPerMm = GetDipsPerMm(dpi);

            float dipsPerMm = GetDipsPerMm(96);

            var payload = new JsonObject();
            var strokesArray = new JsonArray();

            foreach (InkStroke stroke in StrokeMap.Values)
            {
                var jStroke = new JsonObject();
                IReadOnlyList<InkPoint> pointsCollection = stroke.GetInkPoints();
                Matrix3x2 transform = stroke.PointTransform;

                jStroke["id"] = JsonValue.CreateNumberValue(stroke.Id);

                if (pointsCollection.Count >= 2)
                {
                    var points = new StringBuilder();
                    for (int i = 0; i < pointsCollection.Count; i++)
                    {
                        var transformedPoint = Vector2.Transform(new Vector2((float)pointsCollection[i].Position.X, (float)pointsCollection[i].Position.Y), transform);
                        double x = transformedPoint.X / dipsPerMm;
                        double y = transformedPoint.Y / dipsPerMm;
                        points.Append($"{x},{y}");
                        if (i != pointsCollection.Count - 1)
                        {
                            points.Append(",");
                        }
                    }

                    jStroke["points"] = JsonValue.CreateStringValue(points.ToString());
                    strokesArray.Add(jStroke);
                }
            }

            payload["version"] = JsonValue.CreateNumberValue(1.0);
            payload["language"] = JsonValue.CreateStringValue(this.LanguageCode);
            payload["strokes"] = strokesArray;

            return payload;
        }

        public async Task<HttpResponseMessage> RecognizeAsync(JsonObject json)
        {
            string payload = json.Stringify();
            var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await httpClient.PutAsync(inkRecognitionUrl, httpContent);

            return httpResponse;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
