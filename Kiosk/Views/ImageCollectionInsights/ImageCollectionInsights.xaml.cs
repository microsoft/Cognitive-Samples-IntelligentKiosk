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

using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.ImageCollectionInsights
{
    [KioskExperience(Title = "Image Collection Insights", ImagePath = "ms-appx:/Assets/ImageCollectionInsights.jpg", ExperienceType = ExperienceType.Other)]
    public sealed partial class ImageCollectionInsights : Page
    {
        private StorageFolder currentRootFolder;

        public List<ImageInsightsViewModel> AllResults { get; set; } = new List<ImageInsightsViewModel>();
        public ObservableCollection<ImageInsightsViewModel> FilteredResults { get; set; } = new ObservableCollection<ImageInsightsViewModel>();
        public ObservableCollection<TagFilterViewModel> TagFilters { get; set; } = new ObservableCollection<TagFilterViewModel>();
        public ObservableCollection<FaceFilterViewModel> FaceFilters { get; set; } = new ObservableCollection<FaceFilterViewModel>();
        public ObservableCollection<EmotionFilterViewModel> EmotionFilters { get; set; } = new ObservableCollection<EmotionFilterViewModel>();

        public ImageCollectionInsights()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey) ||
                string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey))
            {
                await new MessageDialog("Missing Face or Vision API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async void ProcessImagesClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    await ProcessImagesAsync(folder);
                }

                this.currentRootFolder = folder;
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error picking the target folder.");
            }
        }

        private async void ReProcessImagesClicked(object sender, RoutedEventArgs e)
        {
            await this.ProcessImagesAsync(this.currentRootFolder, forceProcessing: true);
        }

        private async Task ProcessImagesAsync(StorageFolder rootFolder, bool forceProcessing = false)
        {
            this.progressRing.IsActive = true;

            this.landingMessage.Visibility = Visibility.Collapsed;
            this.filterTab.Visibility = Visibility.Visible;
            this.reprocessImagesButton.IsEnabled = true;

            this.FilteredResults.Clear();
            this.AllResults.Clear();
            this.TagFilters.Clear();
            this.EmotionFilters.Clear();
            this.FaceFilters.Clear();

            List<ImageInsights> insightsList = new List<ImageInsights>();

            if (!forceProcessing)
            {
                // see if we have pre-computed results and if so load it from the json file
                try
                {
                    StorageFile insightsResultFile = (await rootFolder.TryGetItemAsync("ImageInsights.json")) as StorageFile;
                    if (insightsResultFile != null)
                    {
                        using (StreamReader reader = new StreamReader(await insightsResultFile.OpenStreamForReadAsync()))
                        {
                            insightsList = JsonConvert.DeserializeObject<List<ImageInsights>>(await reader.ReadToEndAsync());
                            foreach (var insights in insightsList)
                            {
                                await AddImageInsightsToViewModel(rootFolder, insights);
                            }
                        }
                    }
                }
                catch
                {
                    // We will just compute everything again in case of errors
                }
            }

            if (!insightsList.Any())
            {
                // start with fresh face lists
                await FaceListManager.ResetFaceLists();

                // enumerate through the images and extract the insights 
                QueryOptions fileQueryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".png", ".jpg", ".bmp", ".jpeg", ".gif" });
                StorageFileQueryResult queryResult = rootFolder.CreateFileQueryWithOptions(fileQueryOptions);
                var queryFileList = this.limitProcessingToggleButton.IsChecked.Value ? await queryResult.GetFilesAsync(0, 50) : await queryResult.GetFilesAsync();

                foreach (var item in queryFileList)
                {
                    // Resize (if needed) in order to reduce network latency. Then store the result in a temporary file.
                    StorageFile resizedFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("ImageCollectionInsights.jpg", CreationCollisionOption.GenerateUniqueName);
                    var resizeTransform = await Util.ResizePhoto(await item.OpenStreamForReadAsync(), 720, resizedFile);

                    // Send the file for processing
                    ImageInsights insights = await ImageProcessor.ProcessImageAsync(resizedFile.OpenStreamForReadAsync, item.Name);

                    // Delete resized file
                    await resizedFile.DeleteAsync();

                    // Adjust all FaceInsights coordinates based on the transform function between the original and resized photos
                    foreach (var faceInsight in insights.FaceInsights)
                    {
                        faceInsight.FaceRectangle.Left = (int) (faceInsight.FaceRectangle.Left * resizeTransform.Item1);
                        faceInsight.FaceRectangle.Top = (int)(faceInsight.FaceRectangle.Top * resizeTransform.Item2);
                        faceInsight.FaceRectangle.Width = (int)(faceInsight.FaceRectangle.Width * resizeTransform.Item1);
                        faceInsight.FaceRectangle.Height = (int)(faceInsight.FaceRectangle.Height * resizeTransform.Item2);
                    }

                    insightsList.Add(insights);
                    await AddImageInsightsToViewModel(rootFolder, insights);
                }

                // save to json
                StorageFile jsonFile = await rootFolder.CreateFileAsync("ImageInsights.json", CreationCollisionOption.ReplaceExisting);
                using (StreamWriter writer = new StreamWriter(await jsonFile.OpenStreamForWriteAsync()))
                {
                    string jsonStr = JsonConvert.SerializeObject(insightsList, Formatting.Indented);
                    await writer.WriteAsync(jsonStr);
                }
            }

            List<TagFilterViewModel> tagsGroupedByCountAndSorted = new List<TagFilterViewModel>();
            foreach (var group in this.TagFilters.GroupBy(t => t.Count).OrderByDescending(g => g.Key))
            {
                tagsGroupedByCountAndSorted.AddRange(group.OrderBy(t => t.Tag));
            }
            this.TagFilters.Clear();
            this.TagFilters.AddRange(tagsGroupedByCountAndSorted);

            var sortedEmotions = this.EmotionFilters.OrderByDescending(e => e.Count).ToArray();
            this.EmotionFilters.Clear();
            this.EmotionFilters.AddRange(sortedEmotions);

            var sortedFaces = this.FaceFilters.OrderByDescending(f => f.Count).ToArray();
            this.FaceFilters.Clear();
            this.FaceFilters.AddRange(sortedFaces);

            this.progressRing.IsActive = false;
        }

        private async Task AddImageInsightsToViewModel(StorageFolder rootFolder, ImageInsights insights)
        {
            // Load image from file
            BitmapImage bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync((await (await rootFolder.GetFileAsync(insights.ImageId)).OpenStreamForReadAsync()).AsRandomAccessStream());
            bitmapImage.DecodePixelHeight = 360;

            // Create the view models
            ImageInsightsViewModel insightsViewModel = new ImageInsightsViewModel(insights, bitmapImage);
            this.AllResults.Add(insightsViewModel);
            this.FilteredResults.Add(insightsViewModel);

            foreach (var tag in insights.VisionInsights.Tags)
            {
                TagFilterViewModel tvm = this.TagFilters.FirstOrDefault(t => t.Tag == tag);
                if (tvm == null)
                {
                    tvm = new TagFilterViewModel(tag);
                    this.TagFilters.Add(tvm);
                }
                tvm.Count++;
            }

            foreach (var faceInsights in insights.FaceInsights)
            {
                FaceFilterViewModel fvm = this.FaceFilters.FirstOrDefault(f => f.FaceId == faceInsights.UniqueFaceId);
                if (fvm == null)
                {
                    StorageFile file = (await rootFolder.GetFileAsync(insights.ImageId));
                    ImageSource croppedFaced = await Util.GetCroppedBitmapAsync(
                        file.OpenStreamForReadAsync,
                        new FaceRectangle { Height = faceInsights.FaceRectangle.Height, Width = faceInsights.FaceRectangle.Width, Left = faceInsights.FaceRectangle.Left, Top = faceInsights.FaceRectangle.Top });

                    fvm = new FaceFilterViewModel(faceInsights.UniqueFaceId, croppedFaced);
                    this.FaceFilters.Add(fvm);
                }
                fvm.Count++;
            }

            var distinctEmotions = insights.FaceInsights.Select(f => f.TopEmotion).Distinct();
            foreach (var emotion in distinctEmotions)
            {
                EmotionFilterViewModel evm = this.EmotionFilters.FirstOrDefault(f => f.Emotion == emotion);
                if (evm == null)
                {
                    evm = new EmotionFilterViewModel(emotion);
                    this.EmotionFilters.Add(evm);
                }
                evm.Count++;
            }
        }

        private void ApplyFilters()
        {
            this.FilteredResults.Clear();

            var checkedTags = this.TagFilters.Where(t => t.IsChecked);
            var checkedFaces = this.FaceFilters.Where(f => f.IsChecked);
            var checkedEmotions = this.EmotionFilters.Where(e => e.IsChecked);
            if (checkedTags.Any() || checkedFaces.Any() || checkedEmotions.Any())
            {
                var fromTags = this.AllResults.Where(r => HasTag(checkedTags, r.Insights.VisionInsights.Tags));
                var fromFaces = this.AllResults.Where(r => HasFace(checkedFaces, r.Insights.FaceInsights));
                var fromEmotion = this.AllResults.Where(r => HasEmotion(checkedEmotions, r.Insights.FaceInsights));

                this.FilteredResults.AddRange((fromTags.Concat(fromFaces).Concat(fromEmotion)).Distinct());
            }
            else
            {
                this.FilteredResults.AddRange(this.AllResults);
            }
        }

        private bool HasFace(IEnumerable<FaceFilterViewModel> checkedFaces, FaceInsights[] faceInsights)
        {
            foreach (var item in checkedFaces)
            {
                if (faceInsights.Any(f => f.UniqueFaceId == item.FaceId))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasEmotion(IEnumerable<EmotionFilterViewModel> checkedEmotions, FaceInsights[] faceInsights)
        {
            foreach (var item in checkedEmotions)
            {
                if (faceInsights.Any(f => f.TopEmotion == item.Emotion))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasTag(IEnumerable<TagFilterViewModel> checkedTags, string[] tags)
        {
            foreach (var item in checkedTags)
            {
                if (tags.Any(t => t == item.Tag))
                {
                    return true;
                }
            }

            return false;
        }

        private void TagFilterChanged(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }

        private void FaceFilterChanged(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }

        private void EmotionFilterChanged(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }
    }

    public static class Extensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }

    }
}
