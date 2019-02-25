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

using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Rest;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace IntelligentKioskSample
{
    internal static class Util
    {
        public static string CapitalizeString(string s)
        {
            return string.Join(" ", s.Split(' ').Select(word => !string.IsNullOrEmpty(word) ? char.ToUpper(word[0]) + word.Substring(1) : string.Empty));
        }

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
            if (visionException?.Body?.Message != null)
            {
                errorDetails = visionException.Body.Message;
            }

            HttpOperationException httpException = ex as HttpOperationException;
            if (httpException?.Response?.ReasonPhrase != null)
            {
                errorDetails = string.Format("{0}. The error message was \"{1}\".", ex.Message, httpException?.Response?.ReasonPhrase);
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

        public static async Task ConfirmActionAndExecute(string message, Func<Task> action,
            Func<Task> cancelAction = null, string confirmActionLabel = "Yes", string cancelActionLabel = "Cancel")
        {
            var messageDialog = new MessageDialog(message);

            messageDialog.Commands.Add(new UICommand(confirmActionLabel, new UICommandInvokedHandler(async (c) => await action())));

            if (cancelAction != null)
            {
                messageDialog.Commands.Add(new UICommand(cancelActionLabel, new UICommandInvokedHandler(async (c) => { await cancelAction(); })));
            }
            else
            {
                messageDialog.Commands.Add(new UICommand(cancelActionLabel, new UICommandInvokedHandler((c) => { })));
            }

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

        public static Microsoft.Azure.CognitiveServices.Vision.Face.Models.Gender? GetFaceGender(Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Gender? gender)
        {
            switch (gender)
            {
                case Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Gender.Male:
                    return Gender.Male;
                case Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Gender.Female:
                    return Gender.Female;
                default:
                    return null;
            }
        }

        public static bool ExtractFileFromZipArchive(StorageFile zipFile, string extractedFileName, StorageFile newFile)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFile.Path))
                {
                    ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e => e.FullName != null && e.FullName.Contains(extractedFileName, StringComparison.OrdinalIgnoreCase));
                    if (entry != null)
                    {
                        entry.ExtractToFile(newFile.Path, true);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        async private static Task CropBitmapAsync(Stream localFileStream, FaceRectangle rectangle, StorageFile resultFile)
        {
            //Get pixels of the crop region
            var pixels = await GetCroppedPixelsAsync(localFileStream.AsRandomAccessStream(), rectangle);

            // Save result to new image
            using (Stream resultStream = await resultFile.OpenStreamForWriteAsync())
            {
                IRandomAccessStream randomAccessStream = resultStream.AsRandomAccessStream();
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                        BitmapAlphaMode.Ignore,
                                        (uint)rectangle.Width, (uint)rectangle.Height,
                                        DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, pixels);

                await encoder.FlushAsync();
            }
        }

        async public static Task<ImageSource> DownloadAndCropBitmapAsync(string imageUrl, FaceRectangle rectangle)
        {
            byte[] imgBytes = await new System.Net.Http.HttpClient().GetByteArrayAsync(imageUrl);
            using (Stream stream = new MemoryStream(imgBytes))
            {
                return await GetCroppedBitmapAsync(stream.AsRandomAccessStream(), rectangle);
            }
        }

        async public static Task CropBitmapAsync(Func<Task<Stream>> localFile, FaceRectangle rectangle, StorageFile resultFile)
        {
            await CropBitmapAsync(await localFile(), rectangle, resultFile);
        }

        async public static Task<ImageSource> GetCroppedBitmapAsync(Func<Task<Stream>> originalImgFile, FaceRectangle rectangle)
        {
            try
            {
                using (IRandomAccessStream stream = (await originalImgFile()).AsRandomAccessStream())
                {
                    return await GetCroppedBitmapAsync(stream, rectangle);
                }
            }
            catch
            {
                // default to no image if we fail to crop the bitmap
                return null;
            }
        }

        async public static Task<ImageSource> GetCroppedBitmapAsync(IRandomAccessStream stream, FaceRectangle rectangle)
        {
            var pixels = await GetCroppedPixelsAsync(stream, rectangle);

            // Stream the bytes into a WriteableBitmap 
            WriteableBitmap cropBmp = new WriteableBitmap(rectangle.Width, rectangle.Height);
            cropBmp.FromByteArray(pixels);

            return cropBmp;
        }

        async private static Task<byte[]> GetCroppedPixelsAsync(IRandomAccessStream stream, FaceRectangle rectangle)
        {
            // Create a decoder from the stream. With the decoder, we can get  
            // the properties of the image. 
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // Create cropping BitmapTransform and define the bounds. 
            BitmapTransform transform = new BitmapTransform();
            BitmapBounds bounds = new BitmapBounds();
            bounds.X = (uint)rectangle.Left;
            bounds.Y = (uint)rectangle.Top;
            bounds.Height = (uint)rectangle.Height;
            bounds.Width = (uint)rectangle.Width;
            transform.Bounds = bounds;

            // Get the cropped pixels within the bounds of transform. 
            PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            return pix.DetachPixelData();
        }

        internal static async Task DownloadFileASync(string link, StorageFile destination, IProgress<DownloadOperation> progress, CancellationToken cancellationToken)
        {
            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(new Uri(link), destination);
            await download.StartAsync().AsTask(cancellationToken, progress);
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

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }

        internal static async Task<Stream> ResizePhoto(Stream photo, int height)
        {
            InMemoryRandomAccessStream result = new InMemoryRandomAccessStream();
            await ResizePhoto(photo, height, result);
            return result.AsStream();
        }

        internal static async Task<Tuple<double, double>> ResizePhoto(Stream photo, int height, StorageFile resultFile)
        {
            var resultStream = (await resultFile.OpenStreamForWriteAsync()).AsRandomAccessStream();
            var result = await ResizePhoto(photo, height, resultStream);
            resultStream.Dispose();

            return result;
        }

        private static async Task<Tuple<double, double>> ResizePhoto(Stream photo, int height, IRandomAccessStream resultStream)
        {
            WriteableBitmap wb = new WriteableBitmap(1, 1);
            wb = await wb.FromStream(photo.AsRandomAccessStream());

            int originalWidth = wb.PixelWidth;
            int originalHeight = wb.PixelHeight;

            if (wb.PixelHeight > height)
            {
                wb = wb.Resize((int)(((double)wb.PixelWidth / wb.PixelHeight) * height), height, WriteableBitmapExtensions.Interpolation.Bilinear);
            }

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, resultStream);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                    BitmapAlphaMode.Ignore,
                                    (uint)wb.PixelWidth, (uint)wb.PixelHeight,
                                    DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, wb.PixelBuffer.ToArray());

            await encoder.FlushAsync();

            return new Tuple<double, double>((double)originalWidth / wb.PixelWidth, (double)originalHeight / wb.PixelHeight);
        }
    }
}
