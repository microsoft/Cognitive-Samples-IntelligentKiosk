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

using IntelligentKioskSample.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.SpeechToText
{
    [KioskExperience(Id = "SpeechToTextExplorer",
        DisplayName = "Speech to Text Explorer",
        Description = "Transcribe speech from live audio in 10 languages",
        ImagePath = "ms-appx:/Assets/DemoGallery/Speech to Text Explorer.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.SpeechToText,
        TechnologyArea = TechnologyAreaType.Speech,
        DateAdded = "2019/05/23")]
    public sealed partial class SpeechToTextExplorer : Page, INotifyPropertyChanged
    {
        public static readonly int RecognitionMaxTimeDelayInSeconds = 5;
        public static readonly int RecognitionTimeLimitInSeconds = 30;

        private static readonly List<AudioSampleViewModel> audioSamples = new List<AudioSampleViewModel>()
        {
            new AudioSampleViewModel() { Name = "Sample 1 (English / US)", FileName = "ms-appx:///Assets/AudioSamples/Baryonyx.wav", LanguageCode = "en-US" },
            new AudioSampleViewModel() { Name = "Sample 2 (English / US)", FileName = "ms-appx:///Assets/AudioSamples/ClassQX_F.wav", LanguageCode = "en-US" },
            new AudioSampleViewModel() { Name = "Sample 3 (English / US)", FileName = "ms-appx:///Assets/AudioSamples/Fare4Denver_M.wav", LanguageCode = "en-US" },

            new AudioSampleViewModel() { Name = "Sample 1 (German / DE)", FileName = "ms-appx:///Assets/AudioSamples/de-de-1.wav", LanguageCode = "de-DE" },
            new AudioSampleViewModel() { Name = "Sample 2 (German / DE)", FileName = "ms-appx:///Assets/AudioSamples/de-de-2.wav", LanguageCode = "de-DE" },

            new AudioSampleViewModel() { Name = "Sample 1 (French / FR)", FileName = "ms-appx:///Assets/AudioSamples/fr-fr-1.wav", LanguageCode = "fr-FR" },

            new AudioSampleViewModel() { Name = "Sample 1 (Italian / IT)", FileName = "ms-appx:///Assets/AudioSamples/it-it-1.wav", LanguageCode = "it-IT" },
            new AudioSampleViewModel() { Name = "Sample 2 (Italian / IT)", FileName = "ms-appx:///Assets/AudioSamples/it-it-2.wav", LanguageCode = "it-IT" },

            new AudioSampleViewModel() { Name = "Sample 1 (Spanish / ES)", FileName = "ms-appx:///Assets/AudioSamples/es-es-1.wav", LanguageCode = "es-ES" },
            new AudioSampleViewModel() { Name = "Sample 2 (Spanish / ES)", FileName = "ms-appx:///Assets/AudioSamples/es-es-2.wav", LanguageCode = "es-ES" },

            new AudioSampleViewModel() { Name = "Sample 1 (Chinese / CN)", FileName = "ms-appx:///Assets/AudioSamples/zh-cn-1.wav", LanguageCode = "zh-CN" },
            new AudioSampleViewModel() { Name = "Sample 2 (Chinese / CN)", FileName = "ms-appx:///Assets/AudioSamples/zh-cn-2.wav", LanguageCode = "zh-CN" },

            new AudioSampleViewModel() { Name = "Sample 1 (Russian / RU)", FileName = "ms-appx:///Assets/AudioSamples/ru-ru-1.wav", LanguageCode = "ru-RU" },
            new AudioSampleViewModel() { Name = "Sample 2 (Russian / RU)", FileName = "ms-appx:///Assets/AudioSamples/ru-ru-2.wav", LanguageCode = "ru-RU" }
        };

        private static readonly List<SupportedLanguage> supportedLanguages = new List<SupportedLanguage>()
        {
            new SupportedLanguage() { Name = "English (US)", Code = "en-US", TranslationCode = "en" },
            new SupportedLanguage() { Name = "Arabic (EG)", Code = "ar-EG", TranslationCode = "ar" },
            new SupportedLanguage() { Name = "Chinese (Mandarin)", Code = "zh-CN", TranslationCode = "zh-Hans" },
            new SupportedLanguage() { Name = "French (FR)", Code = "fr-FR", TranslationCode = "fr" },
            new SupportedLanguage() { Name = "German", Code = "de-DE", TranslationCode = "de" },
            new SupportedLanguage() { Name = "Italian", Code = "it-IT", TranslationCode = "it" },
            new SupportedLanguage() { Name = "Japanese", Code = "ja-JP", TranslationCode = "ja" },
            new SupportedLanguage() { Name = "Portuguese (BR)", Code = "pt-BR", TranslationCode = "pt" },
            new SupportedLanguage() { Name = "Russian", Code = "ru-RU", TranslationCode = "ru" },
            new SupportedLanguage() { Name = "Spanish (ES)", Code = "es-ES", TranslationCode = "es" }
        };

        private DispatcherTimer timer;
        private DispatcherTimer recordingTimer;
        private int recordingSeconds = 0;
        private bool isAzureSpeechEndpoint = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool isProcessing = false;
        public bool IsProcessing
        {
            get { return isProcessing; }
            set
            {
                if (isProcessing != value)
                {
                    isProcessing = value;
                    NotifyPropertyChanged("IsProcessing");
                }
            }
        }

        private SpeechExplorerState speechExplorerState = SpeechExplorerState.Initial;
        private SpeechExplorerState oldSpeechExplorerState = SpeechExplorerState.Initial;
        public SpeechExplorerState SpeechExplorerState
        {
            get { return speechExplorerState; }
            set
            {
                if (speechExplorerState != value)
                {
                    oldSpeechExplorerState = SpeechExplorerState;
                    speechExplorerState = value;
                    NotifyPropertyChanged("SpeechExplorerState");
                }
            }
        }

        private SupportedLanguage currentInputLanguage = supportedLanguages.FirstOrDefault(x => x.Code == "en-US");
        public SupportedLanguage CurrentInputLanguage
        {
            get { return currentInputLanguage; }
            set
            {
                if (currentInputLanguage != value)
                {
                    currentInputLanguage = value;
                    NotifyPropertyChanged("CurrentInputLanguage");
                    CurrentInputLanguageChanged();
                }
            }
        }

        private SupportedLanguage firstTranslationLanguage;
        public SupportedLanguage FirstTranslationLanguage
        {
            get { return firstTranslationLanguage; }
            set
            {
                if (firstTranslationLanguage != value)
                {
                    firstTranslationLanguage = value;
                    NotifyPropertyChanged("FirstTranslationLanguage");

                    TranslationRecognitionData outputData = new TranslationRecognitionData()
                    {
                        FirstTranslationText = this.speechToTextWithTranslation.GetFirstTranslatedText()
                    };
                    TranslationLanguagesChanged(outputData);
                }
            }
        }

        private SupportedLanguage secondTranslationLanguage;
        public SupportedLanguage SecondTranslationLanguage
        {
            get { return secondTranslationLanguage; }
            set
            {
                if (secondTranslationLanguage != value)
                {
                    secondTranslationLanguage = value;
                    NotifyPropertyChanged("SecondTranslationLanguage");

                    TranslationRecognitionData outputData = new TranslationRecognitionData()
                    {
                        SecondTranslationText = this.speechToTextWithTranslation.GetSecondTranslatedText()
                    };
                    TranslationLanguagesChanged(outputData);
                }
            }
        }

        public ObservableCollection<SupportedLanguage> InputLanguagesCollection { get; set; } = new ObservableCollection<SupportedLanguage>(supportedLanguages);
        public ObservableCollection<SupportedLanguage> TranslationLanguagesCollection { get; set; } = new ObservableCollection<SupportedLanguage>();
        public ObservableCollection<AudioSampleViewModel> AudioSampleCollection { get; set; } = new ObservableCollection<AudioSampleViewModel>();

        public SpeechToTextExplorer()
        {
            this.InitializeComponent();
            this.DataContext = this;

            TranslationLanguagesCollection.AddRange(supportedLanguages.Where(x => x.Code != CurrentInputLanguage?.Code));
            AudioSampleCollection.AddRange(audioSamples.Where(x => x.LanguageCode == CurrentInputLanguage?.Code));
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // check Speech API keys
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiKey) && !string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiEndpoint))
            {
                this.isAzureSpeechEndpoint = IsAzureSpeechEndpoint(SettingsHelper.Instance.SpeechApiEndpoint);
                await Initialize();
            }
            else
            {
                this.mainPage.IsEnabled = false;
                ContentDialog missingApiKeyDialog = new ContentDialog
                {
                    Title = "Missing Speech API Key",
                    Content = "Please enter a valid Speech API key in the Settings Page.",
                    PrimaryButtonText = "Open Settings",
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Primary
                };

                ContentDialogResult result = await missingApiKeyDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    AppShell.Current.NavigateToPage(typeof(SettingsPage));
                }
            }
        }

        private async Task Initialize()
        {
            // Prompt the user for permission to access the microphone.
            bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained)
            {
                this.mainPage.IsEnabled = true;
                this.notificationControl.Visibility = Visibility.Visible;
                this.targetLanguagesListView.IsEnabled = this.isAzureSpeechEndpoint;

                timer = new DispatcherTimer();
                timer.Tick += AutoStopRecognitionHandlerTimerTick;
                timer.Interval = new TimeSpan(0, 0, RecognitionTimeLimitInSeconds);

                recordingTimer = new DispatcherTimer();
                recordingTimer.Tick += RecordingTimerTickHandler;
                recordingTimer.Interval = new TimeSpan(0, 0, 1);

                this.speechToTextView.ShowNotificationEventHandler += OnShowNotification;
                this.speechToTextWithTranslation.ShowNotificationEventHandler += OnShowNotification;
                this.speechToTextWithTranslation.Closed += (s, args) => {
                    SpeechExplorerState = oldSpeechExplorerState != SpeechExplorerState.SpeechToTextWithTranslation ? oldSpeechExplorerState : SpeechExplorerState.SpeechToText;
                };
            }
            else
            {
                this.mainPage.IsEnabled = false;
                this.notificationControl.Visibility = Visibility.Collapsed;

                ContentDialog deleteFileDialog = new ContentDialog
                {
                    Title = "Intelligent Kiosk can't access the microphone",
                    Content = "To let kiosk use this device's microphone, go to Windows Settings -&gt; Apps and turn on microphone permissions for Intelligent Kiosk.",
                    PrimaryButtonText = "Open Settings",
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Primary
                };

                ContentDialogResult result = await deleteFileDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-microphone"));
                }
            }
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.speechToTextView.ShowNotificationEventHandler -= OnShowNotification;
            this.speechToTextWithTranslation.ShowNotificationEventHandler -= OnShowNotification;
            await StopRecognitionAsync();
            base.OnNavigatedFrom(e);
        }

        private async void AudioSampleCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioSampleViewModel selectedAudioSample = this.audioSampleListView.SelectedValue as AudioSampleViewModel;
            if (selectedAudioSample == null || string.IsNullOrEmpty(selectedAudioSample.FileName))
            {
                return;
            }

            try
            {
                IsProcessing = true;
                var properties = new Dictionary<string, string>();
                this.startDictateButton.IsEnabled = false;
                this.stopDictateButton.IsEnabled = false;
                recordingSeconds = 0;
                this.recordingTimePanel.Visibility = Visibility.Collapsed;
                this.audioSampleFlyout.Hide();

                var audioFileUri = new Uri(selectedAudioSample.FileName);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(audioFileUri);
                if (file != null)
                {
                    Play(audioFileUri);
                    switch (SpeechExplorerState)
                    {
                        case SpeechExplorerState.Initial:
                        case SpeechExplorerState.SpeechToText:
                            SpeechExplorerState = SpeechExplorerState.SpeechToText;
                            await this.speechToTextView.SpeechRecognitionFromFileAsync(file);
                            break;
                        case SpeechExplorerState.SpeechToTextWithTranslation:
                            await this.speechToTextWithTranslation.SpeechRecognitionFromFileAsync(file);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Audio sample recognition error");
            }
            finally
            {
                IsProcessing = false;
                this.recordingTimePanel.Visibility = Visibility.Visible;
                this.startDictateButton.IsEnabled = true;
                this.stopDictateButton.IsEnabled = true;
                this.audioSampleListView.SelectedItem = null;
            }
        }

        private void CurrentInputLanguageChanged()
        {
            // update the text boxes
            RecognitionData inputData;
            switch (SpeechExplorerState)
            {
                case SpeechExplorerState.Initial:
                case SpeechExplorerState.SpeechToText:
                    inputData = this.speechToTextView.GetRecognizedData();
                    if (!string.IsNullOrEmpty(inputData?.Text) && !Regex.IsMatch(inputData.Text, @"[()]"))
                    {
                        inputData.Text = inputData.Language != null ? $"({inputData.Language.Name}) {inputData.Text}" : inputData.Text;
                        inputData.HighlightStyle = true;
                        this.speechToTextView.SetRecognitionData(inputData);
                    }
                    break;
                case SpeechExplorerState.SpeechToTextWithTranslation:
                    inputData = this.speechToTextWithTranslation.GetRecognizedData();
                    if (!string.IsNullOrEmpty(inputData?.Text) && !Regex.IsMatch(inputData.Text, @"[()]"))
                    {
                        inputData.Text = inputData.Language != null ? $"({inputData.Language.Name}) {inputData.Text}" : inputData.Text;
                        inputData.HighlightStyle = true;
                        this.speechToTextWithTranslation.SetRecognitionData(inputData, null);
                    }
                    break;
            }

            // update audio samples
            AudioSampleCollection.Clear();
            AudioSampleCollection.AddRange(audioSamples.Where(x => x.LanguageCode == CurrentInputLanguage?.Code));

            // update translation languages
            string firstLangCode = FirstTranslationLanguage?.Code;
            string secondLangCode = SecondTranslationLanguage?.Code;

            TranslationLanguagesCollection.Clear();
            TranslationLanguagesCollection.AddRange(supportedLanguages.Where(x => x.Code != CurrentInputLanguage?.Code));

            FirstTranslationLanguage = CurrentInputLanguage?.Code == firstLangCode
                ? TranslationLanguagesCollection.FirstOrDefault() : TranslationLanguagesCollection.FirstOrDefault(x => x.Code == firstLangCode);
            SecondTranslationLanguage = CurrentInputLanguage?.Code == secondLangCode
                ? TranslationLanguagesCollection.LastOrDefault() : TranslationLanguagesCollection.FirstOrDefault(x => x.Code == secondLangCode);
        }

        private void TranslationLanguagesChanged(TranslationRecognitionData outputData)
        {
            switch (SpeechExplorerState)
            {
                case SpeechExplorerState.Initial:
                case SpeechExplorerState.SpeechToText:
                    this.speechToTextWithTranslation.SetRecognitionData(this.speechToTextView.GetRecognizedData(), null);
                    SpeechExplorerState = SpeechExplorerState.SpeechToTextWithTranslation;
                    break;

                case SpeechExplorerState.SpeechToTextWithTranslation:

                    if (!string.IsNullOrEmpty(outputData?.FirstTranslationText?.Text))
                    {
                        string text = outputData.FirstTranslationText.Text;
                        string language = outputData.FirstTranslationText.Language?.Name;
                        if (!Regex.IsMatch(text, @"[()]"))
                        {
                            outputData.FirstTranslationText.Text = language != null ? $"({language}) {text}" : string.Empty;
                            outputData.FirstTranslationText.HighlightStyle = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(outputData?.SecondTranslationText?.Text))
                    {
                        string text = outputData.SecondTranslationText.Text;
                        string language = outputData.SecondTranslationText.Language?.Name;
                        if (!Regex.IsMatch(text, @"[()]"))
                        {
                            outputData.SecondTranslationText.Text = language != null ? $"({language}) {text}" : string.Empty;
                            outputData.SecondTranslationText.HighlightStyle = true;
                        }
                    }
                    this.speechToTextWithTranslation.SetRecognitionData(null, outputData);
                    break;
            }

            this.targetLanguagesListView.IsEnabled = FirstTranslationLanguage == null || SecondTranslationLanguage == null;
        }

        private async void StartDictateButtonClicked(object sender, RoutedEventArgs e)
        {
            await StartRecognitionAsync();
        }

        private async void StopDictateButtonClicked(object sender, RoutedEventArgs e)
        {
            await StopRecognitionAsync();
        }

        private async Task StartRecognitionAsync()
        {
            IsProcessing = true;
            switch (SpeechExplorerState)
            {
                case SpeechExplorerState.Initial:
                case SpeechExplorerState.SpeechToText:
                    SpeechExplorerState = SpeechExplorerState.SpeechToText;
                    await this.speechToTextView.StartSpeechRecognitionAsync();
                    break;
                case SpeechExplorerState.SpeechToTextWithTranslation:
                    await this.speechToTextWithTranslation.StartSpeechRecognitionAsync();
                    break;
            }

            recordingSeconds = 0;
            recordingTimer?.Start();
            timer?.Start();
        }

        private async Task StopRecognitionAsync()
        {
            if (!IsProcessing)
            {
                return;
            }

            try
            {
                IsProcessing = false;
                timer?.Stop();
                recordingTimer?.Stop();

                switch (SpeechExplorerState)
                {
                    case SpeechExplorerState.Initial:
                    case SpeechExplorerState.SpeechToText:
                        SpeechExplorerState = SpeechExplorerState.SpeechToText;
                        await this.speechToTextView.StopSpeechRecognitionAsync();
                        break;
                    case SpeechExplorerState.SpeechToTextWithTranslation:
                        await this.speechToTextWithTranslation.StopSpeechRecognitionAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Stop speech recognition error");
            }
        }

        private async void AutoStopRecognitionHandlerTimerTick(object sender, object e)
        {
            ShowNotificationAlert(prms: new NotificationParams { Message = "Your recording session ended after reaching the time limit." });
            await StopRecognitionAsync();
        }

        private void RecordingTimerTickHandler(object sender, object e)
        {
            if (recordingSeconds <= RecognitionTimeLimitInSeconds)
            {
                recordingSeconds += 1;
                this.recordingSecondsTextBlock.Text = recordingSeconds < 10 ? $"00:0{recordingSeconds}" : $"00:{recordingSeconds}";
            }
        }

        private void Play(Uri fileUri)
        {
            this.mediaPlayerElement.Source = MediaSource.CreateFromUri(fileUri);
            this.mediaPlayerElement.MediaPlayer.Play();
        }

        private void OnMainGridTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var tappedElement = e.OriginalSource as FrameworkElement;
            if (tappedElement == this.notificationControl)
            {
                ShowNotificationAlert();
            }
            else
            {
                HideNotificationAlert();
            }
        }

        private void OnShowNotification(object sender, NotificationViewModel notificationViewModel)
        {
            ShowNotificationAlert(notificationViewModel.NotificationType, notificationViewModel.IsFileProcessing, notificationViewModel.NotificationParams);
        }

        private async void ShowNotificationAlert(NotificationType notificationType = NotificationType.Warning, bool isFileProcessing = false, NotificationParams prms = null)
        {
            if (notificationType == NotificationType.Error && !isFileProcessing)
            {
                await this.StopRecognitionAsync();
            }

            if (prms != null)
            {
                this.notificationControl.ShowNotification(prms);
            }
        }

        private void HideNotificationAlert()
        {
            this.notificationControl.HideNotification();
        }

        private void TargetLanguagesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.targetLanguagesListView.SelectedItem is SupportedLanguage supportedLanguage)
            {
                if (FirstTranslationLanguage == null)
                {
                    FirstTranslationLanguage = supportedLanguage;
                    SpeechExplorerState = SpeechExplorerState.SpeechToTextWithTranslation;
                    this.speechToTextWithTranslation.UpdateTranslationView();
                }
                else if (SecondTranslationLanguage == null)
                {
                    SecondTranslationLanguage = supportedLanguage;
                    SpeechExplorerState = SpeechExplorerState.SpeechToTextWithTranslation;
                    this.speechToTextWithTranslation.UpdateTranslationView();
                }
            }
            this.targetLanguagesListView.SelectedItem = null;
            this.translationFlyout.Hide();
        }

        private void TranslationFlyout_Opened(object sender, object e)
        {
            if (!this.isAzureSpeechEndpoint)
            {
                ShowNotificationAlert(prms: new NotificationParams
                {
                    BackgroundColor = Colors.OrangeRed,
                    Message = "Speech Service container only supports Speech-to-text feature. Click here for details: ",
                    Link = new CustomHyperlink(new Uri("https://aka.ms/speechcontainer"))
                });
            }
        }

        private bool IsAzureSpeechEndpoint(string endpoint)
        {
            bool isUri = !string.IsNullOrEmpty(endpoint) && Uri.IsWellFormedUriString(endpoint, UriKind.Absolute);
            if (isUri)
            {
                string host = new Uri(endpoint).Host;
                return !string.IsNullOrEmpty(host) && host.Contains(@".speech.microsoft.com");
            }
            return false;
        }
    }

    public enum SpeechExplorerState
    {
        Initial,
        SpeechToText,
        SpeechToTextWithTranslation
    }

    public enum NotificationType
    {
        Error,
        Warning
    }

    public class AudioSampleViewModel
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string LanguageCode { get; set; }
    }

    public class SupportedLanguage
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string TranslationCode { get; set; }
    }

    public class RecognitionData
    {
        public string Text { get; set; }
        public SupportedLanguage Language { get; set; }
        public bool HighlightStyle { get; set; }

        public RecognitionData(string text = "", SupportedLanguage language = null, bool highlightStyle = false)
        {
            Text = text;
            Language = language;
            HighlightStyle = highlightStyle;
        }
    }

    public class NotificationViewModel
    {
        public NotificationType NotificationType { get; set; } = NotificationType.Warning;
        public bool IsFileProcessing { get; set; }
        public NotificationParams NotificationParams { get; set; }
    }

    public class PageStateToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            SpeechExplorerState? state = value as SpeechExplorerState?;
            SpeechExplorerState? stateParameter = (SpeechExplorerState)Enum.Parse(typeof(SpeechExplorerState), parameter.ToString());

            if (state.HasValue && stateParameter.HasValue && state == stateParameter)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
