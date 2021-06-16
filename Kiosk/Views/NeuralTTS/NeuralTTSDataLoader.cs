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

namespace IntelligentKioskSample.Views.NeuralTTS
{
    public class NeuralTTSDataLoader
    {
        private const string NeuralVoicesFileName = "Views\\NeuralTTS\\NeuralVoices.json";

        public static List<VoiceInfo> GetNeuralVoices()
        {
            try
            {
                string content = File.ReadAllText(NeuralVoicesFileName);
                return JsonConvert.DeserializeObject<List<VoiceInfo>>(content);
            }
            catch (Exception)
            {
                return new List<VoiceInfo>();
            }
        }

        public static async Task<IList<CachedResult>> GetCachedResults(StorageFolder cacheDataFolder)
        {
            try
            {
                var cachedResultList = new List<CachedResult>();
                foreach (StorageFile item in (await cacheDataFolder.GetFilesAsync()).Where(f => f.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).OrderByDescending(f => f.DateCreated))
                {
                    using (var reader = new StreamReader((await item.OpenReadAsync()).AsStreamForRead()))
                    {
                        string json = await reader.ReadToEndAsync();
                        var cachedResult = JsonConvert.DeserializeObject<CachedResult>(json);
                        cachedResult.AudioFilePath = item.Path.Replace(".json", ".wav");
                        cachedResult.MetadataFilePath = item.Path;
                        cachedResultList.Add(cachedResult);
                    }
                }

                return cachedResultList;
            }
            catch (Exception)
            {
                return new List<CachedResult>();
            }
        }
    }

    public class VoiceInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Locale { get; set; }
    }

    public class CachedResult
    {
        public string Text { get; set; }
        public string VoiceId { get; set; }
        public string AudioFilePath { get; set; }
        public string MetadataFilePath { get; set; }
    }
}
