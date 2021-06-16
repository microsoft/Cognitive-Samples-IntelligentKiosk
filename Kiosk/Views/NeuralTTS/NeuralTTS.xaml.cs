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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.NeuralTTS
{
    [KioskExperience(Id = "NeuralTTS",
        DisplayName = "Neural Text To Speech",
        Description = "Hear a male or female voice speak any text you provide",
        ImagePath = "ms-appx:/Assets/DemoGallery/Neural Text To Speech.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.TextToSpeech,
        TechnologyArea = TechnologyAreaType.Speech,
        DateAdded = "2021/06/15")]
    public sealed partial class NeuralTTSPage : Page
    {
        private const int MaxTimeout = 6000000;
        private const string SSMLUserAgent = "NeuralTTSClient";
        private const string SSMLContentType = "application/ssml+xml";
        private const string AudioOutputFormatName = "X-MICROSOFT-OutputFormat";
        private const string AudioOutputFormatValue = "riff-16khz-16bit-mono-pcm";
        private const string EndpointTemplate = "https://{0}.tts.speech.microsoft.com/cognitiveservices/v1";
        private const string EndpointPattern = "wss://(.*).stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";
        private const string SSMLTemplate = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{0}'><voice name='{1}'>{2}</voice></speak>";

        private string apiRegion = string.Empty;
        private StorageFolder cacheDataFolder;

        /// <summary>
        /// NOTE: Neural voices
        /// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#neural-voices
        /// </summary>
        public ObservableCollection<VoiceInfo> AvailableVoices { get; } = new ObservableCollection<VoiceInfo>();

        public ObservableCollection<CachedResult> CachedResults { get; } = new ObservableCollection<CachedResult>();

        public NeuralTTSPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // check Speech API keys
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiKey) && !string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiEndpoint))
            {
                this.speakButton.IsEnabled = true;
                this.apiRegion = Regex.Replace(SettingsHelper.Instance.SpeechApiEndpoint, EndpointPattern, "$1");
                this.cacheDataFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("NeuralTTSDemo\\Cache", CreationCollisionOption.OpenIfExists);

                AvailableVoices.AddRange(NeuralTTSDataLoader.GetNeuralVoices());
                CachedResults.AddRange(await NeuralTTSDataLoader.GetCachedResults(cacheDataFolder));
            }
            else
            {
                this.speakButton.IsEnabled = false;
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

            base.OnNavigatedTo(e);
        }

        private async void SpeakButtonClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.inputTextBox.Text) || voiceComboBox.SelectedValue == null)
            {
                await new MessageDialog("Please type some text and select a voice first.").ShowAsync();
                return;
            }

            try
            {
                this.progressRing.IsActive = true;
                await Speak(this.inputTextBox.Text, voiceComboBox.SelectedValue as VoiceInfo);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error processing your request.");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private async Task Speak(string text, VoiceInfo voice)
        {
            Authentication authentication = new Authentication(SettingsHelper.Instance.SpeechApiKey, this.apiRegion);
            string tokenString = await authentication.RetrieveNewTokenAsync();
            string voiceUrl = string.Format(EndpointTemplate, this.apiRegion);

            WebRequest webRequest = WebRequest.Create(voiceUrl);
            webRequest.ContentType = SSMLContentType;
            webRequest.Headers.Add(AudioOutputFormatName, AudioOutputFormatValue);
            webRequest.Headers.Add("User-Agent", SSMLUserAgent);
            webRequest.Headers["Authorization"] = "Bearer " + tokenString;
            webRequest.Timeout = MaxTimeout;
            webRequest.Method = "POST";

            string ssml = string.Format(SSMLTemplate, voice.Locale, voice.Name, text);
            byte[] btBodys = Encoding.UTF8.GetBytes(ssml);

            webRequest.ContentLength = btBodys.Length;
            webRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

            string uniqueName = Guid.NewGuid().ToString();
            StorageFile audioFile = await this.cacheDataFolder.CreateFileAsync(uniqueName + ".wav");

            WebResponse httpWebResponse = await webRequest.GetResponseAsync();
            using (Stream stream = httpWebResponse.GetResponseStream())
            {
                using (var outputStream = await audioFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await stream.CopyToAsync(outputStream.AsStreamForWrite());
                    await outputStream.FlushAsync();
                }
            }

            StorageFile metadataFile = await this.cacheDataFolder.CreateFileAsync(uniqueName + ".json");
            var cachedResult = new CachedResult { Text = text, VoiceId = voice.Id };

            using (var outoutStream = await metadataFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                string json = JsonConvert.SerializeObject(cachedResult, Formatting.Indented);
                StreamWriter writer = new StreamWriter(outoutStream.AsStreamForWrite());
                await writer.WriteAsync(json);
                writer.Close();
            }

            // Add result to cache list and play it
            cachedResult.AudioFilePath = audioFile.Path;
            cachedResult.MetadataFilePath = metadataFile.Path;
            CachedResults.Insert(0,cachedResult);

            PlayAudioFile(audioFile.Path);
        }

        private void PlayCachedResultButtonClicked(object sender, RoutedEventArgs e)
        {
            var dataContext = (sender as FrameworkElement).DataContext as CachedResult;
            PlayAudioFile(dataContext.AudioFilePath);
        }

        private void PlayAudioFile(string audioFilePath)
        {
            this.mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri(audioFilePath));
            this.mediaPlayerElement.MediaPlayer.Play();
        }

        private async void DeleteCachedResultButtonClicked(object sender, RoutedEventArgs e)
        {
            var dataContext = (sender as FrameworkElement).DataContext as CachedResult;
            await DeleteCachedResult(dataContext);
        }

        private async Task DeleteCachedResult(CachedResult dataContext)
        {
            try
            {
                var audioFile = await StorageFile.GetFileFromPathAsync(dataContext.AudioFilePath);
                await audioFile.DeleteAsync();
            }
            catch { }

            try
            {
                var metadataFile = await StorageFile.GetFileFromPathAsync(dataContext.MetadataFilePath);
                await metadataFile.DeleteAsync();
            }
            catch { }

            CachedResults.Remove(dataContext);
        }

        private async void ClearAllCachedResultsButtonClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete all cached results?", async () =>
            {
                foreach (CachedResult item in CachedResults.ToArray())
                {
                    await DeleteCachedResult(item);
                }
            });
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (voiceComboBox.Items.Any())
            {
                voiceComboBox.SelectedIndex = 0;
            }
        }
    }
}
