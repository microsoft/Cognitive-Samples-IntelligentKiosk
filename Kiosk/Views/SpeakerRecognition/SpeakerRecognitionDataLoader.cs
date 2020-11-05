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

namespace IntelligentKioskSample.Views.SpeakerRecognition
{
    public class SpeakerRecognitionDataLoader
    {
        private static readonly string CustomSpeakerRecognitionModelsFileName = "SpeakerRecognition\\SpeakerRecognitionModels.json";

        public static async Task<List<SpeakerRecognitionViewModel>> GetCustomModelsAsync()
        {
            try
            {
                using (Stream stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(CustomSpeakerRecognitionModelsFileName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string content = await reader.ReadToEndAsync();
                        return JsonConvert.DeserializeObject<List<SpeakerRecognitionViewModel>>(content);
                    }
                }
            }
            catch (Exception)
            {
                return new List<SpeakerRecognitionViewModel>();
            }
        }

        public static async Task SaveCustomModelsToFileAsync(IEnumerable<SpeakerRecognitionViewModel> customModels)
        {
            string data = JsonConvert.SerializeObject(customModels, Formatting.Indented);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(CustomSpeakerRecognitionModelsFileName, CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(data);
                }
            }
        }
    }

    public class SpeakerRecognitionViewModel
    {
        public Guid Id { set; get; }
        public string Name { get; set; }
        public string IdentificationProfileId { get; set; }
        public bool IsPrebuiltModel { get; set; } = false;
    }
}
