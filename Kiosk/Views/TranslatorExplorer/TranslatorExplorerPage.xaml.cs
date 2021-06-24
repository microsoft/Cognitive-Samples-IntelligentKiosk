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
using IntelligentKioskSample.Controls.Overlays.Primitives;
using ServiceHelpers;
using ServiceHelpers.Models;
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
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.TranslatorExplorer
{
    [KioskExperience(Id = "TranslatorExplorer",
        DisplayName = "Translator API Explorer",
        Description = "Translate text between multiple languages",
        ImagePath = "ms-appx:/Assets/DemoGallery/Translation Demo.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologyArea = TechnologyAreaType.Language,
        TechnologiesUsed = TechnologyType.Vision | TechnologyType.TranslatorText,
        DateAdded = "2019/01/15")]
    public sealed partial class TranslatorExplorerPage : Page, INotifyPropertyChanged
    {
        private readonly int autoDetectTimeTickInSecond = 1;
        private readonly int maxCharactersForAlternativeTranslation = 100;
        private readonly int panelHeight = 350;
        private static readonly Language DefaultLanguage = new Language { Code = "en", Name = "English" };

        private TranslatorTextService translatorTextService;
        private string previousTranslatedText = string.Empty;
        private bool translateTextProcessing = false;
        private Language customOutputLanguage;
        private DispatcherTimer timer;
        private Dictionary<string, List<Language>> alternativeTranslationDict = new Dictionary<string, List<Language>>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Language inputLanguage;
        public Language InputLanguage
        {
            get { return inputLanguage; }
            set
            {
                if (inputLanguage != value)
                {
                    inputLanguage = value;
                    NotifyPropertyChanged("InputLanguage");
                    InputLanguageChanged();
                }
            }
        }

        private Language outputLanguage;
        public Language OutputLanguage
        {
            get { return outputLanguage; }
            set
            {
                if (outputLanguage != value)
                {
                    outputLanguage = value;
                    NotifyPropertyChanged("OutputLanguage");
                    OutputLanguageChanged();
                }
            }
        }

        private void InputLanguageChanged()
        {
            if (InputLanguage != null)
            {
                // update output language combobox
                string outputLangCode = customOutputLanguage?.Code ?? OutputLanguage?.Code;
                OutputLanguageCollection.Clear();
                OutputLanguageCollection.AddRange(InputLanguageCollection.Where(x => x.Code != InputLanguage.Code));

                OutputLanguage = OutputLanguageCollection.FirstOrDefault(l => l.Code.Equals(outputLangCode ?? DefaultLanguage.Code, StringComparison.OrdinalIgnoreCase)) ?? OutputLanguageCollection.FirstOrDefault();
                customOutputLanguage = null;

                // Pivot Image
                this.detectedLanguageTextBox.Text = $"Detected Language: {InputLanguage.Name}";
            }
        }

        private async void OutputLanguageChanged()
        {
            await TranslateTextAsync();
        }

        public ObservableCollection<Language> InputLanguageCollection { get; } = new ObservableCollection<Language>();
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
                    "ms-appx:///Assets/DemoSamples/TranslatorExplorer/1.png",
                    "ms-appx:///Assets/DemoSamples/TranslatorExplorer/2.png"
                };
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.TranslatorTextApiKey))
            {
                await new MessageDialog("Missing Translator Text API Key. Please enter the key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                this.translatorTextService = new TranslatorTextService(SettingsHelper.Instance.TranslatorTextApiKey, SettingsHelper.Instance.TranslatorTextApiRegion);
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
                SupportedLanguages supportedLanguages = await this.translatorTextService.GetSupportedLanguagesAsync();
                this.alternativeTranslationDict = supportedLanguages.Dictionary.ToDictionary(l => l.Key, l => l.Value.Translations);
                List<Language> translationLanguageList = supportedLanguages.Translation
                    .Select(v =>
                    {
                        Language translationLang = v.Value;
                        translationLang.Code = v.Key;
                        return translationLang;
                    })
                    .OrderBy(v => v.Name).ToList();

                InputLanguageCollection.AddRange(translationLanguageList);
                OutputLanguageCollection.AddRange(translationLanguageList);
                OutputLanguage = OutputLanguageCollection.FirstOrDefault(l => l.Code.Equals(DefaultLanguage.Code, StringComparison.OrdinalIgnoreCase));
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
            switch (this.pivotControl.SelectedIndex)
            {
                case 0: // Text Pivot
                    timer.Start();
                    await this.cameraControl?.StopStreamAsync();
                    this.landingMessage.Visibility = Visibility.Visible;

                    this.webCamHostGrid.Visibility = Visibility.Collapsed;
                    this.imageHostGrid.Visibility = Visibility.Collapsed;
                    this.ocrTextGrid.Visibility = Visibility.Collapsed;

                    this.favoritePhotosGridView.SelectedItem = null;
                    WebCamOverlayPresenter.Source = null;
                    WebCamOverlayPresenter.ItemsSource = null;
                    ImageOverlayPresenter.Source = null;
                    WebCamOverlayPresenter.ItemsSource = null;
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

        private void OnSwapLanguageButtonClicked(object sender, RoutedEventArgs e)
        {
            if (InputLanguage != null && OutputLanguage != null)
            {
                this.inputText.Text = this.outputText.Text;
                this.previousTranslatedText = this.inputText.Text;
                this.outputText.Text = string.Empty;

                customOutputLanguage = InputLanguage;
                InputLanguage = InputLanguageCollection.FirstOrDefault(l => l.Code.Equals(OutputLanguage.Code, StringComparison.OrdinalIgnoreCase));
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
            if (args.InvokedItem is SamplePhraseViewModel node && !node.IsGroupHeader)
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
            WebCamOverlayPresenter.Source = null;
            WebCamOverlayPresenter.ItemsSource = null;
        }

        private async void CameraControl_ImageCaptured(object sender, ImageAnalyzer e)
        {
            await TranslateFromImage(e, WebCamOverlayPresenter);
        }

        async Task TranslateFromImage(ImageAnalyzer image, OverlayPresenter overlayPresenter)
        {
            ProgressIndicator.IsActive = true;

            //clear results
            this.UpdateActivePhoto(image);
            overlayPresenter.ItemsSource = null;

            //show image
            overlayPresenter.Source = await image.GetImageSource();
            await this.cameraControl.StopStreamAsync();

            //get OCR
            await image.RecognizeTextAsync();

            //outline words
            overlayPresenter.ItemsSource = image.TextOperationResult?.Lines.SelectMany(line => line.Words.Select(word => new TextOverlayInfo(word.Text, word.BoundingBox))).ToArray();

            //translate text
            await UpdateOcrTextBoxContent(image);

            ProgressIndicator.IsActive = false;
        }

        private async void OnWebCamButtonClicked(object sender, RoutedEventArgs e)
        {
            this.favoritePhotosGridView.SelectedItem = null;
            this.landingMessage.Visibility = Visibility.Collapsed;
            this.webCamHostGrid.Visibility = Visibility.Visible;
            this.imageHostGrid.Visibility = Visibility.Collapsed;

            await this.cameraControl.StartStreamAsync();
            await Task.Delay(250);
            WebCamOverlayPresenter.Source = null;
            WebCamOverlayPresenter.ItemsSource = null;

            UpdateWebCamHostGridSize();
        }

        private async void OnFavoriteSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.favoriteImagePickerFlyout.Hide();
            if (!string.IsNullOrEmpty((string)this.favoritePhotosGridView.SelectedValue))
            {
                this.landingMessage.Visibility = Visibility.Collapsed;

                string photoUrl = this.favoritePhotosGridView.SelectedValue as string;
                StorageFile localImageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(photoUrl));
                ImageAnalyzer image = new ImageAnalyzer(localImageFile.OpenStreamForReadAsync);

                this.imageHostGrid.Visibility = Visibility.Visible;
                this.webCamHostGrid.Visibility = Visibility.Collapsed;

                await TranslateFromImage(image, ImageOverlayPresenter);
            }
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            this.favoritePhotosGridView.SelectedItem = null;

            this.imageSearchFlyout.Hide();
            ImageAnalyzer image = args.First();

            this.imageHostGrid.Visibility = Visibility.Visible;
            this.webCamHostGrid.Visibility = Visibility.Collapsed;

            await TranslateFromImage(image, ImageOverlayPresenter);
        }

        private void OnImageSearchCanceled(object sender, EventArgs e)
        {
            this.imageSearchFlyout.Hide();
        }

        private void UpdateActivePhoto(ImageAnalyzer img)
        {
            ClearOutputData();
            this.landingMessage.Visibility = Visibility.Collapsed;
        }

        private async Task UpdateOcrTextBoxContent(ImageAnalyzer imageAnalyzer)
        {
            this.ocrTextBlock.Text = string.Empty;
            if (imageAnalyzer.TextOperationResult?.Lines != null)
            {
                IEnumerable<string> lines = imageAnalyzer.TextOperationResult.Lines.Select(l => string.Join(" ", l?.Words?.Select(w => w.Text)));
                this.ocrTextBlock.Text = string.Join(" ", lines);
                this.ocrTextGrid.Visibility = Visibility.Visible;

                // Detect language and translate the OCR Recognized text
                await DetectedLanguageAsync(this.ocrTextBlock.Text);
                await TranslateTextAsync();
            }
        }

        #endregion

        private async Task DetectedLanguageAsync(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    DetectedLanguageResult detectedLanguageResult = await this.translatorTextService.DetectLanguageAsync(text);
                    if (detectedLanguageResult != null)
                    {
                        InputLanguage = InputLanguageCollection.FirstOrDefault(l => l.Code.Equals(detectedLanguageResult.Language, StringComparison.OrdinalIgnoreCase));
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
                switch (this.pivotControl.SelectedIndex)
                {
                    case 0:
                        inputText = this.inputText.Text;
                        break;
                    case 1:
                        inputText = this.ocrTextBlock.Text;
                        break;
                }

                if (!string.IsNullOrEmpty(inputText) && !string.IsNullOrEmpty(OutputLanguage?.Code))
                {
                    TranslationTextResult translationTextResult = await this.translatorTextService.TranslateTextAsync(inputText, new List<string>() { OutputLanguage.Code });
                    if (translationTextResult != null)
                    {
                        Translation translation = translationTextResult.Translations?.FirstOrDefault(t => t.To.Equals(OutputLanguage.Code, StringComparison.OrdinalIgnoreCase));
                        this.outputText.Text = translation?.Text ?? string.Empty;
                    }
                    this.previousTranslatedText = inputText;

                    // Provides alternative translations for a word and a small number of idiomatic phrases
                    await UpdateAlternativeTranslationsAsync(inputText, InputLanguage.Code, OutputLanguage.Code);
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
            if (inputText?.Length > maxCharactersForAlternativeTranslation || string.IsNullOrEmpty(inputLanguageCode) || string.IsNullOrEmpty(outputLanguageCode))
            {
                return;
            }

            try
            {
                AlternativeTranslationCollection.Clear();
                this.lookupLanguageTextBlock.Text = string.Empty;

                if (alternativeTranslationDict.ContainsKey(inputLanguageCode) &&
                    alternativeTranslationDict[inputLanguageCode].Any(t => t.Code.Equals(outputLanguageCode, StringComparison.OrdinalIgnoreCase)))
                {
                    LookupLanguage lookupLanguage = await this.translatorTextService.GetDictionaryLookup(inputText, inputLanguageCode, outputLanguageCode);
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
            }
            catch (Exception ex)
            {
                if (SettingsHelper.Instance.ShowDebugInfo)
                {
                    await Util.GenericApiCallExceptionHandler(ex, "Failure loading alternative translations");
                }
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
