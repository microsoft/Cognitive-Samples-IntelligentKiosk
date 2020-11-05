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

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using ServiceHelpers;
using ServiceHelpers.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.SpeakerRecognition
{
    [KioskExperience(Id = "SpeakerRecognitionExplorer",
        DisplayName = "Speaker Recognition Explorer",
        Description = "Identify speakers by their unique voice characteristics using voice biometry",
        ImagePath = "ms-appx:/Assets/DemoGallery/Speaker Recognition Explorer.jpg",
        ExperienceType = ExperienceType.Preview | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.SpeechToText,
        TechnologyArea = TechnologyAreaType.Speech,
        DateAdded = "2020/10/07")]
    public sealed partial class SpeakerRecognitionExplorer : Page, INotifyPropertyChanged
    {
        private static readonly string DefaultSpeechAPIRegion = "westus";

        public static readonly string[] AudioExtensions = new string[] { ".wav" };

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool isWestUSRegion = false;
        private SpeechConfig userProvidedSpeechConfig;
        private SpeakerRecognitionService userProvidedSpeakerRecognitionService;

        public ObservableCollection<SpeakerRecognitionViewModel> Models { get; set; } = new ObservableCollection<SpeakerRecognitionViewModel>();

        private SourceType sourceType = SourceType.FromMicrophone;
        public SourceType SourceType
        {
            get { return sourceType; }
            set
            {
                if (sourceType != value)
                {
                    sourceType = value;
                    NotifyPropertyChanged("SourceType");
                }
            }
        }

        private ProcessState processState = ProcessState.Overview;
        public ProcessState ProcessState
        {
            get { return processState; }
            set
            {
                if (processState != value)
                {
                    processState = value;
                    NotifyPropertyChanged("ProcessState");
                }
            }
        }

        public SpeakerRecognitionExplorer()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.isWestUSRegion = !string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiEndpoint) && SettingsHelper.Instance.SpeechApiEndpoint.Contains("westus.");
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiKey) && isWestUSRegion)
            {
                this.mainPage.IsEnabled = true;
                this.userProvidedSpeechConfig = SpeechConfig.FromSubscription(SettingsHelper.Instance.SpeechApiKey, DefaultSpeechAPIRegion);
                this.userProvidedSpeakerRecognitionService = new SpeakerRecognitionService(SettingsHelper.Instance.SpeechApiKey, $"https://{DefaultSpeechAPIRegion}.api.cognitive.microsoft.com");

                ProcessState = ProcessState.Overview;
                await LoadModelsAsync();
            }
            else
            {
                this.mainPage.IsEnabled = false;
                await new MessageDialog("Missing or unsupported Speech API Key in the Settings Page. \nSpeaker Recognition is a preview service, and currently only available in the West US region.", "Missing API Keys").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Pause();
            base.OnNavigatedFrom(e);
        }

        private async void OnUploadAudioFileClick(object sender, RoutedEventArgs e)
        {
            StorageFile file = await Util.PickSingleFileAsync(AudioExtensions);
            if (file != null)
            {
                await StartRecognizeFromFileAsync(file);
            }
        }

        private async void StartRecognitionButtonClicked(object sender, RoutedEventArgs e)
        {
            if (Models.Any())
            {
                await RecognizeFromSourceAsync(SourceType.FromMicrophone);
            }
            else
            {
                await ShowMessage("No enrolled voices", "You have no enrolled voices. Please enroll a new voice.");
            }
        }

        private async void OnFilesSamplesDropDownButtonClicked(object sender, RoutedEventArgs e)
        {
            if (Models.Any())
            {
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
            }
            else
            {
                await ShowMessage("No enrolled voices", "You have no enrolled voices. Please enroll a new voice.");
            }
        }

        private void OnSwitchInputModeButtonClicked(object sender, RoutedEventArgs e)
        {
            SourceType = SourceType == SourceType.FromFile ? SourceType.FromMicrophone : SourceType.FromFile;
            ProcessState = ProcessState.Overview;
        }

        private void OnTryAgainButtonClicked(object sender, RoutedEventArgs e)
        {
            ProcessState = ProcessState.Overview;
        }

        #region Recognition

        private async Task StartRecognizeFromFileAsync(StorageFile file)
        {
            StorageFile fileCopy = null;
            try
            {
                this.fileSamplesFlyout.Hide();

                fileCopy = await file.CopyAsync(ApplicationData.Current.TemporaryFolder, file.Name, NameCollisionOption.GenerateUniqueName);
                Play(file);
                await RecognizeFromSourceAsync(SourceType.FromFile, fileCopy);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure recognizing from file");
            }
            finally
            {
                if (fileCopy != null)
                {
                    await fileCopy.DeleteAsync();
                }
            }
        }

        private async Task RecognizeFromSourceAsync(SourceType sourceType, StorageFile file = null)
        {
            try
            {
                ProcessState = ProcessState.Processing;
                ToggleUI(isProcessing: true);

                // get all identification profiles based on current speakerRecognitionService
                SpeakerProfilesResponse userProfileResponse = await this.userProvidedSpeakerRecognitionService.GetProfilesAsync(VoiceProfileType.TextIndependentIdentification);
                List<SpeakerRecognitionViewModel> userIdentificationProfiles = userProfileResponse != null ? Models.Where(m => userProfileResponse.Profiles.Select(p => p.ProfileId).Contains(m.IdentificationProfileId)).ToList() : new List<SpeakerRecognitionViewModel>();

                // recognize
                SpeakerRecognitionResult userResult = null;
                using (AudioConfig audioInput = sourceType == SourceType.FromFile ? AudioConfig.FromWavFileInput(file.Path) : AudioConfig.FromDefaultMicrophoneInput())
                {
                    userResult = await RecognizeAsync(userProvidedSpeechConfig, audioInput, userIdentificationProfiles);
                }

                // display result
                if (userResult == null || userResult.Reason == ResultReason.Canceled)
                {
                    ProcessState = ProcessState.Error;
                    var cancellation = SpeakerRecognitionCancellationDetails.FromResult(userResult);
                    await ShowMessage("Speaker Identification Error", $"CANCELED: ErrorCode={cancellation.ErrorCode} ErrorDetails={cancellation.ErrorDetails}");
                }
                else
                {
                    ProcessState = ProcessState.Completed;
                    double score = userResult?.Score ?? 0;
                    this.detectedVoice.Text = userResult != null ? userIdentificationProfiles.FirstOrDefault(m => string.Equals(m.IdentificationProfileId, userResult.ProfileId, StringComparison.OrdinalIgnoreCase))?.Name : string.Empty;
                    this.detectedVoiceProbability.Text = $"({Math.Round(score * 100)}%)";
                }
            }
            catch (Exception ex)
            {
                ProcessState = ProcessState.Error;
                await Util.GenericApiCallExceptionHandler(ex, "Speaker recognition error");
            }
            finally
            {
                ToggleUI(isProcessing: false);
            }
        }

        private async Task<SpeakerRecognitionResult> RecognizeAsync(SpeechConfig speechConfig, AudioConfig audioConfig, List<SpeakerRecognitionViewModel> identificationProfiles)
        {
            var voiceProfiles = new List<VoiceProfile>();
            foreach (SpeakerRecognitionViewModel profile in identificationProfiles)
            {
                voiceProfiles.Add(new VoiceProfile(profile.IdentificationProfileId));
            }

            using (var speakerRecognizer = new SpeakerRecognizer(speechConfig, audioConfig))
            {
                var model = SpeakerIdentificationModel.FromProfiles(voiceProfiles);
                return await speakerRecognizer.RecognizeOnceAsync(model);
            }
        }

        #endregion


        #region Voice Models

        private async Task LoadModelsAsync()
        {
            try
            {
                this.Models.Clear();
                List<SpeakerRecognitionViewModel> allCustomModels = await SpeakerRecognitionDataLoader.GetCustomModelsAsync();
                this.Models.AddRange(allCustomModels);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading models");
            }
        }

        private void OnCreateNewModelClick(object sender, RoutedEventArgs e)
        {
            this.speakerRecognitionModelSetupControl.OpenScenarioSetupForm(this.userProvidedSpeakerRecognitionService, this.userProvidedSpeechConfig);
        }

        private async void OnNewModelCreated(object sender, SpeakerRecognitionViewModel e)
        {
            try
            {
                this.Models.Add(e);

                // update local file with custom models
                await SpeakerRecognitionDataLoader.SaveCustomModelsToFileAsync(this.Models.Where(x => !x.IsPrebuiltModel));
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure creating model");
            }
        }

        private async void OnModelDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            var model = sender is Button button ? (SpeakerRecognitionViewModel)button.DataContext : null;
            if (model != null)
            {
                await Util.ConfirmActionAndExecute("Delete model?", async () => { await DeleteModelAsync(model); });
            }
        }

        private async Task DeleteModelAsync(SpeakerRecognitionViewModel model)
        {
            try
            {
                this.Models.Remove(model);
                await SpeakerRecognitionDataLoader.SaveCustomModelsToFileAsync(this.Models.Where(x => !x.IsPrebuiltModel));

                if (model.IdentificationProfileId != null)
                {
                    await this.userProvidedSpeakerRecognitionService.DeleteProfileAsync(model.IdentificationProfileId, VoiceProfileType.TextIndependentIdentification);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting model");
            }
        }

        #endregion

        private void ToggleUI(bool isProcessing)
        {
            this.startDetectionButton.IsEnabled = !isProcessing;
            if (!isProcessing)
            {
                Pause();
            }
        }

        private void Play(StorageFile file)
        {
            if (file != null)
            {
                this.mediaPlayerElement.Source = MediaSource.CreateFromStorageFile(file);
                this.mediaPlayerElement.MediaPlayer.Play();
            }
        }

        private void Pause()
        {
            this.mediaPlayerElement.MediaPlayer.Pause();
        }

        private async Task ShowMessage(string title, string message)
        {
            await new MessageDialog(message, title).ShowAsync();
        }
    }

    public enum ProcessState
    {
        Overview,
        Processing,
        Completed,
        Error
    }
}
