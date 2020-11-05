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
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Views.SpeakerRecognition
{
    public sealed partial class SpeakerRecognitionModelSetup : UserControl, INotifyPropertyChanged
    {
        private SpeechConfig speechConfig;
        private SpeakerRecognitionService speakerRecognitionService;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<StorageFile> LocalFileCollection { get; private set; } = new ObservableCollection<StorageFile>();

        private ModelSetupState modelSetupState = ModelSetupState.NotStarted;
        public ModelSetupState ModelSetupState
        {
            get { return modelSetupState; }
            set
            {
                modelSetupState = value;
                NotifyPropertyChanged("ModelSetupState");
            }
        }

        private SourceType sourceType = SourceType.FromFile;
        public SourceType SourceType
        {
            get { return sourceType; }
            set
            {
                if (sourceType != value)
                {
                    sourceType = value;
                    NotifyPropertyChanged("SourceType");
                    SourceTypeChanged();
                }
            }
        }

        public event EventHandler<SpeakerRecognitionViewModel> ModelCreated;

        public SpeakerRecognitionModelSetup()
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.LocalFileCollection.CollectionChanged += LocalFileCollectionChanged;
        }

        public void OpenScenarioSetupForm(SpeakerRecognitionService speakerRecognitionService, SpeechConfig speechConfig)
        {
            this.hostGrid.Height = Window.Current.Bounds.Height;
            this.hostGrid.Width = Window.Current.Bounds.Width;

            this.speakerRecognitionService = speakerRecognitionService;
            this.speechConfig = speechConfig;

            this.newModelForm.IsOpen = true;
        }

        private void OnCloseModelFormButtonClicked(object sender, RoutedEventArgs e)
        {
            LocalFileCollection.Clear();
            this.modelNameTextBox.Text = string.Empty;
            ModelSetupState = ModelSetupState.NotStarted;

            this.newModelForm.IsOpen = false;
        }

        private void SourceTypeChanged()
        {
            ValidateSetupForm();
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateSetupForm();
        }

        private void FileListView_OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void FileListView_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var storageItems = await e.DataView.GetStorageItemsAsync();
                LocalFileCollection.Add(storageItems.Select(x => (StorageFile)x).FirstOrDefault());
            }
        }

        public void LocalFileCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ValidateSetupForm();
        }

        private async void OnBrowseFilesButtonClicked(object sender, RoutedEventArgs e)
        {
            StorageFile selectedFile = await Util.PickSingleFileAsync(SpeakerRecognitionExplorer.AudioExtensions);
            if (selectedFile != null)
            {
                LocalFileCollection.Add(selectedFile);
            }
        }

        private void OnDeleteFileButtonClicked(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (button?.DataContext is StorageFile curFile)
            {
                LocalFileCollection.Remove(curFile);
            }
        }

        private async void OnCreateModelButtonClicked(object sender, RoutedEventArgs e)
        {
            var model = await CreateNewModelAsync(SourceType);
            if (model != null)
            {
                this.ModelCreated?.Invoke(this, model);
            }
        }

        private async Task<SpeakerRecognitionViewModel> CreateNewModelAsync(SourceType sourceType)
        {
            StorageFile audioFileCopy = null;
            try
            {
                ModelSetupState = ModelSetupState.Running;

                string identificationModelId = string.Empty;
                switch (sourceType)
                {
                    case SourceType.FromFile:
                        StorageFile audioFile = LocalFileCollection.FirstOrDefault();
                        audioFileCopy = await audioFile.CopyAsync(ApplicationData.Current.TemporaryFolder, audioFile.Name, NameCollisionOption.GenerateUniqueName);
                        identificationModelId = await CreateRecognitionModelFromFileAsync(audioFileCopy, speechConfig, VoiceProfileType.TextIndependentIdentification);
                        break;

                    case SourceType.FromMicrophone:
                        identificationModelId = await CreateRecognitionModelFromMicrophoneAsync(speechConfig);
                        break;
                }

                if (!string.IsNullOrEmpty(identificationModelId))
                {
                    ModelSetupState = ModelSetupState.Completed;
                    return new SpeakerRecognitionViewModel()
                    {
                        Id = Guid.NewGuid(),
                        Name = this.modelNameTextBox.Text,
                        IdentificationProfileId = identificationModelId
                    };
                }
                else
                {
                    ModelSetupState = ModelSetupState.Error;
                }
            }
            catch (Exception)
            {
                ModelSetupState = ModelSetupState.Error;
            }
            finally
            {
                if (audioFileCopy != null)
                {
                    await audioFileCopy.DeleteAsync();
                }
            }

            return null;
        }

        public async Task<string> CreateRecognitionModelFromFileAsync(StorageFile audioFile, SpeechConfig config, VoiceProfileType voiceProfileType)
        {
            using (var audioInput = AudioConfig.FromWavFileInput(audioFile.Path))
            {
                return await EnrollProfileAsync(config, audioInput, voiceProfileType);
            }
        }

        public async Task<string> CreateRecognitionModelFromMicrophoneAsync(SpeechConfig config)
        {
            using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
            {
                return await EnrollProfileAsync(config, audioInput, VoiceProfileType.TextIndependentIdentification);
            }
        }

        private async Task<string> EnrollProfileAsync(SpeechConfig config, AudioConfig audioInput, VoiceProfileType voiceProfileType)
        {
            ClearTextMessages();

            using (var client = new VoiceProfileClient(config))
            {
                VoiceProfile profile = await client.CreateProfileAsync(voiceProfileType, "en-us");
                AddTextMessageToDisplay($"Enrolling identification profile id {profile.Id}.");

                VoiceProfileEnrollmentResult result = null;
                int remainingSeconds = 0;
                while (result is null || result.RemainingEnrollmentsSpeechLength > TimeSpan.Zero)
                {
                    result = await client.EnrollProfileAsync(profile, audioInput);
                    remainingSeconds = result.RemainingEnrollmentsSpeechLength.HasValue ? (int)result.RemainingEnrollmentsSpeechLength.Value.TotalSeconds : 0;
                    AddTextMessageToDisplay($"Remaining identification enrollment audio time needed: {remainingSeconds} sec");
                }

                if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = VoiceProfileEnrollmentCancellationDetails.FromResult(result);
                    AddTextMessageToDisplay($"CANCELED {profile.Id}: ErrorCode={cancellation.ErrorCode} ErrorDetails={cancellation.ErrorDetails}");

                    await this.speakerRecognitionService.DeleteProfileAsync(profile.Id, voiceProfileType);
                }

                if (result.Reason == ResultReason.EnrolledVoiceProfile)
                {
                    return profile.Id;
                }

                return null;
            }
        }

        private void AddTextMessageToDisplay(string message)
        {
            this.processTextBlock.Text += message + "\n\n";
        }

        private void ClearTextMessages()
        {
            this.processTextBlock.Text = string.Empty;
        }

        private void OnTryAgainButtonClicked(object sender, RoutedEventArgs e)
        {
            ModelSetupState = ModelSetupState.NotStarted;
            ValidateSetupForm();
        }

        private void ValidateSetupForm()
        {
            bool isValidModelName = !string.IsNullOrEmpty(this.modelNameTextBox.Text);
            bool isValidForm = SourceType == SourceType.FromFile ? LocalFileCollection.Any() && isValidModelName : isValidModelName;
            this.createModelButton.IsEnabled = isValidForm;
        }


        private void OnPopUpOpened(object sender, object e)
        {
            Window.Current.SizeChanged += WindowSizeChanged;
        }

        private void OnPopUpClosed(object sender, object e)
        {
            Window.Current.SizeChanged -= WindowSizeChanged;
        }

        private void WindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            this.hostGrid.Height = e.Size.Height;
            this.hostGrid.Width = e.Size.Width;
        }

        private void OnSourceTypeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                string tagName = rb.Tag.ToString();
                switch (tagName)
                {
                    case "FromFile":
                        SourceType = SourceType.FromFile;
                        break;
                    case "FromMicrophone":
                        SourceType = SourceType.FromMicrophone;
                        break;
                }
            }
        }
    }

    public enum ModelSetupState
    {
        NotStarted,
        Running,
        Completed,
        Error
    }

    public enum SourceType
    {
        FromFile,
        FromMicrophone
    }
}
