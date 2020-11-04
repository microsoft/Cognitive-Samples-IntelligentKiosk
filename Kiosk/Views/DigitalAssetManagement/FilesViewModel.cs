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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace IntelligentKioskSample.Views.DigitalAssetManagement
{
    public class FilesViewModel
    {
        string _folderName = "DigitalAssetManagement";

        public ObservableCollection<FileViewModel> Files { get; } = new ObservableCollection<FileViewModel>();

        public async Task LoadFilesAsync()
        {
            //load file listing
            var folder = await GetFolder();
            var files = (await folder.GetFilesAsync()).OrderByDescending(i => i.DateCreated);
            Files.Clear();
            foreach (var file in files)
            {
                try
                {
                    Files.Add(new FileViewModel { File = file, Info = await GetFileInfo(file) });
                }
                catch { }
            }
        }

        public async Task SaveFileAsync(DigitalAssetData data)
        {
            //get existing file to replace, or new file
            var file = Files.Where(i => i.Info.Path == data.Info.Path).Select(i => i.File).FirstOrDefault() ?? await (await GetFolder()).CreateFileAsync($"{Guid.NewGuid()}.json", CreationCollisionOption.ReplaceExisting);

            //save file
            using (StreamWriter writer = new StreamWriter(await file.OpenStreamForWriteAsync()))
            {
                string jsonStr = JsonConvert.SerializeObject(data, Formatting.Indented);
                await writer.WriteAsync(jsonStr);
            }

            //reload files
            await LoadFilesAsync();
        }

        public async Task<DigitalAssetData> GetFileData(StorageFile file)
        {
            try
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        using (var json = new JsonTextReader(reader))
                        {
                            var serializer = new JsonSerializer();
                            return serializer.Deserialize<DigitalAssetData>(json);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task DeleteFileAsync(FileViewModel file)
        {
            await file.File.DeleteAsync();

            //refresh file list
            await LoadFilesAsync();
        }

        public async Task DownloadFileAsync(FileViewModel file)
        {
            //prompt for location to save
            try
            {
                var save = new FileSavePicker();
                save.SuggestedStartLocation = PickerLocationId.Downloads;
                save.SuggestedFileName = file.Info.Name;
                save.FileTypeChoices.Add("json", new List<string> { ".json" });
                var newFile = await save.PickSaveFileAsync();
                if (newFile != null)
                {
                    await file.File.CopyAndReplaceAsync(newFile);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error downloading file.");
            }
        }

        async Task<DigitalAssetInfo> GetFileInfo(StorageFile file)
        {
            try
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        using (var json = new JsonTextReader(reader))
                        {
                            var serializer = new JsonSerializer();
                            while (json.Read())
                            {
                                if (json.TokenType == JsonToken.PropertyName && (json.Value as string) == "Info")
                                {
                                    json.Read();
                                    if (json.TokenType == JsonToken.StartObject)
                                    {
                                        return serializer.Deserialize<DigitalAssetInfo>(json);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        async Task<StorageFolder> GetFolder()
        {
            if ((await ApplicationData.Current.LocalFolder.TryGetItemAsync(_folderName)) != null)
            {
                return await ApplicationData.Current.LocalFolder.GetFolderAsync(_folderName);
            }
            else
            {
                return await ApplicationData.Current.LocalFolder.CreateFolderAsync(_folderName);
            }
        }
    }

    public class FileViewModel
    {
        public StorageFile File { get; set; }
        public DigitalAssetInfo Info { get; set; }
    }

    public class DigitalAssetData
    {
        public DigitalAssetInfo Info { get; set; }
        public ImageInsights[] Insights { get; set; }
    }

    public class DigitalAssetInfo
    {
        public Uri Path { get; set; }
        public string Name { get; set; }
        public ImageProcessorServiceType Services { get; set; }
        public Guid[] CustomVisionProjects { get; set; }
        public int? FileLimit { get; set; }
        public int LastFileIndex { get; set; }
        public bool ReachedEndOfFiles { get; set; }
        public string Source { get; set; }
    }

    [Flags]
    public enum ImageProcessorServiceType
    {
        Face = 1,
        ComputerVision = 2,
        CustomVision = 4
    }
}
