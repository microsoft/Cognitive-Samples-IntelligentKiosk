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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace IntelligentKioskSample.Views.FormRecognizer
{
    public class FormRecognizerDataLoader
    {
        private static readonly string BuiltInFormRecognizerModelsFileName = "Views\\FormRecognizer\\BuiltInModels.json";
        private static readonly string CustomFormRecognizerModelsFileName = "FormRecognizer\\FormRecognizerModels.json";

        public static List<FormRecognizerViewModel> GetBuiltInModels()
        {
            try
            {
                var tmp = ApplicationData.Current.LocalFolder;
                string content = File.ReadAllText(BuiltInFormRecognizerModelsFileName);
                return JsonConvert.DeserializeObject<List<FormRecognizerViewModel>>(content);
            }
            catch (Exception)
            {
                return new List<FormRecognizerViewModel>();
            }
        }

        public static async Task<List<FormRecognizerViewModel>> GetCustomModelsAsync()
        {
            try
            {
                using (Stream stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(CustomFormRecognizerModelsFileName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string content = await reader.ReadToEndAsync();
                        return JsonConvert.DeserializeObject<List<FormRecognizerViewModel>>(content);
                    }
                }
            }
            catch (Exception)
            {
                return new List<FormRecognizerViewModel>();
            }
        }

        public static async Task SaveCustomModelsToFileAsync(IEnumerable<FormRecognizerViewModel> customModels)
        {
            string data = JsonConvert.SerializeObject(customModels, Formatting.Indented);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(CustomFormRecognizerModelsFileName, CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(data);
                }
            }
        }

        public static async Task DeleteModelStorageFolderAsync(Guid modelId)
        {
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync($"FormRecognizer\\{modelId}");
            if (storageFolder != null)
            {
                await storageFolder.DeleteAsync();
            }
        }

        public static async Task<StorageFile> CopyFileToLocalModelFolderAsync(StorageFile file, Guid modelId)
        {
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync($"FormRecognizer\\{modelId}", CreationCollisionOption.OpenIfExists);
            return await file?.CopyAsync(storageFolder, file.Name, NameCollisionOption.GenerateUniqueName);
        }
    }

    public class FormRecognizerViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Tuple<string, Uri>> SuggestionSamples { get; set; }
        public bool IsPrebuiltModel { get; set; }
        public bool IsReceiptModel { get; set; }
    }
}
