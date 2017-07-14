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

using IntelligentKioskSample.Views;
using System;
using System.ComponentModel;
using System.IO;
using Windows.Storage;

namespace IntelligentKioskSample
{
    internal class SettingsHelper : INotifyPropertyChanged
    {
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
            Windows.Storage.ApplicationData.Current.DataChanged += RoamingDataChanged;
        }

        private void RoamingDataChanged(ApplicationData sender, object args)
        {
            LoadRoamingSettings();
            instance.OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            if (instance.SettingsChanged != null)
            {
                instance.SettingsChanged(instance, EventArgs.Empty);
            }
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
            if (instance.PropertyChanged != null)
            {
                instance.PropertyChanged(instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static SettingsHelper Instance
        {
            get
            {
                return instance;
            }
        }

        private async void LoadRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["FaceApiKey"];
            if (value != null)
            {
                this.FaceApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["FaceApiKeyRegion"];
            if (value != null)
            {
                this.FaceApiKeyRegion = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["EmotionApiKey"];
            if (value != null)
            {
                this.EmotionApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["VisionApiKey"];
            if (value != null)
            {
                this.VisionApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["VisionApiKeyRegion"];
            if (value != null)
            {
                this.VisionApiKeyRegion = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["BingSearchApiKey"];
            if (value != null)
            {
                this.BingSearchApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["BingAutoSuggestionApiKey"];
            if (value != null)
            {
                this.BingAutoSuggestionApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["WorkspaceKey"];
            if (value != null)
            {
                this.WorkspaceKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["TextAnalyticsKey"];
            if (value != null)
            {
                this.TextAnalyticsKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CameraName"];
            if (value != null)
            {
                this.CameraName = value.ToString();
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

            value = ApplicationData.Current.RoamingSettings.Values["DriverMonitoringSleepingThreshold"];
            if (value != null)
            {
                double threshold;
                if (double.TryParse(value.ToString(), out threshold))
                {
                    this.DriverMonitoringSleepingThreshold = threshold;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["DriverMonitoringYawningThreshold"];
            if (value != null)
            {
                double threshold;
                if (double.TryParse(value.ToString(), out threshold))
                {
                    this.DriverMonitoringYawningThreshold = threshold;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionPredictionApiKey"];
            if (value != null)
            {
                this.CustomVisionPredictionApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionTrainingApiKey"];
            if (value != null)
            {
                this.CustomVisionTrainingApiKey = value.ToString();
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

        private string faceApiKeyRegion = "westus";
        public string FaceApiKeyRegion
        {
            get { return this.faceApiKeyRegion; }
            set
            {
                this.faceApiKeyRegion = value;
                this.OnSettingChanged("FaceApiKeyRegion", value);
            }
        }

        private string emotionApiKey = string.Empty;
        public string EmotionApiKey
        {
            get { return this.emotionApiKey; }
            set
            {
                this.emotionApiKey = value;
                this.OnSettingChanged("EmotionApiKey", value);
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

        private string visionApiKeyRegion = "westus";
        public string VisionApiKeyRegion
        {
            get { return this.visionApiKeyRegion; }
            set
            {
                this.visionApiKeyRegion = value;
                this.OnSettingChanged("VisionApiKeyRegion", value);
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

        private string bingAutoSuggestionSearchApiKey = string.Empty;
        public string BingAutoSuggestionApiKey
        {
            get { return this.bingAutoSuggestionSearchApiKey; }
            set
            {
                this.bingAutoSuggestionSearchApiKey = value;
                this.OnSettingChanged("BingAutoSuggestionApiKey", value);
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

        private double driverMonitoringSleepingThreshold = RealtimeDriverMonitoring.DefaultSleepingApertureThreshold;
        public double DriverMonitoringSleepingThreshold
        {
            get { return this.driverMonitoringSleepingThreshold; }
            set
            {
                this.driverMonitoringSleepingThreshold = value;
                this.OnSettingChanged("DriverMonitoringSleepingThreshold", value);
            }
        }

        private double driverMonitoringYawningThreshold = RealtimeDriverMonitoring.DefaultYawningApertureThreshold;
        public double DriverMonitoringYawningThreshold
        {
            get { return this.driverMonitoringYawningThreshold; }
            set
            {
                this.driverMonitoringYawningThreshold = value;
                this.OnSettingChanged("DriverMonitoringYawningThreshold", value);
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

        public string[] AvailableApiRegions { get { return new string[] { "eastus2", "southeastasia", "westcentralus", "westeurope", "westus" }; } }
    }
}