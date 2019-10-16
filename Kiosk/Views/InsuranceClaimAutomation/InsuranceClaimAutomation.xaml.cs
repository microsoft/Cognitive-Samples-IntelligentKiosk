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

using IntelligentKioskSample.Models.InsuranceClaimAutomation;
using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.InsuranceClaimAutomation
{
    [KioskExperience(Title = "Insurance Claim Automation", ImagePath = "ms-appx:/Assets/InsuranceClaimAutomation.jpg", ExperienceType = ExperienceType.Kiosk)]
    public sealed partial class InsuranceClaimAutomation : Page, INotifyPropertyChanged
    {
        private static readonly string FormRecognizerApiKey = "";              // AZURE FORM RECOGNIZER SUBSCRIPTION KEY;
        private static readonly string FormRecognizerEndpoint = "";            // AZURE FORM RECOGNIZER SUBSCRIPTION ENPOINT
        private static readonly Guid FormRecognizerModelId = Guid.Empty;       // FORM RECOGNIZER MODEL ID
        private static readonly Guid ObjectDetectionModelId = Guid.Empty;      // CUSTOM VISION OBJECT DETECTION MODEL ID
        private static readonly Guid ObjectClassificationModelId = Guid.Empty; // CUSTOM VISION CLASSIFICATION MODEL ID

        private static readonly string DataGridAction = "DataGridAction";
        private static readonly TokenOverlayInfo emptyRow = new TokenOverlayInfo() { Text = "---" };

        private static readonly double MinProbability = 0.6;
        private static readonly string ObjectName = "windshield";
        private static readonly Dictionary<CustomFormFieldType, string> CustomFormFieldsDict = new Dictionary<CustomFormFieldType, string>()
        {
            { CustomFormFieldType.CustomName,     "name" },
            { CustomFormFieldType.Date,           "date" },
            { CustomFormFieldType.WarrantyId,     "warranty claim id" },
            { CustomFormFieldType.WarrantyAmount, "manufacturer warranty" },
            { CustomFormFieldType.Total,          "total" }
        };

        private int claimId = 0;
        private StorageFolder TempStorageFolder;
        private StorageFile CurrentInputFormFile;
        private ImageAnalyzer CurrentInputProductImage;

        private CustomVisionTrainingClient trainingApi;
        private CustomVisionPredictionClient predictionApi;
        private FormRecognizerService formRecognizerService;

        public event PropertyChangedEventHandler PropertyChanged;

        private InputViewState inputImageViewState = InputViewState.NotSelected;
        public InputViewState InputImageViewState
        {
            get { return inputImageViewState; }
            set
            {
                inputImageViewState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InputImageViewState"));
                ValidateInputParams();
            }
        }

        private InputViewState inputFormViewState = InputViewState.NotSelected;
        public InputViewState InputFormViewState
        {
            get { return inputFormViewState; }
            set
            {
                inputFormViewState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InputFormViewState"));
                ValidateInputParams();
            }
        }

        public bool isFormImageSource = true;
        public bool IsFormImageSource
        {
            get { return isFormImageSource; }
            set
            {
                if (isFormImageSource != value)
                {
                    isFormImageSource = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsFormImageSource"));
                }
            }
        }

        public ObservableCollection<InputSampleViewModel> ImageSampleCollection { get; set; } = new ObservableCollection<InputSampleViewModel>()
        {
            new InputSampleViewModel("https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedphotos/1.jpg"),
            new InputSampleViewModel("https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedphotos/2.jpg"),
            new InputSampleViewModel("https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedphotos/3.jpg"),
            new InputSampleViewModel("https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedphotos/4.jpg")
        };

        public ObservableCollection<InputSampleViewModel> FormSampleCollection { get; set; } = new ObservableCollection<InputSampleViewModel>()
        {
            new InputSampleViewModel("Form1.jpg", "https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedforms/Form1.jpg"),
            new InputSampleViewModel("Form2.jpg", "https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedforms/Form2.jpg"),
            new InputSampleViewModel("Form3.jpg", "https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedforms/Form3.jpg"),
            new InputSampleViewModel("Form4.jpg", "https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedforms/Form4.jpg"),
            new InputSampleViewModel("Form5.jpg", "https://intelligentkioskstore.blob.core.windows.net/process-automation/suggestedforms/Form5.jpg")
        };

        public ObservableCollection<DataGridViewModel> DataGridCollection { get; set; } = new ObservableCollection<DataGridViewModel>();

        public InsuranceClaimAutomation()
        {
            this.InitializeComponent();
            DataContext = this;

            this.detailsViewControl.OnViewClosed += OnDetailsViewClosed;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            bool enableFormRecognizerKeys = !string.IsNullOrEmpty(FormRecognizerApiKey) && !string.IsNullOrEmpty(FormRecognizerEndpoint);
            bool enableCustomVisionKeys = !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey) && !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionPredictionApiKey);

            if (enableFormRecognizerKeys)
            {
                TempStorageFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("InsuranceClaimAutomation", CreationCollisionOption.OpenIfExists);
                this.formRecognizerService = new FormRecognizerService(FormRecognizerApiKey, FormRecognizerEndpoint);
            }

            if (enableCustomVisionKeys)
            {
                trainingApi = new CustomVisionTrainingClient { Endpoint = SettingsHelper.Instance.CustomVisionTrainingApiKeyEndpoint, ApiKey = SettingsHelper.Instance.CustomVisionTrainingApiKey };
                predictionApi = new CustomVisionPredictionClient { Endpoint = SettingsHelper.Instance.CustomVisionPredictionApiKeyEndpoint, ApiKey = SettingsHelper.Instance.CustomVisionPredictionApiKey };
            }

            if (!enableFormRecognizerKeys || !enableCustomVisionKeys)
            {
                this.mainPage.IsEnabled = false;
                await new MessageDialog("Please enter Custom Vision / Form Recognizer API Keys in the code behind of this demo.", "Missing API Keys").ShowAsync();
            }
            else
            {
                this.mainPage.IsEnabled = true;
                await LoadDataGridCollectionAsync();

                this.claimId = DataGridCollection.OrderByDescending(x => x.ClaimId).Select(x => x.ClaimId).FirstOrDefault();
            }

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (TempStorageFolder != null)
            {
                await TempStorageFolder.DeleteAsync();
            }
            base.OnNavigatedFrom(e);
        }

        private async Task LoadDataGridCollectionAsync()
        {
            try
            {
                this.progressRing.IsActive = true;

                IList<DataGridViewModel> data = await InsuranceClaimDataLoader.GetDataAsync();
                foreach (var item in data)
                {
                    if (!string.IsNullOrEmpty(item.ProductImageUri))
                    {
                        item.ProductImage = new BitmapImage(new Uri(item.ProductImageUri));
                    }
                    if (!string.IsNullOrEmpty(item.FormImageUri))
                    {
                        item.FormImage = new BitmapImage(new Uri(item.FormImageUri));
                    }
                }
                DataGridCollection.Clear();
                DataGridCollection.AddRange(data);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading grid data");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private void AddImageButtonClicked(object sender, RoutedEventArgs e)
        {
            this.imageControl.DataContext = null;
            InputImageViewState = InputViewState.Selection;
        }

        private void AddFormFileButtonClicked(object sender, RoutedEventArgs e)
        {
            this.formImageControl.DataContext = null;
            this.pdfViewerControl.Source = null;
            InputFormViewState = InputViewState.Selection;
        }

        private void OnInputImageSearchCompleted(object sender, ImageAnalyzer img)
        {
            if (img != null)
            {
                CurrentInputProductImage = img;

                this.imageControl.DataContext = img;

                InputImageViewState = InputViewState.Selected;
            }
        }

        private async void OnInputFormImageSearchCompleted(object sender, ImageAnalyzer img)
        {
            if (img != null)
            {
                IsFormImageSource = true;
                this.formImageControl.DataContext = img;

                StorageFile file = await TempStorageFolder.CreateFileAsync($"{Guid.NewGuid().ToString()}.jpg", CreationCollisionOption.ReplaceExisting);
                if (img.ImageUrl != null)
                {
                    await Util.DownloadAndSaveBitmapAsync(img.ImageUrl, file);
                }
                else if (img.GetImageStreamCallback != null)
                {
                    await Util.SaveBitmapToStorageFileAsync(await img.GetImageStreamCallback(), file);
                }

                CurrentInputFormFile = file;
            }

            InputFormViewState = InputViewState.Selected;
        }

        private async void OnInputFormFileSearchCompleted(object sender, StorageFile file)
        {
            if (file != null)
            {
                IsFormImageSource = false;
                StorageFile localFile = await file.CopyAsync(TempStorageFolder, $"{Guid.NewGuid().ToString()}{file.FileType}");
                this.pdfViewerControl.Source = new Uri(localFile.Path);
                CurrentInputFormFile = localFile;
            }

            InputFormViewState = InputViewState.Selected;
        }

        private void ValidateInputParams()
        {
            this.submitClaimButton.IsEnabled = InputImageViewState == InputViewState.Selected && InputFormViewState == InputViewState.Selected;
        }

        private void OnCancelInputButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag.ToString() == "image")
                {
                    this.imageControl.DataContext = null;
                    InputImageViewState = InputViewState.Selection;
                    this.inputImagePicker.RestartPicker();
                }
                else
                {
                    this.formImageControl.DataContext = null;
                    this.pdfViewerControl.Source = null;
                    InputFormViewState = InputViewState.Selection;
                    this.inputFormPicker.RestartPicker();
                }
            }
        }

        private async void OnSubmitClaimButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                this.progressRing.IsActive = true;

                CloseDetailsView();

                var productImage = this.imageControl.DataContext as ImageAnalyzer;
                var formImage = this.formImageControl.DataContext as ImageAnalyzer;
                var formFile = this.pdfViewerControl.Source as Uri;

                if (productImage != null && (formImage != null || formFile != null))
                {
                    var claim = new DataGridViewModel(Guid.NewGuid())
                    {
                        ClaimId = ++claimId,
                        IsFormImage = IsFormImageSource,
                        CustomName = emptyRow,
                        Date = emptyRow,
                        WarrantyId = emptyRow,
                        Warranty = emptyRow,
                        InvoiceTotal = emptyRow
                    };

                    // store image file to local folder
                    if (productImage.ImageUrl != null)
                    {
                        claim.ProductImageUri = productImage.ImageUrl;
                        claim.ProductImage = new BitmapImage(new Uri(productImage.ImageUrl));
                    }
                    else if (productImage.GetImageStreamCallback != null)
                    {
                        StorageFile imageFile = await InsuranceClaimDataLoader.CreateFileInLocalFolderAsync(claim.Id, "ProductImage.jpg");
                        await Util.SaveBitmapToStorageFileAsync(await productImage.GetImageStreamCallback(), imageFile);

                        claim.ProductImageUri = imageFile.Path;
                        claim.ProductImage = new BitmapImage(new Uri(imageFile.Path));
                    }

                    // store form file to local folder
                    if (isFormImageSource)
                    {
                        if (formImage?.ImageUrl != null)
                        {
                            claim.FormImageUri = formImage.ImageUrl;
                            claim.FormImage = new BitmapImage(new Uri(formImage.ImageUrl));
                        }
                        else if (formImage?.GetImageStreamCallback != null)
                        {
                            StorageFile imageFile = await InsuranceClaimDataLoader.CreateFileInLocalFolderAsync(claim.Id, "FormImage.jpg");
                            await Util.SaveBitmapToStorageFileAsync(await formImage.GetImageStreamCallback(), imageFile);

                            claim.FormImageUri = imageFile.Path;
                            claim.FormImage = new BitmapImage(new Uri(imageFile.Path));
                        }
                    }
                    else if (CurrentInputFormFile != null)
                    {
                        StorageFile file = await InsuranceClaimDataLoader.CopyFileToLocalFolderAsync(CurrentInputFormFile, $"FormFile{CurrentInputFormFile.FileType}", claim.Id);
                        claim.FormFile = new Uri(file.Path);
                    }

                    DataGridCollection.Add(claim);

                    // show submitted form
                    this.inputSubmittedGrid.Visibility = Visibility.Visible;

                    // validate product image and form
                    await ProcessClaimAsync(claim);

                    // store current claim
                    await InsuranceClaimDataLoader.SaveOrUpdateDataAsync(claim);

                    // update datagrid collection
                    await LoadDataGridCollectionAsync();
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure processing claim");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private async Task ProcessClaimAsync(DataGridViewModel claim)
        {
            var extractedFieldsTask = AnalyzeFormFromFileAsync(CurrentInputFormFile);
            var detectProductImageTask = AnalyzeProductImageAsync(ObjectDetectionModelId);        // Custom Vision Object Detection
            var classifyProductImageTask = AnalyzeProductImageAsync(ObjectClassificationModelId); // Custom Vision Object Classification
            await Task.WhenAll(detectProductImageTask, classifyProductImageTask, extractedFieldsTask);

            // Product validation
            List<PredictionModel> detectionMatches = detectProductImageTask.Result?.Predictions?.Where(p => p.Probability >= MinProbability && p.TagName.ToLower().Contains(ObjectName)).ToList();
            List<PredictionModel> classificationMatches = classifyProductImageTask.Result?.Predictions?.Where(p => p.Probability >= MinProbability && p.TagName.ToLower().Contains(ObjectName)).ToList();

            bool isProductImage = detectionMatches != null && detectionMatches.Any() && classificationMatches != null && classificationMatches.Any();
            claim.ProductImageValidStatus = isProductImage ? InputValidationStatus.Valid : InputValidationStatus.Invalid;
            if (isProductImage)
            {
                claim.ObjectDetectionMatches = detectionMatches;
                claim.ObjectClassificationMatches = classificationMatches;
            }

            // Form validation
            var extractedFields = extractedFieldsTask.Result;
            if (extractedFields != null)
            {
                claim.CustomName = extractedFields.FirstOrDefault(x => x.Item1.Text.ToLower().Contains(CustomFormFieldsDict[CustomFormFieldType.CustomName]))?.Item2 ?? emptyRow;
                claim.Date = extractedFields.FirstOrDefault(x => x.Item1.Text.ToLower().Contains(CustomFormFieldsDict[CustomFormFieldType.Date]))?.Item2 ?? emptyRow;
                claim.WarrantyId = extractedFields.FirstOrDefault(x => x.Item1.Text.ToLower().Contains(CustomFormFieldsDict[CustomFormFieldType.WarrantyId]))?.Item2 ?? emptyRow;
                claim.Warranty = extractedFields.FirstOrDefault(x => x.Item1.Text.ToLower().Contains(CustomFormFieldsDict[CustomFormFieldType.WarrantyAmount]))?.Item2 ?? emptyRow;
                claim.InvoiceTotal = extractedFields.FirstOrDefault(x => x.Item1.Text.ToLower().Contains(CustomFormFieldsDict[CustomFormFieldType.Total]))?.Item2 ?? emptyRow;

                string[] fields = CustomFormFieldsDict.Select(f => f.Value).ToArray();
                bool isValidForm = extractedFields.Any(e => fields.Any(f => e.Item1.Text.ToLower().Contains(f) && !string.IsNullOrEmpty(e.Item2.Text)));
                claim.FormValidStatus = isValidForm ? InputValidationStatus.Valid : InputValidationStatus.Invalid;
            }
            else
            {
                claim.FormValidStatus = InputValidationStatus.Invalid;
            }
        }

        private async Task<List<Tuple<TokenOverlayInfo, TokenOverlayInfo>>> AnalyzeFormFromFileAsync(StorageFile file)
        {
            if (file == null)
            {
                return null;
            }

            try
            {
                AnalyzeResult result = null;
                using (FileStream stream = new FileStream(file.Path, FileMode.Open))
                {
                    result = IsFormImageSource ? await this.formRecognizerService.AnalyzeImageFormWithCustomModelAsync(FormRecognizerModelId, stream)
                                               : await this.formRecognizerService.AnalyzePdfFormWithCustomModelAsync(FormRecognizerModelId, stream);
                }

                ExtractedPage page = result?.Pages.FirstOrDefault();
                if (page != null)
                {
                    int width = page.Width ?? 0;
                    int height = page.Height ?? 0;

                    var keyValuePairList = page.KeyValuePairs
                        .Where(x => x.Key != null && x.Key.Any() && x.Value != null && x.Value.Any() &&
                              !x.Key.Select(k => k.Text.ToLower()).Contains("__tokens__")) // showing pairs with non-empty keys
                        .Select(x => new Tuple<TokenOverlayInfo, TokenOverlayInfo>(
                                           new TokenOverlayInfo(x.Key, width, height),
                                           new TokenOverlayInfo(x.Value, width, height)))
                        .ToList();

                    return keyValuePairList;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure recognizing form");
            }

            return null;
        }

        private async Task<ImagePrediction> AnalyzeProductImageAsync(Guid modelId)
        {
            ImagePrediction result = null;

            try
            {
                var iteractions = await trainingApi.GetIterationsAsync(modelId);

                var latestTrainedIteraction = iteractions.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();

                if (latestTrainedIteraction == null)
                {
                    throw new Exception("This project doesn't have any trained models yet.");
                }

                if (CurrentInputProductImage?.ImageUrl != null)
                {
                    result = await CustomVisionServiceHelper.PredictImageUrlWithRetryAsync(predictionApi, modelId, new ImageUrl(CurrentInputProductImage.ImageUrl), latestTrainedIteraction.Id);
                }
                else if (CurrentInputProductImage?.GetImageStreamCallback != null)
                {
                    result = await CustomVisionServiceHelper.PredictImageWithRetryAsync(predictionApi, modelId, CurrentInputProductImage.GetImageStreamCallback, latestTrainedIteraction.Id);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Custom Vision error analyzing product image");
            }
            return result;
        }

        private void AddAnotherClaimButtonClicked(object sender, RoutedEventArgs e)
        {
            InputImageViewState = InputViewState.NotSelected;
            InputFormViewState = InputViewState.NotSelected;
            this.inputSubmittedGrid.Visibility = Visibility.Collapsed;

            CloseDetailsView();
        }

        private void DataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isAction = this.dataGrid.Tag != null && this.dataGrid.Tag.ToString() == DataGridAction;
            if (this.dataGrid.SelectedItem is DataGridViewModel selectedRow && !isAction)
            {
                this.detailsViewControl.OpenDetailsView(selectedRow);
                this.detailsViewControl.Visibility = Visibility.Visible;
            }
        }

        private void OnDetailsViewClosed(object sender, EventArgs args)
        {
            CloseDetailsView();
        }

        private void CloseDetailsView()
        {
            this.dataGrid.SelectedItem = null;
            this.detailsViewControl.Visibility = Visibility.Collapsed;
        }

        private async void OnDeleteDataGridRowButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                this.dataGrid.Tag = DataGridAction;

                var currentRow = (sender as Button)?.DataContext as DataGridViewModel;
                if (currentRow != null)
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Are you sure?",
                        Content = $"This operation will delete this claim permanently.",
                        PrimaryButtonText = "Delete",
                        SecondaryButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Secondary
                    };

                    ContentDialogResult result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        var itemToRemove = DataGridCollection.FirstOrDefault(x => x.Id == currentRow.Id);
                        bool entryRemovedFromFile = DataGridCollection.Remove(itemToRemove);
                        if (entryRemovedFromFile)
                        {
                            await InsuranceClaimDataLoader.DeleteDataAsync(new List<DataGridViewModel>() { itemToRemove });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting datagrid row");
            }
            finally
            {
                this.dataGrid.Tag = null;
                this.dataGrid.SelectedItem = null;
            }
        }
    }
}
