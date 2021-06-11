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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Media.Capture;
using Windows.Storage;

namespace IntelligentKioskSample
{
    internal class SettingsHelper : INotifyPropertyChanged
    {
        public static readonly string CustomEndpointName = "Custom";
        public static readonly string GlobalRegionName = "Global";
        public static readonly string DefaultTranslatorTextApiRegion = "westus2";
        public static readonly string DefaultApiEndpoint = "https://westus.api.cognitive.microsoft.com";
        public static readonly string DefaultCustomVisionApiEndpoint = "https://southcentralus.api.cognitive.microsoft.com";
        public static readonly string DefaultFormRecognizerApiEndpoint = "https://westus2.api.cognitive.microsoft.com";
        public static readonly string DefaultSpeechApiEndpoint = "wss://westus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";

        public static readonly KeyValuePair<string, string>[] AvailableApiRegions = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string,string>("West US", "westus"),
            new KeyValuePair<string,string>("West US 2", "westus2"),
            new KeyValuePair<string,string>("East US", "eastus"),
            new KeyValuePair<string,string>("East US 2", "eastus2"),
            new KeyValuePair<string,string>("Central US", "centralus"),
            new KeyValuePair<string,string>("North Central US", "northcentralus"),
            new KeyValuePair<string,string>("South Central US", "southcentralus"),
            new KeyValuePair<string,string>("West Central US", "westcentralus"),
            new KeyValuePair<string,string>("Canada Central", "canadacentral"),
            new KeyValuePair<string,string>("Central India", "centralindia"),
            new KeyValuePair<string,string>("East Asia", "eastasia"),
            new KeyValuePair<string,string>("Southeast Asia", "southeastasia"),
            new KeyValuePair<string,string>("Japan East", "japaneast"),
            new KeyValuePair<string,string>("Japan West", "japanwest"),
            new KeyValuePair<string,string>("Korea Central", "koreacentral"),
            new KeyValuePair<string,string>("North Europe", "northeurope"),
            new KeyValuePair<string,string>("West Europe", "westeurope"),
            new KeyValuePair<string,string>("UK South", "uksouth"),
            new KeyValuePair<string,string>("France Central", "francecentral"),
            new KeyValuePair<string,string>("Brazil South", "brazilsouth"),
            new KeyValuePair<string,string>("Australia East", "australiaeast"),
            new KeyValuePair<string,string>("South Africa North", "southafricanorth"),
            new KeyValuePair<string,string>("UAE North", "uaenorth"),
        };

        public event EventHandler SettingsChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private static SettingsHelper instance;

        static SettingsHelper()
        {
            instance = new SettingsHelper();
        }

        public void Initialize()
        {
            LoadRoamingSettings();
            ApplicationData.Current.DataChanged += RoamingDataChanged;

            List<string> pageList = new List<string>();
            pageList.Add("Demo Gallery");
            pageList.AddRange(KioskExperiences.Experiences.Select(e => e.Attributes.Id));
            this.startingPageNames = pageList.ToArray();
        }

        private void RoamingDataChanged(ApplicationData sender, object args)
        {
            LoadRoamingSettings();
            instance.OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            instance.SettingsChanged?.Invoke(instance, EventArgs.Empty);
        }

        private async void OnSettingChanged(string propertyName, object value)
        {
            if (propertyName == "MallKioskDemoCustomSettings")
            {
                // save to file as the content is too big to be saved as a string-like setting
                StorageFile file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(
                    "MallKioskDemoCustomSettings.xml",
                    CreationCollisionOption.ReplaceExisting);

                using (Stream stream = await file.OpenStreamForWriteAsync())
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(value.ToString());
                    }
                }
            }
            else
            {
                ApplicationData.Current.RoamingSettings.Values[propertyName] = value;
            }

            instance.OnSettingsChanged();
            instance.OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            instance.PropertyChanged?.Invoke(instance, new PropertyChangedEventArgs(propertyName));
        }

        public static SettingsHelper Instance
        {
            get
            {
                return instance;
            }
        }

        private void LoadFaceRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["FaceApiKey"];
            if (value != null)
            {
                this.FaceApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["FaceApiKeyEndpoint"];
            if (value == null && ApplicationData.Current.RoamingSettings.Values["FaceApiKeyRegion"] != null)
            {
                var faceApiRegion = ApplicationData.Current.RoamingSettings.Values["FaceApiKeyRegion"].ToString();
                value = GetRegionEndpoint(faceApiRegion);
            }
            if (value != null)
            {
                this.FaceApiKeyEndpoint = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomFaceApiEndpoint"];
            if (value != null)
            {
                this.CustomFaceApiEndpoint = value.ToString();
            }
        }

        private void LoadComputerVisionRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["VisionApiKey"];
            if (value != null)
            {
                this.VisionApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["VisionApiKeyEndpoint"];
            if (value == null && ApplicationData.Current.RoamingSettings.Values["VisionApiKeyRegion"] != null)
            {
                var visionApiRegion = ApplicationData.Current.RoamingSettings.Values["VisionApiKeyRegion"].ToString();
                value = GetRegionEndpoint(visionApiRegion);
            }
            if (value != null)
            {
                this.VisionApiKeyEndpoint = value.ToString();
            }
        }

        private void LoadBingRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["BingSearchApiKey"];
            if (value != null)
            {
                this.BingSearchApiKey = value.ToString();
            }
        }

        private void LoadSpeechRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["SpeechApiKey"];
            if (value != null)
            {
                this.SpeechApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["SpeechApiEndpoint"];
            if (value != null)
            {
                this.SpeechApiEndpoint = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomSpeechApiEndpoint"];
            if (value != null)
            {
                this.CustomSpeechApiEndpoint = value.ToString();
            }
        }

        private void LoadAppRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["WorkspaceKey"];
            if (value != null)
            {
                this.WorkspaceKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["StartingPage"];
            if (value != null)
            {
                this.StartingPage = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CameraName"];
            if (value != null)
            {
                this.CameraName = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CameraRotation"];
            if (value != null)
            {
                object rotationValue;
                if (Enum.TryParse(typeof(VideoRotation), value.ToString(), out rotationValue))
                {
                    this.CameraRotation = (VideoRotation)rotationValue;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["MicrophoneName"];
            if (value != null)
            {
                this.MicrophoneName = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["HowOldKioskResultDisplayDuration"];
            if (value != null)
            {
                int duration;
                if (int.TryParse(value.ToString(), out duration))
                {
                    this.HowOldKioskResultDisplayDuration = duration;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["MinDetectableFaceCoveragePercentage"];
            if (value != null)
            {
                uint size;
                if (uint.TryParse(value.ToString(), out size))
                {
                    this.MinDetectableFaceCoveragePercentage = size;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["ShowDebugInfo"];
            if (value != null)
            {
                bool booleanValue;
                if (bool.TryParse(value.ToString(), out booleanValue))
                {
                    this.ShowDebugInfo = booleanValue;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["StartupFullScreenMode"];
            if (value != null)
            {
                this.StartupFullScreenMode = (bool)value;
            }

            value = ApplicationData.Current.RoamingSettings.Values["ShowAgeAndGender"];
            if (value != null)
            {
                if (bool.TryParse(value.ToString(), out bool booleanValue))
                {
                    this.ShowAgeAndGender = booleanValue;
                }
            }
        }

        private void LoadTextAnalyticsRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["TextAnalyticsKey"];
            if (value != null)
            {
                this.TextAnalyticsKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["TextAnalyticsApiKeyEndpoint"];
            if (value == null && ApplicationData.Current.RoamingSettings.Values["TextAnalyticsApiKeyRegion"] != null)
            {
                var textAnalyticsApiRegion = ApplicationData.Current.RoamingSettings.Values["TextAnalyticsApiKeyRegion"].ToString();
                value = GetRegionEndpoint(textAnalyticsApiRegion);
            }
            if (value != null)
            {
                this.TextAnalyticsApiKeyEndpoint = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomTextAnalyticsEndpoint"];
            if (value != null)
            {
                this.CustomTextAnalyticsEndpoint = value.ToString();
            }
        }

        private void LoadCustomVisionRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["CustomVisionPredictionApiKey"];
            if (value != null)
            {
                this.CustomVisionPredictionApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionPredictionApiKeyEndpoint"];
            if (value != null)
            {
                this.CustomVisionPredictionApiKeyEndpoint = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionPredictionResourceId"];
            if (value != null)
            {
                this.CustomVisionPredictionResourceId = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionTrainingApiKey"];
            if (value != null)
            {
                this.CustomVisionTrainingApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionTrainingApiKeyEndpoint"];
            if (value != null)
            {
                this.CustomVisionTrainingApiKeyEndpoint = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomComputerVisionApiEndpoint"];
            if (value != null)
            {
                this.CustomComputerVisionApiEndpoint = value.ToString();
            }
        }

        private void LoadTranslatorRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["TranslatorTextApiKey"];
            if (value != null)
            {
                this.TranslatorTextApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["TranslatorTextApiRegion"];
            if (value != null)
            {
                this.TranslatorTextApiRegion = value.ToString();
            }
        }

        public void LoadAnomalyDetectorRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["AnomalyDetectorApiKey"];
            if (value != null)
            {
                this.AnomalyDetectorApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["AnomalyDetectorKeyEndpoint"];
            if (value == null && ApplicationData.Current.RoamingSettings.Values["AnomalyDetectorApiKeyRegion"] != null)
            {
                var anomalyDetectorApiRegion = ApplicationData.Current.RoamingSettings.Values["AnomalyDetectorApiKeyRegion"].ToString();
                value = GetRegionEndpoint(anomalyDetectorApiRegion);
            }
            if (value != null)
            {
                this.AnomalyDetectorKeyEndpoint = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomAnomalyDetectorKeyEndpoint"];
            if (value != null)
            {
                this.CustomAnomalyDetectorApiEndpoint = value.ToString();
            }
        }

        public void LoadFormRecognizerRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["FormRecognizerApiKey"];
            if (value != null)
            {
                this.FormRecognizerApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["FormRecognizerApiKeyEndpoint"];
            if (value != null)
            {
                this.FormRecognizerApiKeyEndpoint = value.ToString();
            }
        }

        private async void LoadRoamingSettings()
        {
            LoadAppRoamingSettings();
            LoadFaceRoamingSettings();
            LoadComputerVisionRoamingSettings();
            LoadBingRoamingSettings();
            LoadSpeechRoamingSettings();
            LoadTextAnalyticsRoamingSettings();
            LoadCustomVisionRoamingSettings();
            LoadTranslatorRoamingSettings();
            LoadAnomalyDetectorRoamingSettings();
            LoadFormRecognizerRoamingSettings();

            object value = ApplicationData.Current.RoamingSettings.Values["AutoRotateThroughDemos"];
            if (value != null)
            {
                bool booleanValue;
                if (bool.TryParse(value.ToString(), out booleanValue))
                {
                    this.AutoRotateThroughDemos = booleanValue;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["DemoRotationTimePerDemo"];
            if (value != null)
            {
                int duration;
                if (int.TryParse(value.ToString(), out duration))
                {
                    this.DemoRotationTimePerDemo = duration;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["ShowDialogOnApiErrors"];
            if (value != null)
            {
                bool booleanValue;
                if (bool.TryParse(value.ToString(), out booleanValue))
                {
                    this.ShowDialogOnApiErrors = booleanValue;
                }
            }

            // load mall kiosk demo custom settings from file as the content is too big to be saved as a string-like setting
            try
            {
                using (Stream stream = await ApplicationData.Current.RoamingFolder.OpenStreamForReadAsync("MallKioskDemoCustomSettings.xml"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        this.MallKioskDemoCustomSettings = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception)
            {
                this.RestoreMallKioskSettingsToDefaultFile();
            }
        }

        public static string GetRegionEndpoint(string region, string endpointTemplate = "https://{0}.api.cognitive.microsoft.com", string defaultEndpoint = null)
        {
            if (!string.IsNullOrEmpty(region) && AvailableApiRegions.Any(x => string.Compare(x.Value, region, StringComparison.OrdinalIgnoreCase) == 0))
            {
                return string.Format(endpointTemplate, region.ToLower());
            }
            return defaultEndpoint ?? DefaultApiEndpoint;
        }

        public void RestoreMallKioskSettingsToDefaultFile()
        {
            this.MallKioskDemoCustomSettings = File.ReadAllText("Views\\MallKioskDemoConfig\\MallKioskDemoSettings.xml");
        }

        public void RestoreAllSettings()
        {
            ApplicationData.Current.RoamingSettings.Values.Clear();
        }

        private string faceApiKey = string.Empty;
        public string FaceApiKey
        {
            get { return this.faceApiKey; }
            set
            {
                this.faceApiKey = value;
                this.OnSettingChanged("FaceApiKey", value);
            }
        }

        private string faceApiKeyEndpoint = DefaultApiEndpoint;
        public string FaceApiKeyEndpoint
        {
            get
            {
                return string.Equals(this.faceApiKeyEndpoint, SettingsHelper.CustomEndpointName, StringComparison.OrdinalIgnoreCase)
                    ? this.customFaceApiEndpoint
                    : this.faceApiKeyEndpoint;
            }
            set
            {
                this.faceApiKeyEndpoint = value;
                this.OnSettingChanged("FaceApiKeyEndpoint", value);
            }
        }

        public string BindingFaceApiKeyEndpoint
        {
            get { return this.faceApiKeyEndpoint; }
            set
            {
                this.faceApiKeyEndpoint = value;
                this.OnSettingChanged("FaceApiKeyEndpoint", value);
            }
        }

        private string visionApiKey = string.Empty;
        public string VisionApiKey
        {
            get { return this.visionApiKey; }
            set
            {
                this.visionApiKey = value;
                this.OnSettingChanged("VisionApiKey", value);
            }
        }

        private string visionApiKeyEndpoint = DefaultApiEndpoint;
        public string VisionApiKeyEndpoint
        {
            get
            {
                return string.Equals(this.visionApiKeyEndpoint, SettingsHelper.CustomEndpointName, StringComparison.OrdinalIgnoreCase)
                    ? this.customComputerVisionApiEndpoint
                    : this.visionApiKeyEndpoint;
            }
            set
            {
                this.visionApiKeyEndpoint = value;
                this.OnSettingChanged("VisionApiKeyEndpoint", value);
            }
        }

        public string BindingVisionApiKeyEndpoint
        {
            get { return this.visionApiKeyEndpoint; }
            set
            {
                this.visionApiKeyEndpoint = value;
                this.OnSettingChanged("VisionApiKeyEndpoint", value);
            }
        }

        private string bingSearchApiKey = string.Empty;
        public string BingSearchApiKey
        {
            get { return this.bingSearchApiKey; }
            set
            {
                this.bingSearchApiKey = value;
                this.OnSettingChanged("BingSearchApiKey", value);
            }
        }

        private string workspaceKey = string.Empty;
        public string WorkspaceKey
        {
            get { return workspaceKey; }
            set
            {
                this.workspaceKey = value;
                this.OnSettingChanged("WorkspaceKey", value);
            }
        }

        private string mallKioskDemoCustomSettings = string.Empty;
        public string MallKioskDemoCustomSettings
        {
            get { return this.mallKioskDemoCustomSettings; }
            set
            {
                this.mallKioskDemoCustomSettings = value;
                this.OnSettingChanged("MallKioskDemoCustomSettings", value);
            }
        }

        private int howOldKioskResultDisplayDuration = 10;
        public int HowOldKioskResultDisplayDuration
        {
            get { return this.howOldKioskResultDisplayDuration; }
            set
            {
                this.howOldKioskResultDisplayDuration = value;
                this.OnSettingChanged("HowOldKioskResultDisplayDuration", value);
            }
        }

        private string startingPage = "Demo Gallery";
        public string StartingPage
        {
            get { return startingPage; }
            set
            {
                this.startingPage = value;
                this.OnSettingChanged("StartingPage", value);
            }
        }

        private string[] startingPageNames;
        public string[] StartingPageNames
        {
            get { return startingPageNames; }
            set
            {
                this.startingPageNames = value;
                this.OnSettingChanged("StartingPageNames", value);
            }
        }

        private string textAnalyticsKey = string.Empty;
        public string TextAnalyticsKey
        {
            get { return textAnalyticsKey; }
            set
            {
                this.textAnalyticsKey = value;
                this.OnSettingChanged("TextAnalyticsKey", value);
            }
        }

        private string textAnalyticsApiKeyEndpoint = DefaultApiEndpoint;
        public string TextAnalyticsApiKeyEndpoint
        {
            get
            {
                return string.Equals(this.textAnalyticsApiKeyEndpoint, SettingsHelper.CustomEndpointName, StringComparison.OrdinalIgnoreCase)
                    ? this.customTextAnalyticsEndpoint
                    : this.textAnalyticsApiKeyEndpoint;
            }
            set
            {
                this.textAnalyticsApiKeyEndpoint = value;
                this.OnSettingChanged("TextAnalyticsApiKeyEndpoint", value);
            }
        }

        public string BindingTextAnalyticsApiKeyEndpoint
        {
            get { return this.textAnalyticsApiKeyEndpoint; }
            set
            {
                this.textAnalyticsApiKeyEndpoint = value;
                this.OnSettingChanged("TextAnalyticsApiKeyEndpoint", value);
            }
        }

        private string cameraName = string.Empty;
        public string CameraName
        {
            get { return cameraName; }
            set
            {
                this.cameraName = value;
                this.OnSettingChanged("CameraName", value);
            }
        }

        private string microphoneName = string.Empty;
        public string MicrophoneName
        {
            get { return microphoneName; }
            set
            {
                this.microphoneName = value;
                this.OnSettingChanged("MicrophoneName", value);
            }
        }

        private VideoRotation cameraRotation = VideoRotation.None;
        public VideoRotation CameraRotation
        {
            get { return cameraRotation; }
            set
            {
                this.cameraRotation = value;
                this.OnSettingChanged("CameraRotation", value.ToString());
            }
        }

        private uint minDetectableFaceCoveragePercentage = 7;
        public uint MinDetectableFaceCoveragePercentage
        {
            get { return this.minDetectableFaceCoveragePercentage; }
            set
            {
                this.minDetectableFaceCoveragePercentage = value;
                this.OnSettingChanged("MinDetectableFaceCoveragePercentage", value);
            }
        }

        private bool showDebugInfo = false;
        public bool ShowDebugInfo
        {
            get { return showDebugInfo; }
            set
            {
                this.showDebugInfo = value;
                this.OnSettingChanged("ShowDebugInfo", value);
            }
        }

        private string customVisionTrainingApiKey = string.Empty;
        public string CustomVisionTrainingApiKey
        {
            get { return this.customVisionTrainingApiKey; }
            set
            {
                this.customVisionTrainingApiKey = value;
                this.OnSettingChanged("CustomVisionTrainingApiKey", value);
            }
        }

        private string customVisionTrainingApiKeyEndpoint = DefaultCustomVisionApiEndpoint;
        public string CustomVisionTrainingApiKeyEndpoint
        {
            get { return this.customVisionTrainingApiKeyEndpoint; }
            set
            {
                this.customVisionTrainingApiKeyEndpoint = value;
                this.OnSettingChanged("CustomVisionTrainingApiKeyEndpoint", value);
            }
        }

        private string customVisionPredictionApiKey = string.Empty;
        public string CustomVisionPredictionApiKey
        {
            get { return this.customVisionPredictionApiKey; }
            set
            {
                this.customVisionPredictionApiKey = value;
                this.OnSettingChanged("CustomVisionPredictionApiKey", value);
            }
        }

        private string customVisionPredictionResourceId = string.Empty;
        public string CustomVisionPredictionResourceId
        {
            get { return this.customVisionPredictionResourceId; }
            set
            {
                this.customVisionPredictionResourceId = value;
                this.OnSettingChanged("CustomVisionPredictionResourceId", value);
            }
        }

        private string customVisionPredictionApiKeyEndpoint = DefaultCustomVisionApiEndpoint;
        public string CustomVisionPredictionApiKeyEndpoint
        {
            get { return this.customVisionPredictionApiKeyEndpoint; }
            set
            {
                this.customVisionPredictionApiKeyEndpoint = value;
                this.OnSettingChanged("CustomVisionPredictionApiKeyEndpoint", value);
            }
        }

        private string customFaceApiEndpoint = string.Empty;
        public string CustomFaceApiEndpoint
        {
            get { return this.customFaceApiEndpoint; }
            set
            {
                this.customFaceApiEndpoint = value;
                this.OnSettingChanged("CustomFaceApiEndpoint", value);
            }
        }

        private string customComputerVisionApiEndpoint = string.Empty;
        public string CustomComputerVisionApiEndpoint
        {
            get { return this.customComputerVisionApiEndpoint; }
            set
            {
                this.customComputerVisionApiEndpoint = value;
                this.OnSettingChanged("CustomComputerVisionApiEndpoint", value);
            }
        }

        private string customTextAnalyticsEndpoint = string.Empty;
        public string CustomTextAnalyticsEndpoint
        {
            get { return this.customTextAnalyticsEndpoint; }
            set
            {
                this.customTextAnalyticsEndpoint = value;
                this.OnSettingChanged("CustomTextAnalyticsEndpoint", value);
            }
        }

        private string customAnomalyDetectorApiEndpoint = string.Empty;
        public string CustomAnomalyDetectorApiEndpoint
        {
            get { return this.customAnomalyDetectorApiEndpoint; }
            set
            {
                this.customAnomalyDetectorApiEndpoint = value;
                this.OnSettingChanged("CustomAnomalyDetectorApiEndpoint", value);
            }
        }

        private string customSpeechApiEndpoint = string.Empty;
        public string CustomSpeechApiEndpoint
        {
            get { return this.customSpeechApiEndpoint; }
            set
            {
                this.customSpeechApiEndpoint = value;
                this.OnSettingChanged("CustomSpeechApiEndpoint", value);
            }
        }

        private string translatorTextApiKey = string.Empty;
        public string TranslatorTextApiKey
        {
            get { return translatorTextApiKey; }
            set
            {
                this.translatorTextApiKey = value;
                this.OnSettingChanged("TranslatorTextApiKey", value);
            }
        }

        private string translatorTextApiRegion = DefaultTranslatorTextApiRegion;
        public string TranslatorTextApiRegion
        {
            get 
            {
                return string.Equals(this.translatorTextApiRegion, SettingsHelper.GlobalRegionName, StringComparison.OrdinalIgnoreCase)
                        ? string.Empty
                        : this.translatorTextApiRegion;
            }
            set
            {
                this.translatorTextApiRegion = value;
                this.OnSettingChanged("TranslatorTextApiRegion", value);
            }
        }

        public string BindingTranslatorTextApiRegion
        {
            get { return this.translatorTextApiRegion; }
            set
            {
                this.translatorTextApiRegion = value;
                this.OnSettingChanged("TranslatorTextApiRegion", value);
            }
        }

        private string anomalyDetectorApiKey = string.Empty;
        public string AnomalyDetectorApiKey
        {
            get { return anomalyDetectorApiKey; }
            set
            {
                this.anomalyDetectorApiKey = value;
                this.OnSettingChanged("AnomalyDetectorApiKey", value);
            }
        }

        private string anomalyDetectorKeyEndpoint = DefaultApiEndpoint;
        public string AnomalyDetectorKeyEndpoint
        {
            get 
            { 
                return string.Equals(this.anomalyDetectorKeyEndpoint, SettingsHelper.CustomEndpointName, StringComparison.OrdinalIgnoreCase)
                    ? this.customAnomalyDetectorApiEndpoint
                    : this.anomalyDetectorKeyEndpoint;
            }
            set
            {
                this.anomalyDetectorKeyEndpoint = value;
                this.OnSettingChanged("AnomalyDetectorKeyEndpoint", value);
            }
        }

        public string BindingAnomalyDetectorApiKeyEndpoint
        {
            get { return this.anomalyDetectorKeyEndpoint; }
            set
            {
                this.AnomalyDetectorKeyEndpoint = value;
            }
        }

        private string formRecognizerApiKey;
        public string FormRecognizerApiKey
        {
            get { return this.formRecognizerApiKey; }
            set
            {
                this.formRecognizerApiKey = value;
                this.OnSettingChanged("FormRecognizerApiKey", value);
            }
        }

        private string formRecognizerApiKeyEndpoint = DefaultFormRecognizerApiEndpoint;
        public string FormRecognizerApiKeyEndpoint
        {
            get { return this.formRecognizerApiKeyEndpoint; }
            set
            {
                this.formRecognizerApiKeyEndpoint = value;
                this.OnSettingChanged("FormRecognizerApiKeyEndpoint", value);
            }
        }

        private string speechApiKey;
        public string SpeechApiKey
        {
            get { return this.speechApiKey; }
            set
            {
                this.speechApiKey = value;
                this.OnSettingChanged("SpeechApiKey", value);
            }
        }

        private string speechApiEndpoint = DefaultSpeechApiEndpoint;
        public string SpeechApiEndpoint
        {
            get
            {
                return string.Equals(this.speechApiEndpoint, SettingsHelper.CustomEndpointName, StringComparison.OrdinalIgnoreCase)
                        ? this.customSpeechApiEndpoint
                        : this.speechApiEndpoint;
            }
            set
            {
                this.speechApiEndpoint = value;
                this.OnSettingChanged("SpeechApiEndpoint", value);
            }
        }

        public string BindingSpeechApiKeyEndpoint
        {
            get { return this.speechApiEndpoint; }
            set
            {
                this.speechApiEndpoint = value;
                this.OnSettingChanged("SpeechApiEndpoint", value);
            }
        }

        private bool startupFullScreenMode = false;
        public bool StartupFullScreenMode
        {
            get { return this.startupFullScreenMode; }
            set
            {
                this.startupFullScreenMode = value;
                this.OnSettingChanged("StartupFullScreenMode", value);
            }
        }

        private bool showAgeAndGender = false;
        public bool ShowAgeAndGender
        {
            get { return showAgeAndGender; }
            set
            {
                this.showAgeAndGender = value;
                this.OnSettingChanged("ShowAgeAndGender", value);
            }
        }

        private bool autoRotateThroughDemos = false;
        public bool AutoRotateThroughDemos
        {
            get { return this.autoRotateThroughDemos; }
            set
            {
                this.autoRotateThroughDemos = value;
                this.OnSettingChanged("AutoRotateThroughDemos", value);
            }
        }

        private int demoRotationTimePerDemo = 30;
        public int DemoRotationTimePerDemo
        {
            get { return this.demoRotationTimePerDemo; }
            set
            {
                this.demoRotationTimePerDemo = value;
                this.OnSettingChanged("DemoRotationTimePerDemo", value);
            }
        }

        private bool showDialogOnApiErrors = false;
        public bool ShowDialogOnApiErrors
        {
            get { return showDialogOnApiErrors; }
            set
            {
                this.showDialogOnApiErrors = value;
                this.OnSettingChanged("ShowDialogOnApiErrors", value);
            }
        }

        public KeyValuePair<string, string>[] AvailableApiEndpoints
        {
            get
            {
                return AvailableApiRegions.Select(i => new KeyValuePair<string, string>(i.Key, $"https://{i.Value}.api.cognitive.microsoft.com")).Concat(new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Custom Endpoint", CustomEndpointName) }).ToArray();
            }
        }

        public KeyValuePair<string, string>[] AvailableSpeechApiEndpoints
        {
            get
            {
                return AvailableApiRegions.Select(i => new KeyValuePair<string, string>(i.Key, $"wss://{i.Value}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1")).Concat(new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Custom Endpoint", CustomEndpointName) }).ToArray();
            }
        }

        public KeyValuePair<string, string>[] AvailableCustomVisionApiEndpoints
        {
            get
            {
                return new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string,string>("East US",            "https://eastus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("East US 2",          "https://eastus2.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("South Central US",   "https://southcentralus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("West US 2",          "https://westus2.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("North Central US",   "https://northcentralus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Australia East",     "https://australiaeast.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Southeast Asia",     "https://southeastasia.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Central India",      "https://centralindia.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Japan East",         "https://japaneast.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("North Europe",       "https://northeurope.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("UK South",           "https://uksouth.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("West Europe",        "https://westeurope.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("South Africa North", "https://southafricanorth.api.cognitive.microsoft.com")
                };
            }
        }

        public KeyValuePair<string, string>[] AvailableFormRecognizerApiEndpoints
        {
            get
            {
                return new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string,string>("East US",          "https://eastus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("East US 2",        "https://eastus2.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("West US",          "https://westus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("West US 2",        "https://westus2.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Central US",       "https://centralus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("West Central US",  "https://westcentralus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("North Central US", "https://northcentralus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("South Central US", "https://southcentralus.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Canada Central",   "https://canadacentral.api.cognitive.microsoft.com"),

                    new KeyValuePair<string,string>("Australia East",   "https://australiaeast.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Southeast Asia",   "https://southeastasia.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("East Asia",        "https://eastasia.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Japan East",       "https://japaneast.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Japan West",       "https://japanwest.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Korea Central",    "https://koreacentral.api.cognitive.microsoft.com"),

                    new KeyValuePair<string,string>("North Europe",     "https://northeurope.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("UK South",         "https://uksouth.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("West Europe",      "https://westeurope.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("France Central",   "https://francecentral.api.cognitive.microsoft.com"),

                    new KeyValuePair<string,string>("UAE North",        "https://uaenorth.api.cognitive.microsoft.com"),
                    new KeyValuePair<string,string>("Brazil South",     "https://brazilsouth.api.cognitive.microsoft.com")
                };
            }
        }

        public KeyValuePair<string, string>[] AvailableTranslatorTextApiRegions
        {
            get
            {
                return new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("Global", GlobalRegionName),
                    new KeyValuePair<string, string>("Central US", "centralus"),
                    new KeyValuePair<string, string>("Central US EUAP", "centraluseuap"),
                    new KeyValuePair<string, string>("East US", "eastus"),
                    new KeyValuePair<string, string>("East US 2", "eastus2"),
                    new KeyValuePair<string, string>("North Central US", "northcentralus"),
                    new KeyValuePair<string, string>("South Central US", "southcentralus"),
                    new KeyValuePair<string, string>("West Central US", "westcentralus"),
                    new KeyValuePair<string, string>("West US", "westus"),
                    new KeyValuePair<string, string>("West US 2", "westus2"),
                    new KeyValuePair<string, string>("Australia East", "australiaeast"),
                    new KeyValuePair<string, string>("Brazil South", "brazilsouth"),
                    new KeyValuePair<string, string>("Canada Central", "canadacentral"),
                    new KeyValuePair<string, string>("Central India", "centralindia"),
                    new KeyValuePair<string, string>("East Asia", "eastasia"),
                    new KeyValuePair<string, string>("France Central", "francecentral"),
                    new KeyValuePair<string, string>("Japan East", "japaneast"),
                    new KeyValuePair<string, string>("Japan West", "japanwest"),
                    new KeyValuePair<string, string>("Korea Central", "koreacentral"),
                    new KeyValuePair<string, string>("North Europe", "northeurope"),
                    new KeyValuePair<string, string>("Southeast Asia", "southeastasia"),
                    new KeyValuePair<string, string>("UK South", "uksouth"),
                    new KeyValuePair<string, string>("West Europe", "westeurope"),
                    new KeyValuePair<string, string>("South Africa North", "southafricanorth")
                };
            }
        }
    }
}
