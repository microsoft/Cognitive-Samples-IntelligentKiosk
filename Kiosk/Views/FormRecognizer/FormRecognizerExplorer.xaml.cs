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

using IntelligentKioskSample.Controls.Overlays;
using Newtonsoft.Json;
using ServiceHelpers;
using ServiceHelpers.Models.FormRecognizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.FormRecognizer
{
    [KioskExperience(Id = "FormRecognizer",
        DisplayName = "Form Recognizer",
        Description = "Extract tables and key/value pairs from scanned forms",
        ImagePath = "ms-appx:/Assets/DemoGallery/Forms Recognizer.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business | ExperienceType.Preview,
        TechnologiesUsed = TechnologyType.FormRecognizer,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2019/07/22",
        DateUpdated = "2020/06/03",
        UpdatedDescription = "Updated to V2.0, including support for new fields in Receipt scenario")]
    public sealed partial class FormRecognizerExplorer : Page, INotifyPropertyChanged
    {
        private FormRecognizerService formRecognizerService;

        public static readonly string[] PdfExtensions = new string[] { ".pdf" };
        public static readonly string[] ImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<FormRecognizerViewModel> Models { get; set; } = new ObservableCollection<FormRecognizerViewModel>();

        public ObservableCollection<MultiPageResultViewModel> MultiPageResultCollection { get; set; } = new ObservableCollection<MultiPageResultViewModel>();

        private FormRecognizerViewModel currentFormModel;
        public FormRecognizerViewModel CurrentFormModel
        {
            get { return currentFormModel; }
            set
            {
                if (currentFormModel != value)
                {
                    currentFormModel = value;
                    NotifyPropertyChanged("CurrentFormModel");
                    CurrentFormModelChanged();
                }
            }
        }

        public bool isImageSource = true;
        public bool IsImageSource
        {
            get { return isImageSource; }
            set
            {
                if (isImageSource != value)
                {
                    isImageSource = value;
                    NotifyPropertyChanged("IsImageSource");
                }
            }
        }

        public bool isReceipt = true;
        public bool IsReceipt
        {
            get { return isReceipt; }
            set
            {
                if (isReceipt != value)
                {
                    isReceipt = value;
                    NotifyPropertyChanged(nameof(IsReceipt));
                }
            }
        }

        public FormRecognizerExplorer()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            bool enableFormRecognizerKeys = !string.IsNullOrEmpty(SettingsHelper.Instance.FormRecognizerApiKey) && !string.IsNullOrEmpty(SettingsHelper.Instance.FormRecognizerApiKeyEndpoint);
            this.newScenarioButton.IsEnabled = enableFormRecognizerKeys;
            if (enableFormRecognizerKeys)
            {
                this.formRecognizerService = new FormRecognizerService(SettingsHelper.Instance.FormRecognizerApiKey, SettingsHelper.Instance.FormRecognizerApiKeyEndpoint);
                await LoadModelsAsync();
            }
            else
            {
                await new MessageDialog("Missing Form Recognizer Key. Please enter the key in the Settings page.", "Missing API Key").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async Task LoadModelsAsync()
        {
            try
            {
                this.Models.Clear();
                this.Models.AddRange(FormRecognizerDataLoader.GetBuiltInModels());

                // get all model Ids by custom Form Recognizer API Key
                IList<Guid> allModelIdsBySubKey = (await this.formRecognizerService.GetCustomModelsAsync())?.ModelList.Select(x => x.ModelId).ToList() ?? new List<Guid>();

                // get stored models from local file by your custom Form Recognizer API Key
                List<FormRecognizerViewModel> allCustomModels = await FormRecognizerDataLoader.GetCustomModelsAsync();
                List<FormRecognizerViewModel> customModels = allCustomModels?.Where(x => allModelIdsBySubKey.Contains(x.Id)).ToList();

                if (customModels != null && customModels.Any())
                {
                    this.Models.AddRange(customModels);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading models");
            }
        }

        private void CurrentFormModelChanged()
        {
            if (CurrentFormModel != null)
            {
                this.initialViewControl.IsEnabled = false;
                this.suggestionSamplesListView.SelectedIndex = 0;
            }
        }

        private async void OnCameraImageCaptured(object sender, ImageAnalyzer img)
        {
            this.inputTypeFlyout.Hide();

            if (CurrentFormModel.IsReceiptModel)
            {
                await AnalyzeReceiptAsync(img);
            }
            else
            {
                StorageFile imageFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("FormImage.jpg", CreationCollisionOption.ReplaceExisting);
                if (img?.GetImageStreamCallback != null)
                {
                    await Util.SaveBitmapToStorageFileAsync(await img.GetImageStreamCallback(), imageFile);
                }

                await AnalyzeFormFromFileAsync(imageFile);
            }
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            this.inputTypeFlyout.Hide();

            if (CurrentFormModel.IsReceiptModel)
            {
                await AnalyzeReceiptAsync(args.First());
            }
        }

        private async Task AnalyzeLocalFileAsync()
        {
            if (CurrentFormModel.IsReceiptModel)
            {
                StorageFile localFile = await Util.PickSingleFileAsync(ImageExtensions);
                if (localFile != null)
                {
                    await AnalyzeReceiptAsync(new Uri(localFile.Path), localFile);
                }
            }
            else
            {
                string[] inputFormExtensions = ImageExtensions.Concat(PdfExtensions).ToArray();
                StorageFile localFile = await Util.PickSingleFileAsync(inputFormExtensions);
                if (localFile != null)
                {
                    await AnalyzeFormFromFileAsync(localFile);
                }
            }
        }

        private async Task AnalyzeReceiptAsync(ImageAnalyzer image)
        {
            await AnalyzeReceiptAsync(image, null, null);
        }

        private async Task AnalyzeReceiptAsync(Uri uri, StorageFile file = null)
        {
            await AnalyzeReceiptAsync(null, uri, file);
        }

        private async Task AnalyzeReceiptAsync(ImageAnalyzer image = null, Uri uri = null, StorageFile file = null)
        {
            try
            {
                IsReceipt = true;
                this.progressControl.IsActive = false;
                this.MultiPageResultCollection.Clear();
                this.OverlayPresenter.TokenInfo = null;
                this.notFoundGrid.Visibility = Visibility.Collapsed;

                //analyze and show receipt
                if (uri != null)
                {
                    await receiptView.AnalyzeReceiptAsync(uri, file);
                }
                else if (image != null)
                {
                    await receiptView.AnalyzeReceiptAsync(image);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Receipt Recognizer error.");
            }
        }

        private async Task AnalyzeFormFromFileAsync(StorageFile file)
        {
            if (file == null)
            {
                return;
            }

            try
            {
                IsReceipt = false;
                this.progressControl.IsActive = true;
                this.MultiPageResultCollection.Clear();
                this.OverlayPresenter.TokenInfo = null;
                this.notFoundGrid.Visibility = Visibility.Collapsed;

                StorageFile imageFile = await file?.CopyAsync(ApplicationData.Current.TemporaryFolder, $"{Guid.NewGuid().ToString()}{file.FileType}");
                if (imageFile != null)
                {
                    IsImageSource = ImageExtensions.Contains(imageFile.FileType);
                    if (IsImageSource)
                    {
                        this.OverlayPresenter.Source = new BitmapImage(new Uri(imageFile.Path));
                    }
                    else
                    {
                        this.pdfViewerControl.Source = new Uri(imageFile.Path);
                    }

                    AnalyzeFormResult formResult = null;
                    string fileType = IsImageSource ? FormRecognizerService.ImageJpegContentType : FormRecognizerService.PdfContentType;
                    using (FileStream stream = new FileStream(imageFile.Path, FileMode.Open))
                    {
                        formResult = await this.formRecognizerService.AnalyzeImageFormWithCustomModelAsync(CurrentFormModel.Id, stream, fileType);
                    }

                    ProcessFormResult(formResult);

                    imageFile?.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Form Recognizer error.");
            }
            finally
            {
                this.progressControl.IsActive = false;
            }
        }

        private void ProcessFormResult(AnalyzeFormResult result)
        {
            IList<PageResult> pages = result?.PageResults ?? new List<PageResult>();

            this.MultiPageResultCollection.Clear();
            foreach (var (page, index) in pages.Select((p, i) => (p, i)))
            {
                var keyValuePairList = page.KeyValuePairs
                    .Where(x => !string.IsNullOrEmpty(x.Key?.Text) && x.Key?.BoundingBox != null) // showing pairs with non-empty keys
                    .Select(x => new Tuple<List<TokenOverlayInfo>, List<TokenOverlayInfo>>(
                        new List<TokenOverlayInfo>() { new TokenOverlayInfo(x.Key) { IsMuted = true } },
                        new List<TokenOverlayInfo>() { new TokenOverlayInfo(x.Value) { IsMuted = true } }));

                var multiPage = new MultiPageResultViewModel()
                {
                    Title = $"Page {index + 1}",
                    ExtractedKeyValuePairCollection = new ObservableCollection<Tuple<List<TokenOverlayInfo>, List<TokenOverlayInfo>>>(keyValuePairList),
                    DataTableSourceCollection = new ObservableCollection<DataTable>(ListDataTableResult(page.Tables))
                };
                this.MultiPageResultCollection.Add(multiPage);
            }

            string jsonResult = result != null ? JsonConvert.SerializeObject(result, Formatting.Indented) : string.Empty;
            this.PreparePageResult(jsonResult);
        }

        private void PreparePageResult(string jsonResult)
        {
            if (this.MultiPageResultCollection.Any())
            {
                // highlight fields just in the image
                var mPage = this.MultiPageResultCollection.FirstOrDefault();
                if (IsImageSource && mPage != null)
                {
                    this.OverlayPresenter.TokenInfo = mPage.ExtractedKeyValuePairCollection.SelectMany(x => x.Item1.Concat(x.Item2)).ToList();
                }

                //add json page
                this.MultiPageResultCollection.Add(new MultiPageResultJson() { Title = "Json", Json = jsonResult });
            }
            else
            {
                this.notFoundGrid.Visibility = Visibility.Visible;
            }
        }

        private async void SuggestionSampleListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.suggestionSamplesListView.SelectedValue is Tuple<string, Uri> selectedSuggestionSample)
            {
                Uri fileUri = selectedSuggestionSample.Item2;
                bool isAppFile = fileUri.Scheme?.Equals("ms-appx", StringComparison.OrdinalIgnoreCase) ?? false;

                if (CurrentFormModel.IsReceiptModel)
                {
                    //analyze recipt
                    if (isAppFile)
                    {
                        StorageFile localFile = await StorageFile.GetFileFromApplicationUriAsync(fileUri);
                        await AnalyzeReceiptAsync(new Uri(localFile.Path), localFile);
                    }
                    else
                    {
                        await AnalyzeReceiptAsync(fileUri);
                    }
                }
                else
                {
                    //analyze form
                    await AnalyzeFormFromFileAsync(isAppFile ? await StorageFile.GetFileFromApplicationUriAsync(fileUri) : await StorageFile.GetFileFromPathAsync(fileUri.LocalPath));
                }
            }
        }

        private async void OnNewFormScenarioOpenButtonClicked(object sender, RoutedEventArgs e)
        {
            bool enableStorageKeys = !string.IsNullOrEmpty(FormRecognizerScenarioSetup.StorageAccount) && !string.IsNullOrEmpty(FormRecognizerScenarioSetup.StorageKey);
            if (enableStorageKeys)
            {
                this.formRecognizerScenarioSetupControl.OpenScenarioSetupForm(this.formRecognizerService);
            }
            else
            {
                await new MessageDialog("Please enter Storage Keys in the code behind of this demo.", "Missing Storage Keys").ShowAsync();
            }
        }

        private async void OnNewModelCreated(object sender, FormRecognizerViewModel e)
        {
            try
            {
                this.Models.Add(e);
                CurrentFormModel = this.Models.FirstOrDefault(m => m.Id == e.Id);

                // update local file with custom models
                await FormRecognizerDataLoader.SaveCustomModelsToFileAsync(this.Models.Where(x => !x.IsPrebuiltModel));
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure creating model");
            }
        }

        private async void OnDeleteModelClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete current scenario?", async () => { await DeleteModelAsync(); });
        }

        private async Task DeleteModelAsync()
        {
            try
            {
                if (CurrentFormModel?.Id != null && !CurrentFormModel.IsPrebuiltModel)
                {
                    Guid modelId = CurrentFormModel.Id;

                    // delete Form Recognizer model
                    await this.formRecognizerService.DeleteCustomModelAsync(modelId);

                    // update local file with custom models
                    this.Models.Remove(CurrentFormModel);
                    await FormRecognizerDataLoader.SaveCustomModelsToFileAsync(this.Models.Where(x => !x.IsPrebuiltModel));
                    await FormRecognizerDataLoader.DeleteModelStorageFolderAsync(modelId);

                    // re-select the first item from collection
                    CurrentFormModel = this.Models.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting model");
            }
        }

        private List<DataTable> ListDataTableResult(IList<TableResult> tables)
        {
            var tableList = new List<DataTable>();
            foreach (TableResult tableItem in tables)
            {
                var dataTable = new DataTable();
                var columnData = new List<string[]>();
                var columns = tableItem.Cells.GroupBy(c => c.ColumnIndex).ToDictionary(c => c.Key, c => c.ToList());

                // prepare columns
                foreach (var col in columns)
                {
                    var headerText = col.Value.FirstOrDefault(c => c.IsHeader)?.Text ?? string.Empty;
                    if (dataTable.Columns.Contains(headerText))
                    {
                        // we already have a column with this same header, so let's add an unique suffix
                        int countOfColumnsWithMatchingHeaderPrefix = dataTable.Columns.OfType<DataColumn>().Count(c => c.ColumnName.StartsWith(headerText + "("));
                        headerText = $"{headerText}({countOfColumnsWithMatchingHeaderPrefix + 1})";
                    }

                    dataTable.Columns.Add(headerText, typeof(string));

                    var columndArr = col.Value.Where(x => !x.IsHeader).Select(x => x.Text).ToArray();
                    columnData.Add(columndArr);
                }

                // prepare rows
                this.PrepareTableRows(dataTable, columnData);
                tableList.Add(dataTable);
            }

            return tableList;
        }

        private void PrepareTableRows(DataTable dataTable, List<string[]> columnData)
        {
            int columnCount = columnData.FirstOrDefault()?.Length ?? 0;
            for (int i = 0; i < columnCount; i++)
            {
                var rowItems = new List<string>();
                foreach (string[] col in columnData)
                {
                    rowItems.Add(col[i]);
                }

                dataTable.Rows.Add(rowItems.ToArray());
            }
        }

        private void OnInputTypeFlyoutOpened(object sender, object e)
        {
            this.flyoutScrollViewer.Width = this.inputTypeSelector.ActualWidth;
        }

        private async void OnInputTypeListViewItemTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            string option = ((ListViewItem)sender)?.Tag.ToString() ?? string.Empty;
            switch (option.ToLower())
            {
                case "file":
                    await AnalyzeLocalFileAsync();
                    break;
                case "camera":
                    FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
                    break;
                case "search":
                    FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
                    break;
            }

            this.suggestionSamplesListView.SelectedItem = null;
        }

        private async void OnCameraFlyoutOpened(object sender, object e)
        {
            await this.cameraControl.StartStreamAsync();
        }

        private async void OnCameraFlyoutClosed(object sender, object e)
        {
            await this.cameraControl.StopStreamAsync();
        }
    }

    public class MultiPageResultViewModel
    {
        public string Title { get; set; }
        public ObservableCollection<DataTable> DataTableSourceCollection { get; set; } = new ObservableCollection<DataTable>();
        public ObservableCollection<Tuple<List<TokenOverlayInfo>, List<TokenOverlayInfo>>> ExtractedKeyValuePairCollection { get; set; } = new ObservableCollection<Tuple<List<TokenOverlayInfo>, List<TokenOverlayInfo>>>();
    }

    public class MultiPageResultJson : MultiPageResultViewModel
    {
        public string Json { get; set; }
    }

    public class FileUriToFontGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Uri fileUri = (Uri)value;
            if (fileUri != null)
            {
                if (FormRecognizerExplorer.PdfExtensions.Any(x => fileUri.LocalPath.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return "\uEA90";
                }
                else if (FormRecognizerExplorer.ImageExtensions.Any(x => fileUri.LocalPath.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return "\uEB9F";
                }
            }
            return "\uE8A5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
