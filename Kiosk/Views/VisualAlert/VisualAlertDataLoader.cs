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

namespace IntelligentKioskSample.Views.VisualAlert
{
    public class VisualAlertDataLoader
    {
        private static readonly string ClassificationOnnxModelStorageName = "VisualAlert\\ONNX";
        private static readonly string ClassificationModelsFileName = "VisualAlert\\ImageClassificationModels.json";

        public static async Task<StorageFolder> GetOnnxModelStorageFolderAsync()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync(ClassificationOnnxModelStorageName, CreationCollisionOption.OpenIfExists);
        }

        public static async Task<List<VisualAlertScenarioData>> GetScenarioCollectionAsync()
        {
            try
            {
                using (Stream stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(ClassificationModelsFileName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string content = await reader.ReadToEndAsync();
                        return JsonConvert.DeserializeObject<List<VisualAlertScenarioData>>(content) ?? new List<VisualAlertScenarioData>();
                    }
                }
            }
            catch (Exception)
            {
                return new List<VisualAlertScenarioData>();
            }
        }

        public static async Task StoreScenarioAsync(VisualAlertScenarioData scenario)
        {
            List<VisualAlertScenarioData> scenarioList = await GetScenarioCollectionAsync();

            VisualAlertScenarioData scenarioWithSameName = scenarioList.FirstOrDefault(x => string.Equals(x.Name, scenario.Name));
            if (scenarioWithSameName != null)
            {
                string titleMessage = $"There is already a “{scenarioWithSameName.Name}” model in this device. Select “Replace” if you would like to replace it, or “Keep Both” if you would like to keep both.";
                await Util.ConfirmActionAndExecute(titleMessage,
                    async () =>
                    {
                        // if user select Yes, we replace the model with the same name
                        bool modelEntryRemovedFromFile = scenarioList.Remove(scenarioWithSameName);
                        StorageFolder onnxProjectDataFolder = await GetOnnxModelStorageFolderAsync();
                        StorageFile modelFileToRemove = await onnxProjectDataFolder.GetFileAsync(scenarioWithSameName.FileName);
                        if (modelEntryRemovedFromFile && modelFileToRemove != null)
                        {
                            await modelFileToRemove.DeleteAsync();
                        }
                        await SaveOrUpdateCustomVisionModelAsync(scenarioList, scenario);
                    },

                    cancelAction: async () =>
                    {
                        int maxNumberOfModelWithSameName = scenarioList
                            .Where(x => x.Name != null && x.Name.StartsWith(scenario.Name, StringComparison.OrdinalIgnoreCase))
                            .Select(x =>
                            {
                                string modelNumberInString = x.Name.Split('_').LastOrDefault();
                                int.TryParse(modelNumberInString, out int number);
                                return number;
                            })
                            .Max();

                        // if user select Cancel we just save the new model with the same name
                        scenario.Name = $"{scenario.Name}_{maxNumberOfModelWithSameName + 1}";
                        await SaveOrUpdateCustomVisionModelAsync(scenarioList, scenario);
                    },

                    confirmActionLabel: "Replace",
                    cancelActionLabel: "Keep Both");
            }
            else
            {
                await SaveOrUpdateCustomVisionModelAsync(scenarioList, scenario);
            }
        }

        public static async Task DeleteScenariosAsync(IList<VisualAlertScenarioData> scenarios)
        {
            StorageFolder onnxProjectDataFolder = await GetOnnxModelStorageFolderAsync();
            List<VisualAlertScenarioData> scenarioList = await GetScenarioCollectionAsync();

            // remove scenarios
            foreach (VisualAlertScenarioData scenario in scenarios)
            {
                var itemToRemove = scenarioList.FirstOrDefault(s => s.Id == scenario.Id);
                bool modelEntryRemovedFromFile = scenarioList.Remove(itemToRemove);

                StorageFile modelFileToRemove = !string.IsNullOrEmpty(scenario.FileName) ? await onnxProjectDataFolder.GetFileAsync(scenario.FileName) : null;
                if (modelEntryRemovedFromFile && modelFileToRemove != null)
                {
                    await modelFileToRemove.DeleteAsync();
                }
            }

            // update scenario list file
            await SaveCustomVisionModelDataAsync(scenarioList);
        }

        private static async Task SaveOrUpdateCustomVisionModelAsync(List<VisualAlertScenarioData> modelList, VisualAlertScenarioData modelData)
        {
            if (modelList != null)
            {
                // Update existing model, otherwise add a new one
                int index = modelList.FindIndex(x => x.Id == modelData.Id);
                if (index >= 0)
                {
                    modelList[index] = modelData;
                }
                else
                {
                    modelList.Add(modelData);
                }
                await SaveCustomVisionModelDataAsync(modelList);
            }
        }

        private static async Task SaveCustomVisionModelDataAsync(List<VisualAlertScenarioData> data)
        {
            string content = JsonConvert.SerializeObject(data, Formatting.Indented);

            // save to file as the content is too big to be saved as a string-like setting
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(ClassificationModelsFileName, CreationCollisionOption.ReplaceExisting);
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
