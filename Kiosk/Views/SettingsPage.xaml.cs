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

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.DataContext = SettingsHelper.Instance;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.versionTextBlock.Text = string.Format("Version: {0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, 
                Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

            this.cameraSourceComboBox.ItemsSource = await Util.GetAvailableCameraNamesAsync();
            this.cameraSourceComboBox.SelectedItem = SettingsHelper.Instance.CameraName;
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

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.BingAutoSuggestionApiKey)
                ? CallApiAndReportResult("Bing Auto Suggestion API Test: ", async () => await CognitiveServiceApiKeyTester.TestBingAutosuggestApiKeyAsync(SettingsHelper.Instance.BingAutoSuggestionApiKey))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsKey)
                ? CallApiAndReportResult("Text Analytics API Test: ", async () => await CognitiveServiceApiKeyTester.TestTextAnalyticsApiKeyAsync(
                    SettingsHelper.Instance.TextAnalyticsKey, SettingsHelper.Instance.TextAnalyticsApiKeyEndpoint))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.TranslatorTextApiKey)
                ? CallApiAndReportResult("Translator Text API Test: ", async () => await CognitiveServiceApiKeyTester.TestTranslatorTextApiKeyAsync(SettingsHelper.Instance.TranslatorTextApiKey))
                : Task.CompletedTask);

            await (!string.IsNullOrEmpty(SettingsHelper.Instance.AnomalyDetectorApiKey)
                ? CallApiAndReportResult("Anomaly Detector API Test: ", async () => await CognitiveServiceApiKeyTester.TestAnomalyDetectorApiKeyAsync(SettingsHelper.Instance.AnomalyDetectorApiKey, SettingsHelper.Instance.AnomalyDetectorKeyEndpoint))
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
