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

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using ServiceHelpers;
using ServiceHelpers.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.TranslatorExplorer
{
    [KioskExperience(Title = "Translator API Explorer", ImagePath = "ms-appx:/Assets/TranslatorExplorer.png")]
    public sealed partial class TranslatorExplorerPage : Page
    {
        private readonly int autoDetectTimeTickInSecond = 1;
        private readonly int maxCharactersForAlternativeTranslation = 100;
        private readonly int panelHeight = 350;
        private readonly LanguageDictionary DefaultInputLanguage = new LanguageDictionary { Code = "en", Name = "English" };

        private TranslatorTextService translatorTextService;
        private string outputLanguageName = "spanish";
        private string previousTranslatedText = string.Empty;
        private bool translateTextProcessing = false;
        private DispatcherTimer timer;
        private LanguageDictionary inputLanguage;

        public ObservableCollection<LanguageDictionary> InputLanguageCollection { get; } = new ObservableCollection<LanguageDictionary>();
        public ObservableCollection<Language> OutputLanguageCollection { get; } = new ObservableCollection<Language>();
        public ObservableCollection<AlternativeTranslations> AlternativeTranslationCollection { get; } = new ObservableCollection<AlternativeTranslations>();
        public ObservableCollection<SamplePhraseViewModel> SamplePhraseCollection { get; set; } = new ObservableCollection<SamplePhraseViewModel>();

        public TranslatorExplorerPage()
        {
            this.InitializeComponent();
            this.DataContext = this;

            timer = new DispatcherTimer();
            timer.Tick += AutoDetectHandlerTimerTick;
            timer.Interval = new TimeSpan(0, 0, autoDetectTimeTickInSecond);
            if (this.cameraControl != null)
            {
                this.cameraControl.ImageCaptured += CameraControl_ImageCaptured;
                this.cameraControl.CameraRestarted += CameraControl_CameraRestarted;
            }

            if (this.favoritePhotosGridView != null)
            {
                this.favoritePhotosGridView.ItemsSource = new string[]
                {
                    "https://intelligentkioskstore.blob.core.windows.net/translator-explorer/suggestedphotos/1.png",
                    "https://intelligentkioskstore.blob.core.windows.net/translator-explorer/suggestedphotos/2.png",
                };
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.TranslatorTextApiKey))
            {
                await new MessageDialog("Missing Text Analytics Key. Please enter the key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                this.translatorTextService = new TranslatorTextService(SettingsHelper.Instance.TranslatorTextApiKey);
                await LoadSupportedLanguagesAsync();
                LoadSamplePhrases();
                timer.Start();
            }

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            timer.Stop();
            await this.cameraControl?.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        private async Task LoadSupportedLanguagesAsync()
        {
            try
            {
                SupportedLanguages supportedLanguages = await this.translatorTextService?.GetSupportedLanguagesAsync();
                if (supportedLanguages != null)
                {
                    List<LanguageDictionary> languageDictionaryList = supportedLanguages.Dictionary
                    .Select(v =>
                    {
                        LanguageDictionary languageDict = v.Value;
                        languageDict.Code = v.Key;
                        return languageDict;
                    })
                    .OrderBy(v => v.Name)
                    .ToList();
                    InputLanguageCollection.Clear();
                    InputLanguageCollection.AddRange(languageDictionaryList);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading languages.");
            }
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWebCamHostGridSize();
            UpdateImageSize();
        }

        private async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearOutputData();
            this.previousInputLanguage = null;
            switch (this.pivotControl.SelectedIndex)
            {
                case 0: // Text Pivot
                    timer.Start();
                    await this.cameraControl?.StopStreamAsync();
                    this.landingMessage.Visibility = Visibility.Visible;

                    this.webCamHostGrid.Visibility = Visibility.Collapsed;
                    this.imageHostGrid.Visibility = Visibility.Collapsed;
                    this.ocrModeCombobox.Visibility = Visibility.Collapsed;
                    this.ocrTextGrid.Visibility = Visibility.Collapsed;

                    this.favoritePhotosGridView.SelectedItem = null;
                    this.imageFromCameraWithFaces.DataContext = null;
                    this.imageWithFacesControl.DataContext = null;
                    this.ocrTextBlock.Text = string.Empty;
                    this.outputText.MinHeight = panelHeight;
                    break;

                case 1: // Image Pivot
                    this.inputText.Text = string.Empty;
                    this.outputText.MinHeight = 150;
                    timer.Stop();
                    break;
            }
        }

        #region Pivot Text
        private LanguageDictionary previousInputLanguage = null;
        private void InputLanguageComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.inputLanguage = this.inputLanguageComboBox.SelectedValue as LanguageDictionary;
            if (this.inputLanguage == null)
            {
                return;
            }
            UpdateOutputLanguageCombobox(this.inputLanguage);
        }

        private async void OutputLanguageComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await TranslateTextAsync();
        }

        private void OnSwapLanguageButtonClicked(object sender, RoutedEventArgs e)
        {
            var inputLanguage = this.inputLanguageComboBox.SelectedValue as LanguageDictionary;
            var outputLanguage = this.outputLanguageComboBox.SelectedValue as Language;
            if (inputLanguage == null || outputLanguage == null)
            {
                return;
            }

            this.inputText.Text = this.outputText.Text;
            this.previousTranslatedText = this.inputText.Text;
            this.outputText.Text = string.Empty;
            LanguageDictionary newInputLanguage = InputLanguageCollection.FirstOrDefault(l => l.Code.Equals(outputLanguage.Code, StringComparison.OrdinalIgnoreCase));
            if (newInputLanguage != null)
            {
                Language newOutputLanguage = newInputLanguage.Translations?.FirstOrDefault(l => l.Code.Equals(inputLanguage.Code, StringComparison.OrdinalIgnoreCase));
                this.outputLanguageName = newOutputLanguage?.Name ?? this.outputLanguageName;
                this.inputLanguageComboBox.SelectedValue = newInputLanguage;
            }
        }

        private void OnInputTextSelectionChanged(object sender, RoutedEventArgs e)
        {
            int inputTextLength = this.inputText.Text.Length;
            this.inputTextLength.Text = inputTextLength.ToString();
            if (this.inputText.Text.Trim() == string.Empty)
            {
                ClearOutputData();
            }
        }

        private async void AutoDetectHandlerTimerTick(object sender, object e)
        {
            // Auto-detect language: call this request when input text longer then 1 symbol
            string newInputText = this.inputText.Text;
            if (newInputText.Length > 1 && !string.Equals(newInputText, previousTranslatedText, StringComparison.OrdinalIgnoreCase))
            {
                previousTranslatedText = newInputText;
                await DetectedLanguageAsync(this.inputText.Text);
                await TranslateTextAsync();
            }
        }

        private void SamplePhraseItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var node = args.InvokedItem as SamplePhraseViewModel;
            if (node != null && !node.IsGroupHeader)
            {
                this.favoriteTextPickerFlyout.Hide();
                this.inputText.Text = node.Text;
            }
        }
        #endregion

        #region Pivot Image
        private async void CameraControl_CameraRestarted(object sender, EventArgs e)
        {
            // We induce a delay here to give the camera some time to start rendering before we hide the last captured photo.
            // This avoids a black flash.
            await Task.Delay(500);
            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;
        }

        private async void CameraControl_ImageCaptured(object sender, ImageAnalyzer e)
        {
            this.UpdateActivePhoto(e);

            this.imageFromCameraWithFaces.TextRecognitionMode = e.TextRecognitionMode;
            this.imageFromCameraWithFaces.DataContext = e;
            this.imageFromCameraWithFaces.Visibility = Visibility.Visible;

            await this.cameraControl.StopStreamAsync();
        }

        private async void OnWebCamButtonClicked(object sender, RoutedEventArgs e)
        {
            this.favoritePhotosGridView.SelectedItem = null;
            this.landingMessage.Visibility = Visibility.Collapsed;
            this.webCamHostGrid.Visibility = Visibility.Visible;
            this.imageHostGrid.Visibility = Visibility.Collapsed;

            await this.cameraControl.StartStreamAsync();
            await Task.Delay(250);
            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;

            UpdateWebCamHostGridSize();
        }

        private async void OnFavoriteSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.favoriteImagePickerFlyout.Hide();
            if (!string.IsNullOrEmpty((string)this.favoritePhotosGridView.SelectedValue))
            {
                this.landingMessage.Visibility = Visibility.Collapsed;
                ImageAnalyzer image = new ImageAnalyzer((string)this.favoritePhotosGridView.SelectedValue);
                image.ShowDialogOnFaceApiErrors = true;

                this.imageHostGrid.Visibility = Visibility.Visible;
                this.webCamHostGrid.Visibility = Visibility.Collapsed;
                await this.cameraControl.StopStreamAsync();

                this.UpdateActivePhoto(image);

                this.imageWithFacesControl.TextRecognitionMode = image.TextRecognitionMode;
                this.imageWithFacesControl.DataContext = image;

                UpdateImageSize();
            }
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            this.favoritePhotosGridView.SelectedItem = null;

            this.imageSearchFlyout.Hide();
            ImageAnalyzer image = args.First();
            image.ShowDialogOnFaceApiErrors = true;

            this.imageHostGrid.Visibility = Visibility.Visible;
            this.webCamHostGrid.Visibility = Visibility.Collapsed;
            await this.cameraControl.StopStreamAsync();

            this.UpdateActivePhoto(image);

            this.imageWithFacesControl.TextRecognitionMode = image.TextRecognitionMode;
            this.imageWithFacesControl.DataContext = image;

            UpdateImageSize();
        }

        private void OnImageSearchCanceled(object sender, EventArgs e)
        {
            this.imageSearchFlyout.Hide();
        }

        private void UpdateActivePhoto(ImageAnalyzer img)
        {
            ClearOutputData();
            this.landingMessage.Visibility = Visibility.Collapsed;

            if (printedOCRComboBoxItem.IsSelected)
            {
                img.TextRecognitionMode = TextRecognitionMode.Printed;
            }
            else if (handwrittigOCRComboBoxItem.IsSelected)
            {
                img.TextRecognitionMode = TextRecognitionMode.Handwritten;
            }

            if (img.TextOperationResult?.RecognitionResult != null)
            {
                this.UpdateOcrTextBoxContent(img);
            }
            else
            {
                img.TextRecognitionCompleted += (s, args) =>
                {
                    this.UpdateOcrTextBoxContent(img);
                };
            }
        }

        private async void UpdateOcrTextBoxContent(ImageAnalyzer imageAnalyzer)
        {
            this.ocrTextBlock.Text = string.Empty;
            if (imageAnalyzer.TextOperationResult?.RecognitionResult?.Lines != null)
            {
                this.ocrModeCombobox.Visibility = Visibility.Visible;

                IEnumerable<string> lines = imageAnalyzer.TextOperationResult.RecognitionResult.Lines.Select(l => string.Join(" ", l?.Words?.Select(w => w.Text)));
                this.ocrTextBlock.Text = string.Join(" ", lines);
                this.ocrTextGrid.Visibility = Visibility.Visible;

                // Detect language and translate the OCR Recognized text
                await DetectedLanguageAsync(this.ocrTextBlock.Text);
                await TranslateTextAsync();
            }
        }

        private void OcrModeSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (printedOCRComboBoxItem.IsSelected)
            {
                UpdateTextRecognition(TextRecognitionMode.Printed);
            }
            else if (handwrittigOCRComboBoxItem.IsSelected)
            {
                UpdateTextRecognition(TextRecognitionMode.Handwritten);
            }
        }

        private void UpdateTextRecognition(TextRecognitionMode textRecognitionMode)
        {
            if (this.imageFromCameraWithFaces == null || this.imageWithFacesControl == null)
            {
                return;
            }

            imageFromCameraWithFaces.TextRecognitionMode = textRecognitionMode;
            imageWithFacesControl.TextRecognitionMode = textRecognitionMode;

            var currentImageDisplay = this.imageWithFacesControl.Visibility == Visibility.Visible ? this.imageWithFacesControl : this.imageFromCameraWithFaces;
            if (currentImageDisplay.DataContext != null)
            {
                var img = currentImageDisplay.DataContext;
                ImageAnalyzer analyzer = (ImageAnalyzer)img;
                if (analyzer.TextOperationResult?.RecognitionResult != null)
                {
                    UpdateOcrTextBoxContent(analyzer);
                }
                else
                {
                    analyzer.TextRecognitionCompleted += (s, args) =>
                    {
                        UpdateOcrTextBoxContent(analyzer);
                    };
                }
                currentImageDisplay.DataContext = null;
                currentImageDisplay.DataContext = img;
            }
        }
        #endregion

        private void UpdateOutputLanguageCombobox(LanguageDictionary inputLanguage)
        {
            if (inputLanguage != null && previousInputLanguage != inputLanguage)
            {
                // update output language combobox
                List<Language> outputLanguages = inputLanguage?.Translations?.OrderBy(v => v.Name).ToList() ?? new List<Language>();
                OutputLanguageCollection.Clear();
                OutputLanguageCollection.AddRange(outputLanguages);
                this.outputLanguageComboBox.SelectedValue =
                    OutputLanguageCollection.FirstOrDefault(v => v.Name.Equals(outputLanguageName, StringComparison.OrdinalIgnoreCase)) ??
                    OutputLanguageCollection.FirstOrDefault();
            }
            previousInputLanguage = inputLanguage;
        }

        private async Task DetectedLanguageAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                DetectedLanguageResult detectedLanguageResult = await this.translatorTextService?.DetectLanguageAsync(text);
                if (detectedLanguageResult != null)
                {
                    this.inputLanguage = InputLanguageCollection
                        .FirstOrDefault(l => l.Code.Equals(detectedLanguageResult.Language, StringComparison.OrdinalIgnoreCase));
                    if (this.inputLanguage != null)
                    {
                        switch (this.pivotControl.SelectedIndex)
                        {
                            case 0: // Pivot Text
                                this.inputLanguageComboBox.SelectedValue = this.inputLanguage;
                                break;
                            case 1: // Pivot Image
                                this.detectedLanguageTextBox.Text = $"Detected Language: {this.inputLanguage.Name}";
                                UpdateOutputLanguageCombobox(this.inputLanguage);
                                break;
                        }
                    }
                }
            }
            catch
            {
                // just ignore the exception here and let user select the language
            }
        }

        private async Task TranslateTextAsync()
        {
            if (translateTextProcessing)
            {
                return;
            }

            try
            {
                this.processingRing.IsActive = true;
                translateTextProcessing = true;

                string inputText = string.Empty;
                this.inputLanguage = this.inputLanguage ?? DefaultInputLanguage;
                switch (this.pivotControl.SelectedIndex)
                {
                    case 0:
                        inputText = this.inputText.Text;
                        break;
                    case 1:
                        inputText = this.ocrTextBlock.Text;
                        break;
                }

                var outputLanguage = this.outputLanguageComboBox.SelectedValue as Language;
                List<string> languageCodeList = outputLanguage != null ? new List<string>() { outputLanguage.Code } : new List<string>();
                if (!string.IsNullOrEmpty(inputText) && languageCodeList.Any())
                {
                    TranslationTextResult translationTextResult = await this.translatorTextService?.TranslateTextAsync(inputText, languageCodeList);
                    if (translationTextResult != null)
                    {
                        Translation translation = translationTextResult.Translations?.FirstOrDefault(t => t.To.Equals(outputLanguage.Code, StringComparison.OrdinalIgnoreCase));
                        this.outputText.Text = translation?.Text ?? string.Empty;
                    }
                    this.previousTranslatedText = inputText;

                    // Provides alternative translations for a word and a small number of idiomatic phrases
                    await UpdateAlternativeTranslationsAsync(inputText, this.inputLanguage.Code, outputLanguage.Code);
                }
                else
                {
                    ClearOutputData();
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure translating text");
            }
            finally
            {
                translateTextProcessing = false;
                this.processingRing.IsActive = false;
            }
        }

        private async Task UpdateAlternativeTranslationsAsync(string inputText, string inputLanguageCode, string outputLanguageCode)
        {
            if (inputText == null || inputText.Length > maxCharactersForAlternativeTranslation)
            {
                return;
            }

            try
            {
                AlternativeTranslationCollection.Clear();
                this.lookupLanguageTextBlock.Text = string.Empty;
                LookupLanguage lookupLanguage = await this.translatorTextService?.GetDictionaryLookup(inputText, inputLanguageCode, outputLanguageCode);
                if (lookupLanguage?.Translations != null && lookupLanguage.Translations.Any())
                {
                    var alternativeTranslations = new List<AlternativeTranslations>();
                    Dictionary<string, List<LookupTranslations>> groupedTranslations =
                        lookupLanguage.Translations.GroupBy(t => t.PosTag).ToDictionary(k => k.Key, v => v.ToList());
                    foreach (var groupedTranslation in groupedTranslations)
                    {
                        alternativeTranslations.Add(new AlternativeTranslations()
                        {
                            Tag = groupedTranslation.Key,
                            Translations = groupedTranslation.Value.Select(v => new CustomLookupTranslations
                            {
                                DisplayTarget = v.DisplayTarget,
                                BackTranslations = string.Join(", ", v.BackTranslations?.Select(t => t.DisplayText).ToList())
                            }).ToList()
                        });
                    }

                    // Update alternative translations
                    this.lookupLanguageTextBlock.Text = $"Translations of '{this.inputText.Text}'";
                    AlternativeTranslationCollection.AddRange(alternativeTranslations);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading alternative translations");
            }
        }

        private void UpdateWebCamHostGridSize()
        {
            double newHeight = this.webCamHostGrid.ActualWidth / (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
            this.webCamHostGrid.Height = newHeight;
            this.outputText.Height = newHeight > 0 ? newHeight : panelHeight;
        }

        private void UpdateImageSize()
        {
            double newHeight = this.imageHostGrid.ActualHeight;
            this.outputText.Height = newHeight > 0 ? newHeight : panelHeight;
        }

        private void ClearOutputData()
        {
            AlternativeTranslationCollection?.Clear();
            this.lookupLanguageTextBlock.Text = string.Empty;
            this.outputText.Text = string.Empty;
            this.previousTranslatedText = string.Empty;
        }

        private void LoadSamplePhrases()
        {
            var list = new List<SamplePhraseViewModel>()
            {
                new SamplePhraseViewModel("Essentials (English)", true)
                {
                    IsExpanded = true,
                    GroupHeaderSymbol = "Comment",
                    Children =
                    {
                        new SamplePhraseViewModel("Hello"),
                        new SamplePhraseViewModel("Thank you"),
                        new SamplePhraseViewModel("Hi, how are you?"),
                        new SamplePhraseViewModel("Hello, can I help you?")
                    }
                },
                new SamplePhraseViewModel("Technology (English)", true)
                {
                    IsExpanded = true,
                    GroupHeaderSymbol = "Keyboard",
                    Children =
                    {
                        new SamplePhraseViewModel("I need to make a telephone call."),
                        new SamplePhraseViewModel("Could you take a photo?"),
                        new SamplePhraseViewModel("What is your email address?"),
                        new SamplePhraseViewModel("Can you text me your contact information?")
                    }
                },
                new SamplePhraseViewModel("Essentials (Spanish)", true)
                {
                    IsExpanded = false,
                    GroupHeaderSymbol = "Comment",
                    Children =
                    {
                        new SamplePhraseViewModel("Hola"),
                        new SamplePhraseViewModel("Gracias"),
                        new SamplePhraseViewModel("Hola cómo estás?"),
                        new SamplePhraseViewModel("Hola, puedo ayudarte?")
                    }
                },
                new SamplePhraseViewModel("Technology (Spanish)", true)
                {
                    IsExpanded = false,
                    GroupHeaderSymbol = "Keyboard",
                    Children =
                    {
                        new SamplePhraseViewModel("Tengo que hacer una llamada de telefono."),
                        new SamplePhraseViewModel("Puedes tomar una foto?"),
                        new SamplePhraseViewModel("Cuál es tu dirección de correo electrónico?"),
                    }
                },
            };
            SamplePhraseCollection.AddRange(list);
        }
    }

    public class SamplePhraseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Text { get; set; }
        public bool IsGroupHeader { get; set; }
        public string GroupHeaderSymbol { get; set; }

        public SamplePhraseViewModel(string text, bool isGroupHeader = false)
        {
            Text = text;
            IsGroupHeader = isGroupHeader;
        }

        private ObservableCollection<SamplePhraseViewModel> children;
        public ObservableCollection<SamplePhraseViewModel> Children
        {
            get
            {
                if (children == null) { children = new ObservableCollection<SamplePhraseViewModel>(); }
                return children;
            }
            set { children = value; }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AlternativeTranslations
    {
        public string Tag { get; set; }
        public List<CustomLookupTranslations> Translations { get; set; }
    }

    public class CustomLookupTranslations
    {
        public string DisplayTarget { get; set; }
        public string BackTranslations { get; set; }
    }
}
