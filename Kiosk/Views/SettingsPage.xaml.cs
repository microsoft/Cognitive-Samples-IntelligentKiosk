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

using IntelligentKioskSample.Views.DemoLauncher;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    public sealed partial class SettingsPage : Page
    {
        private DemoLauncherConfig demoRotationConfig;

        public SettingsPage()
        {
            this.InitializeComponent();
            this.DataContext = SettingsHelper.Instance;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.versionTextBlock.Text = string.Format("Version: {0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, 
                Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

            this.cameraSourceComboBox.ItemsSource = await Util.GetAvailableDeviceNamesAsync(DeviceClass.VideoCapture);
            this.microphoneSourceComboBox.ItemsSource = await Util.GetAvailableDeviceNamesAsync(DeviceClass.AudioCapture);
            this.cameraSourceComboBox.SelectedItem = SettingsHelper.Instance.CameraName;
            this.microphoneSourceComboBox.SelectedItem = SettingsHelper.Instance.MicrophoneName;
            var cameraRotations = new[]
            {
                Tuple.Create("None", VideoRotation.None),
                Tuple.Create("Clockwise 90 Degrees", VideoRotation.Clockwise90Degrees),
                Tuple.Create("Clockwise 180 Degrees", VideoRotation.Clockwise180Degrees),
                Tuple.Create("Clockwise 270 Degrees", VideoRotation.Clockwise270Degrees)
            };
            this.cameraRotationComboBox.ItemsSource = cameraRotations;
            this.cameraRotationComboBox.SelectedItem = cameraRotations.FirstOrDefault(i => i.Item2 == SettingsHelper.Instance.CameraRotation);
            base.OnNavigatedFrom(e);
        }

        private void OnGenerateNewKeyClicked(object sender, RoutedEventArgs e)
        {
            SettingsHelper.Instance.WorkspaceKey = Guid.NewGuid().ToString();
        }

        private async void OnResetSettingsClick(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("This will reset all the settings and erase your changes. Confirm?",
                async () =>
                {
                    await Task.Run(() => SettingsHelper.Instance.RestoreAllSettings());
                    await new MessageDialog("Settings restored. Please restart the application to load the default settings.").ShowAsync();
                });
        }

        private void OnCameraSourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cameraSourceComboBox.SelectedItem != null)
            {
                SettingsHelper.Instance.CameraName = this.cameraSourceComboBox.SelectedItem.ToString();
            }
        }

        private void OnMicrophoneSourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.microphoneSourceComboBox.SelectedItem != null)
            {
                SettingsHelper.Instance.MicrophoneName = this.microphoneSourceComboBox.SelectedItem.ToString();
            }
        }

        private void OnCameraRotationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cameraRotationComboBox.SelectedItem != null)
            {
                SettingsHelper.Instance.CameraRotation = ((Tuple<string, VideoRotation>)this.cameraRotationComboBox.SelectedItem).Item2;
            }
        }

        private async void KeyTestFlyoutOpened(object sender, object e)
        {
            this.keyTestResultTextBox.Text = "";

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey)
                ? CallApiAndReportResult("Face API Test: ", async () => await CognitiveServiceApiKeyTester.TestFaceApiKeyAsync(
                    SettingsHelper.Instance.FaceApiKey, SettingsHelper.Instance.FaceApiKeyEndpoint))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey)
                ? CallApiAndReportResult("Computer Vision API Test: ", async () => await CognitiveServiceApiKeyTester.TestComputerVisionApiKeyAsync(
                    SettingsHelper.Instance.VisionApiKey, SettingsHelper.Instance.VisionApiKeyEndpoint))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey)
                ? CallApiAndReportResult("Custom Vision Training API Test: ", async () => await CognitiveServiceApiKeyTester.TestCustomVisionTrainingApiKeyAsync(
                    SettingsHelper.Instance.CustomVisionTrainingApiKey, SettingsHelper.Instance.CustomVisionTrainingApiKeyEndpoint))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.BingSearchApiKey)
                ? CallApiAndReportResult("Bing Search API Test: ", async () => await CognitiveServiceApiKeyTester.TestBingSearchApiKeyAsync(SettingsHelper.Instance.BingSearchApiKey))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsKey)
                ? CallApiAndReportResult("Text Analytics API Test: ", async () => await CognitiveServiceApiKeyTester.TestTextAnalyticsApiKeyAsync(
                    SettingsHelper.Instance.TextAnalyticsKey, SettingsHelper.Instance.TextAnalyticsApiKeyEndpoint))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.TranslatorTextApiKey)
                ? CallApiAndReportResult("Translator Text API Test: ", async () => await CognitiveServiceApiKeyTester.TestTranslatorTextApiKeyAsync(SettingsHelper.Instance.TranslatorTextApiKey, SettingsHelper.Instance.TranslatorTextApiRegion))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.AnomalyDetectorApiKey)
                ? CallApiAndReportResult("Anomaly Detector API Test: ", async () => await CognitiveServiceApiKeyTester.TestAnomalyDetectorApiKeyAsync(SettingsHelper.Instance.AnomalyDetectorApiKey, SettingsHelper.Instance.AnomalyDetectorKeyEndpoint))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiKey)
                ? CallApiAndReportResult("Speech API Test: ", async () => await CognitiveServiceApiKeyTester.TestSpeechApiKeyAsync(
                    SettingsHelper.Instance.SpeechApiKey, SettingsHelper.Instance.SpeechApiEndpoint))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.FormRecognizerApiKey)
                ? CallApiAndReportResult("Form Recognizer API Test: ", async () => await CognitiveServiceApiKeyTester.TestFormRecognizerApiKeyAsync(
                    SettingsHelper.Instance.FormRecognizerApiKey, SettingsHelper.Instance.FormRecognizerApiKeyEndpoint))
                : Task.CompletedTask);
        }

        private async Task CallApiAndReportResult(string testName, Func<Task> testTask)
        {
            try
            {
                this.keyTestResultTextBox.Text += testName;
                await testTask();
                this.keyTestResultTextBox.Text += "Passed!\n\n";
            }
            catch (Exception ex)
            {
                this.keyTestResultTextBox.Text += string.Format("Failed! Error message: \"{0}\"\n\n", Util.GetMessageFromException(ex));
            }
        }

        private async void DemoAvailabilityFlyoutOpened(object sender, object e)
        {
            await LoadDemoAvailability();
        }

        private async void ApplyDemoAvailability(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await SaveDemoAvailability(this.demoAvailabilityListView.DataContext as DemoLauncherConfig);
            DemoAvailabilityFlyout.Hide();
        }

        private async Task LoadDemoAvailability(bool forceLoad = false)
        {
            if (this.demoAvailabilityListView.ItemsSource == null || forceLoad)
            {
                this.demoAvailabilityListView.DataContext = await DemoLauncherPage.LoadDemoLauncherConfigFromFile("DemoLauncherConfig.xml");
            }
        }

        private async Task SaveDemoAvailability(DemoLauncherConfig config)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DemoLauncherConfig));
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("DemoLauncherConfig.xml", CreationCollisionOption.ReplaceExisting);

            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                serializer.Serialize(stream, config);
            }
        }

        private async void RestoreDemoAvailability(object sender, RoutedEventArgs e)
        {
            var config = this.demoAvailabilityListView.DataContext as DemoLauncherConfig;
            if (config != null)
            {
                config.Entries.ForEach(d => d.Enabled = true);
                await SaveDemoAvailability(config);
                await LoadDemoAvailability(forceLoad: true);
            }
        }

        private async void DemoRotationFlyoutClosed(object sender, object e)
        {
            this.demoRotationEntriesListView.ItemsSource = null;

            // Persist to file
            XmlSerializer serializer = new XmlSerializer(typeof(DemoLauncherConfig));
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("DemoRotationConfig.xml", CreationCollisionOption.ReplaceExisting);

            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                serializer.Serialize(stream, this.demoRotationConfig);
            }

            // Reset the scheduler
            DemoRotationScheduler.Instance.Start();
        }

        private async void DemoRotationFlyoutOpened(object sender, object e)
        {
            // Load from file
            this.demoRotationConfig = await LoadDemoRotationConfigFromFileAsync();

            this.UpdateDemoRotationEntriesUI();
        }

        private void UpdateDemoRotationEntriesUI()
        {
            if (this.demoRotationConfig == null)
            {
                return;
            }

            var entries = this.demoRotationConfig.Entries.OrderBy(entry => entry.KioskExperience.Attributes.DisplayName).ToList();

            if (this.handsFreeDemoRotationCheckBox.IsChecked.Value)
            {
                entries = entries.Where(d => (d.KioskExperience.Attributes.ExperienceType & ExperienceType.Automated) == ExperienceType.Automated).ToList();
            }

            this.demoRotationEntriesListView.ItemsSource = entries;
        }

        public static async Task<DemoLauncherConfig> LoadDemoRotationConfigFromFileAsync()
        {
            return await DemoLauncherPage.LoadDemoLauncherConfigFromFile("DemoRotationConfig.xml", enableDemosByDefault: false);
        }

        private void AutoRotateToggleChanged(object sender, RoutedEventArgs e)
        {
            if (!this.autoRotateDemosToggle.IsOn)
            {
                DemoRotationScheduler.Instance.Stop();
            }
        }

        private void HandsFreeDemoRotationCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateDemoRotationEntriesUI();
        }
    }

    public class DemoIdToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string demoId = (string)value;
            KioskExperience matchingExp = KioskExperiences.Experiences.FirstOrDefault(exp => exp.Attributes.Id == demoId);
            if (matchingExp != null)
            {
                return matchingExp.Attributes.DisplayName;
            }

            return demoId;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Not used
            string demoDisplayName = (string)value;
            KioskExperience matchingExp = KioskExperiences.Experiences.FirstOrDefault(exp => exp.Attributes.DisplayName == demoDisplayName);
            if (matchingExp != null)
            {
                return matchingExp.Attributes.Id;
            }

            return demoDisplayName;
        }
    }
}
