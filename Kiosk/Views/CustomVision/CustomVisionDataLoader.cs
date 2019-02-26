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

using IntelligentKioskSample.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace IntelligentKioskSample.Views.CustomVision
{
    public class CustomVisionDataLoader
    {
        private static readonly string RealtimeImageClassificationModelsFileName = "RealtimeImageClassification\\RealtimeImageClassificationModels.json";
        private static readonly string RealtimeObjectDetectionModelsFileName = "RealtimeObjectDetection\\RealtimeObjectDetectionModels.json";
        private static readonly string ClassificationOnnxModelStorageName = "RealtimeImageClassification\\ONNX";
        private static readonly string ObjectDetectionOnnxModelStorageName = "RealtimeObjectDetection\\ONNX";
        private static readonly string BuiltInClassificationModelsFileName = "Views\\CustomVision\\BuiltInClassificationModels.json";
        private static readonly string BuiltInObjectDetectionModelsFileName = "Views\\CustomVision\\BuiltInObjectDetectionModels.json";

        public static async Task<StorageFolder> GetOnnxModelStorageFolderAsync(CustomVisionProjectType customVisionProjectType)
        {
            string foldername = customVisionProjectType == CustomVisionProjectType.Classification ? ClassificationOnnxModelStorageName : ObjectDetectionOnnxModelStorageName;
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync(foldername, CreationCollisionOption.OpenIfExists);
        }

        public static List<CustomVisionModelData> GetBuiltInModelData(CustomVisionProjectType customVisionProjectType)
        {
            try
            {
                string fileName = customVisionProjectType == CustomVisionProjectType.Classification ? BuiltInClassificationModelsFileName : BuiltInObjectDetectionModelsFileName;
                string content = File.ReadAllText(fileName);
                return JsonConvert.DeserializeObject<List<CustomVisionModelData>>(content);
            }
            catch (Exception)
            {
                return new List<CustomVisionModelData>();
            }
        }

        public static async Task<List<CustomVisionModelData>> GetCustomVisionModelDataAsync(CustomVisionProjectType customVisionProjectType)
        {
            switch (customVisionProjectType)
            {
                case CustomVisionProjectType.Classification:
                    return await ReadCustomVisionModelDataFromFileAsync(RealtimeImageClassificationModelsFileName);
                case CustomVisionProjectType.ObjectDetection:
                    return await ReadCustomVisionModelDataFromFileAsync(RealtimeObjectDetectionModelsFileName);
                default:
                    return new List<CustomVisionModelData>();
            }
        }

        public static async Task SaveCustomVisionModelDataAsync(List<CustomVisionModelData> customVisionModelData, CustomVisionProjectType customVisionProjectType)
        {
            switch (customVisionProjectType)
            {
                case CustomVisionProjectType.Classification:
                    await SaveCustomVisionModelDataToFileAsync(customVisionModelData, RealtimeImageClassificationModelsFileName);
                    break;
                case CustomVisionProjectType.ObjectDetection:
                    await SaveCustomVisionModelDataToFileAsync(customVisionModelData, RealtimeObjectDetectionModelsFileName);
                    break;
            }
        }

        private static async Task SaveCustomVisionModelDataToFileAsync(List<CustomVisionModelData> customVisionModelData, string filename)
        {
            string data = JsonConvert.SerializeObject(customVisionModelData, Formatting.Indented);

            // save to file as the content is too big to be saved as a string-like setting
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(data);
                }
            }
        }

        private static async Task<List<CustomVisionModelData>> ReadCustomVisionModelDataFromFileAsync(string filename)
        {
            string content = string.Empty;
            try
            {
                using (Stream stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filename))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        content = await reader.ReadToEndAsync();
                        return JsonConvert.DeserializeObject<List<CustomVisionModelData>>(content);
                    }
                }
            }
            catch (Exception)
            {
                return new List<CustomVisionModelData>();
            }
        }
    }
}
