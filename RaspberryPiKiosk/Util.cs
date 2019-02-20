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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Rest;

namespace IntelligentKioskSample
{
    internal static class Util
    {
        internal static async Task GenericApiCallExceptionHandler(Exception ex, string errorTitle)
        {
            string errorDetails = GetMessageFromException(ex);

            await new MessageDialog(errorDetails, errorTitle).ShowAsync();
        }

        internal static string GetMessageFromException(Exception ex)
        {
            string errorDetails = ex.Message;

            APIErrorException faceApiException = ex as APIErrorException;
            if (faceApiException?.Message != null)
            {
                errorDetails = faceApiException.Message;
            }

            var visionException = ex as Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ComputerVisionErrorException;
            if (!string.IsNullOrEmpty(visionException?.Body?.Message))
            {
                errorDetails = visionException.Body.Message;
            }

            HttpOperationException httpException = ex as HttpOperationException;
            if (httpException?.Response?.ReasonPhrase != null)
            {
                string errorReason = $"\"{httpException.Response.ReasonPhrase}\".";
                if (httpException?.Response?.Content != null)
                {
                    errorReason += $" Some more details: {httpException.Response.Content}";
                }

                errorDetails = $"{ex.Message}. The error was {errorReason}.";
            }

            return errorDetails;
        }

        internal static DetectedFace FindFaceClosestToRegion(IEnumerable<DetectedFace> faces, BitmapBounds region)
        {
            return faces?.Where(f => Util.AreFacesPotentiallyTheSame(region, f.FaceRectangle))
                                  .OrderBy(f => Math.Abs(region.X - f.FaceRectangle.Left) + Math.Abs(region.Y - f.FaceRectangle.Top)).FirstOrDefault();
        }

        internal static bool AreFacesPotentiallyTheSame(BitmapBounds face1, FaceRectangle face2)
        {
            return CoreUtil.AreFacesPotentiallyTheSame((int)face1.X, (int)face1.Y, (int)face1.Width, (int)face1.Height, face2.Left, face2.Top, face2.Width, face2.Height);
        }

        public static async Task ConfirmActionAndExecute(string message, Func<Task> action)
        {
            var messageDialog = new MessageDialog(message);

            messageDialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (c) => await action())));
            messageDialog.Commands.Add(new UICommand("Cancel", new UICommandInvokedHandler((c) => { })));

            messageDialog.DefaultCommandIndex = 1;
            messageDialog.CancelCommandIndex = 1;

            await messageDialog.ShowAsync();
        }

        public static async Task<IEnumerable<string>> GetAvailableCameraNamesAsync()
        {
            DeviceInformationCollection deviceInfo = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return deviceInfo.OrderBy(d => d.Name).Select(d => d.Name);
        }

        public static KeyValuePair<string, double>[] EmotionToRankedList(Emotion emotion)
        {
            return new KeyValuePair<string, double>[]
            {
                new KeyValuePair<string, double>("Anger", emotion.Anger),
                new KeyValuePair<string, double>("Contempt", emotion.Contempt),
                new KeyValuePair<string, double>("Disgust", emotion.Disgust),
                new KeyValuePair<string, double>("Fear", emotion.Fear),
                new KeyValuePair<string, double>("Happiness", emotion.Happiness),
                new KeyValuePair<string, double>("Neutral", emotion.Neutral),
                new KeyValuePair<string, double>("Sadness", emotion.Sadness),
                new KeyValuePair<string, double>("Surprise", emotion.Surprise)
            }
            .OrderByDescending(e => e.Value)
            .ToArray();
        }

        internal static async Task<byte[]> GetPixelBytesFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
		{
			using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
			{
				BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
				encoder.SetSoftwareBitmap(softwareBitmap);
				await encoder.FlushAsync();

				// Read the pixel bytes from the memory stream
				using (var reader = new DataReader(stream.GetInputStreamAt(0)))
				{
					var bytes = new byte[stream.Size];
					await reader.LoadAsync((uint)stream.Size);
					reader.ReadBytes(bytes);
					return bytes;
				}
			}
		}
    }
}
