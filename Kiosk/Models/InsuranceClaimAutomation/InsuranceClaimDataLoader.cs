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
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace IntelligentKioskSample.Models.InsuranceClaimAutomation
{
    public class InsuranceClaimDataLoader
    {
        private static readonly string FolderName = "InsuranceClaimAutomation";
        private static readonly string ResultFileName = $"{FolderName}\\ResultData.json";

        public static async Task<List<DataGridViewModel>> GetDataAsync()
        {
            try
            {
                using (Stream stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(ResultFileName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string content = await reader.ReadToEndAsync();
                        return JsonConvert.DeserializeObject<List<DataGridViewModel>>(content) ?? new List<DataGridViewModel>();
                    }
                }
            }
            catch (Exception)
            {
                return new List<DataGridViewModel>();
            }
        }

        public static async Task DeleteDataAsync(IList<DataGridViewModel> data)
        {
            List<DataGridViewModel> scenarioList = await GetDataAsync();

            // remove scenarios
            foreach (DataGridViewModel item in data)
            {
                var itemToRemove = scenarioList.FirstOrDefault(s => s.Id == item.Id);
                bool modelEntryRemovedFromFile = scenarioList.Remove(itemToRemove);
                if (modelEntryRemovedFromFile)
                {
                    string folderName = $"{FolderName}\\{item.Id.ToString()}";
                    if ((await ApplicationData.Current.LocalFolder.TryGetItemAsync(folderName)) is StorageFolder storageFolder)
                    {
                        await storageFolder.DeleteAsync();
                    }
                }
            }

            // update scenario list file
            await SaveDataAsync(scenarioList);
        }

        public static async Task SaveOrUpdateDataAsync(DataGridViewModel item)
        {
            List<DataGridViewModel> data = await GetDataAsync();
            if (data != null)
            {
                // Update existing model, otherwise add a new one
                int index = data.FindIndex(x => x.Id == item.Id);
                if (index >= 0)
                {
                    data[index] = item;
                }
                else
                {
                    data.Add(item);
                }
                await SaveDataAsync(data);
            }
        }

        public static async Task<StorageFile> CopyFileToLocalFolderAsync(StorageFile file, string fileName, Guid itemId)
        {
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync($"{FolderName}\\{itemId.ToString()}", CreationCollisionOption.OpenIfExists);
            return await file?.CopyAsync(storageFolder, fileName, NameCollisionOption.GenerateUniqueName);
        }

        public static async Task<StorageFile> CreateFileInLocalFolderAsync(Guid itemId, string fileName)
        {
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync($"{FolderName}\\{itemId.ToString()}", CreationCollisionOption.OpenIfExists);
            return await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
        }

        private static async Task SaveDataAsync(List<DataGridViewModel> data)
        {
            string content = JsonConvert.SerializeObject(data, Formatting.Indented);

            // save to file as the content is too big to be saved as a string-like setting
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(ResultFileName, CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(content);
                }
            }
        }
    }
}
