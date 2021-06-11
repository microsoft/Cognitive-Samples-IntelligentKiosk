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
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample
{
    using IntelligentKioskSample.Views.DemoLauncher;
    using ServiceHelpers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Views;
    using Windows.Data.Xml.Dom;
    using Windows.UI.Notifications;
    using Windows.UI.Popups;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += App_UnhandledException;
        }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // handle exceptions so that we dont crash
            e.Handled = true;
            await new MessageDialog("Error:" + e.Message, "An unhandled error occurred").ShowAsync();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // This just gets in the way.
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            AppShell shell = Window.Current.Content as AppShell;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (shell == null)
            {
                // propogate settings to the core library
                SettingsHelper.Instance.SettingsChanged += (target, args) =>
                {
                    FaceServiceHelper.ApiKey = SettingsHelper.Instance.FaceApiKey;
                    FaceServiceHelper.ApiEndpoint = SettingsHelper.Instance.FaceApiKeyEndpoint;
                    VisionServiceHelper.ApiKey = SettingsHelper.Instance.VisionApiKey;
                    VisionServiceHelper.ApiEndpoint = SettingsHelper.Instance.VisionApiKeyEndpoint;
                    BingSearchHelper.SearchApiKey = SettingsHelper.Instance.BingSearchApiKey;
                    TextAnalyticsHelper.ApiKey = SettingsHelper.Instance.TextAnalyticsKey;
                    TextAnalyticsHelper.ApiEndpoint = SettingsHelper.Instance.TextAnalyticsApiKeyEndpoint;
                    TextAnalyticsHelper.ApiKey = SettingsHelper.Instance.TextAnalyticsKey;
                    ImageAnalyzer.PeopleGroupsUserDataFilter = SettingsHelper.Instance.WorkspaceKey;
                    FaceListManager.FaceListsUserDataFilter = SettingsHelper.Instance.WorkspaceKey;
                    CoreUtil.MinDetectableFaceCoveragePercentage = SettingsHelper.Instance.MinDetectableFaceCoveragePercentage;
                    AnomalyDetectorHelper.ApiKey = SettingsHelper.Instance.AnomalyDetectorApiKey;
                    AnomalyDetectorHelper.Endpoint = SettingsHelper.Instance.AnomalyDetectorKeyEndpoint;
                    ReceiptOCRHelper.ApiKey = SettingsHelper.Instance.FormRecognizerApiKey;
                    ReceiptOCRHelper.ApiEndpoint = SettingsHelper.Instance.FormRecognizerApiKeyEndpoint;
                };

                // callbacks for core library
                FaceServiceHelper.Throttled = () => ShowToastNotification("The Face API is throttling your requests. Consider upgrading to a Premium Key.");
                VisionServiceHelper.Throttled = () => ShowToastNotification("The Vision API is throttling your requests. Consider upgrading to a Premium Key.");
                ErrorTrackingHelper.TrackException = (ex, msg) => LogException(ex, msg);
                ErrorTrackingHelper.GenericApiCallExceptionHandler = Util.GenericApiCallExceptionHandler;

                SettingsHelper.Instance.Initialize();

                // Create a AppShell to act as the navigation context and navigate to the first page
                shell = new AppShell();

                // Set the default language
                shell.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                shell.AppFrame.NavigationFailed += OnNavigationFailed;
            }

            // Place our app shell in the current Window
            Window.Current.Content = shell;

            if (shell.AppFrame.Content == null)
            {
                // When the navigation stack isn't restored, navigate to the first page
                // suppressing the initial entrance animation.
                shell.AppFrame.Navigate(typeof(DemoLauncherPage), e.Arguments, new Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
            }

            // Ensure the current window is active
            Window.Current.Activate();

            // Trigger a test of the api keys in the background to alert the user if any of them are bad (e.g. expired, out of quota, etc)
            TestApiKeysAsync();
        }

        private static async void TestApiKeysAsync()
        {
            List<Task> testTasks = new List<Task>
            {
                !string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey)
                ? CognitiveServiceApiKeyTester.TestFaceApiKeyAsync(SettingsHelper.Instance.FaceApiKey, SettingsHelper.Instance.FaceApiKeyEndpoint)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey)
                ? CognitiveServiceApiKeyTester.TestComputerVisionApiKeyAsync(SettingsHelper.Instance.VisionApiKey, SettingsHelper.Instance.VisionApiKeyEndpoint)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.CustomVisionTrainingApiKey)
                ? CognitiveServiceApiKeyTester.TestCustomVisionTrainingApiKeyAsync(SettingsHelper.Instance.CustomVisionTrainingApiKey, SettingsHelper.Instance.CustomVisionTrainingApiKeyEndpoint)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.BingSearchApiKey)
                ? CognitiveServiceApiKeyTester.TestBingSearchApiKeyAsync(SettingsHelper.Instance.BingSearchApiKey)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.TextAnalyticsKey)
                ? CognitiveServiceApiKeyTester.TestTextAnalyticsApiKeyAsync(SettingsHelper.Instance.TextAnalyticsKey, SettingsHelper.Instance.TextAnalyticsApiKeyEndpoint)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.TranslatorTextApiKey)
                ? CognitiveServiceApiKeyTester.TestTranslatorTextApiKeyAsync(SettingsHelper.Instance.TranslatorTextApiKey, SettingsHelper.Instance.TranslatorTextApiRegion)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.AnomalyDetectorApiKey)
                ? CognitiveServiceApiKeyTester.TestAnomalyDetectorApiKeyAsync(SettingsHelper.Instance.AnomalyDetectorApiKey, SettingsHelper.Instance.AnomalyDetectorKeyEndpoint)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.SpeechApiKey)
                ? CognitiveServiceApiKeyTester.TestSpeechApiKeyAsync(SettingsHelper.Instance.SpeechApiKey, SettingsHelper.Instance.SpeechApiEndpoint)
                : Task.CompletedTask,

                !string.IsNullOrEmpty(SettingsHelper.Instance.FormRecognizerApiKey)
                ? CognitiveServiceApiKeyTester.TestFormRecognizerApiKeyAsync(SettingsHelper.Instance.FormRecognizerApiKey, SettingsHelper.Instance.FormRecognizerApiKeyEndpoint)
                : Task.CompletedTask,
            };

            try
            {
                await Task.WhenAll(testTasks);
            }
            catch (Exception)
            {
                ShowToastNotification("Failure validating your API Keys. Please run the Key Validation Test in the Settings Page for more details.");
            }
        }

        private static void ShowToastNotification(string errorMessage)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastText02;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode("Intelligent Kiosk Sample"));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(errorMessage));

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private static void LogException(Exception ex, string message)
        {
            Debug.WriteLine("Error detected! Exception: \"{0}\", More info: \"{1}\".", ex.Message, message);
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            //Save application state and stop any background activity
            var currentView = (Window.Current.Content as AppShell)?.AppFrame?.Content;

            if (currentView != null && currentView.GetType() == typeof(RealTimeDemo))
            {
                await (currentView as RealTimeDemo).HandleApplicationShutdownAsync();
            }

            deferral.Complete();
        }
    }
}
