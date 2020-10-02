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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.DemoLauncher
{
    public partial class DemoLauncherPage : Page, INotifyPropertyChanged
    {
        private readonly static int recentlyUsedDemoNumber = 5;

        private DemoLauncherConfig config;
        private int numberOfActiveDemos = 0;
        private SemaphoreSlim configFileAccessLock = new SemaphoreSlim(1, 1);

        public ObservableCollection<DemoEntry> AllDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public ObservableCollection<DemoEntry> GalleryDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public ObservableCollection<DemoEntry> RecentlyUsedDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public ObservableCollection<DemoEntry> NewOrUpdatedDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public ObservableCollection<DemoEntry> VisionDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public ObservableCollection<DemoEntry> SearchDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public ObservableCollection<DemoEntry> LanguageAndSpeechDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public ObservableCollection<DemoEntry> SocialPhotoBoothsDemos { get; set; } = new ObservableCollection<DemoEntry>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string title = "Demo Gallery";
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public string subTitle = string.Empty;
        public string SubTitle
        {
            get { return subTitle; }
            set
            {
                subTitle = value;
                NotifyPropertyChanged("SubTitle");
            }
        }

        public bool isMainPage = true;
        public bool IsMainPage
        {
            get { return isMainPage; }
            set
            {
                isMainPage = value;
                NotifyPropertyChanged("IsMainPage");
            }
        }

        public bool isSearchBarEnable = false;
        public bool IsSearchBarEnable
        {
            get { return isSearchBarEnable; }
            set
            {
                isSearchBarEnable = value;
                NotifyPropertyChanged("IsSearchBarEnable");
            }
        }

        private DemoCollectionType demoCollectionGroup = DemoCollectionType.All;
        public DemoCollectionType DemoCollectionGroup
        {
            get { return demoCollectionGroup; }
            set
            {
                demoCollectionGroup = value;
                NotifyPropertyChanged("DemoCollectionGroup");
            }
        }

        public DemoLauncherPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        internal async static Task<DemoLauncherConfig> LoadDemoLauncherConfigFromFile(string fileName, bool enableDemosByDefault = true)
        {
            DemoLauncherConfig loadedConfig = null;
            IStorageItem configFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            if (configFile != null)
            {
                try
                {
                    loadedConfig = await DemoLauncherConfig.FromFileAsync(configFile.Path);

                    // Delete any demos from the persisted file list if they are no longer available
                    foreach (var entry in loadedConfig.Entries.ToArray())
                    {
                        if (!KioskExperiences.Experiences.Any(exp => exp.Attributes.Id == entry.Id))
                        {
                            loadedConfig.Entries.Remove(entry);
                        }
                    }

                    // Add any new demos to the persisted file list if they are not there
                    foreach (var experience in KioskExperiences.Experiences)
                    {
                        DemoEntry entry = loadedConfig.Entries.FirstOrDefault(d => d.Id == experience.Attributes.Id);
                        if (entry == null)
                        {
                            // New demo -> add to list
                            loadedConfig.Entries.Add(
                                new DemoEntry
                                {
                                    Id = experience.Attributes.Id,
                                    Enabled = enableDemosByDefault,
                                    KioskExperience = experience
                                });
                        }
                        else
                        {
                            // we don't persist the experience attributes in the config file, so set it here
                            entry.KioskExperience = experience;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            if (loadedConfig == null)
            {
                loadedConfig = new DemoLauncherConfig
                {
                    Entries = KioskExperiences.Experiences.Select(exp =>
                        new DemoEntry
                        {
                            Id = exp.Attributes.Id,
                            Enabled = enableDemosByDefault,
                            KioskExperience = exp
                        }).ToList()
                };
            }

            return loadedConfig;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string)
            {
                Enum.TryParse<DemoCollectionType>((string)e.Parameter, out DemoCollectionType collectionGroup);
                DemoCollectionGroup = collectionGroup;
            }

            this.config = await LoadDemoLauncherConfigFromFile("DemoLauncherConfig.xml");
            this.AllDemos.AddRange(this.config.Entries);

            this.UpdateActiveDemos();

            base.OnNavigatedTo(e);
        }

        private void UpdateActiveDemos()
        {
            IEnumerable<DemoEntry> demoEntries = this.config.Entries.Where(d => d.Enabled)
                .OrderByDescending(d => d.KioskExperience.Attributes.DateUpdated ?? d.KioskExperience.Attributes.DateAdded, StringComparer.OrdinalIgnoreCase);

            // Filter by search box
            if (!string.IsNullOrEmpty(this.searchTextBox?.Text))
            {
                demoEntries = demoEntries.Where(d =>
                    d.KioskExperience.Attributes.DisplayName.Contains(this.searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                    d.KioskExperience.Attributes.Description.Contains(this.searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                    d.KioskExperience.Attributes.ExperienceType.ToString().Contains(this.searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                    d.KioskExperience.Attributes.TechnologiesUsed.ToString().Contains(this.searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                    d.KioskExperience.Attributes.TechnologyArea.ToString().Contains(this.searchTextBox.Text, StringComparison.OrdinalIgnoreCase));
            }

            // Add demo collections
            Func<DemoEntry, bool> recentlyUsedPredicate = d => d.LastOpenDateInUTC > DateTime.MinValue;

            Func<DemoEntry, bool> visionPredicate = d => (d.KioskExperience.Attributes.TechnologyArea & TechnologyAreaType.Vision) != 0 &&
                                                         (d.KioskExperience.Attributes.ExperienceType & ExperienceType.Fun) == 0;

            Func<DemoEntry, bool> searchPredicate = d => ((d.KioskExperience.Attributes.TechnologyArea & TechnologyAreaType.Search) != 0 ||
                                                         (d.KioskExperience.Attributes.TechnologyArea & TechnologyAreaType.Decision) != 0) &&
                                                         (d.KioskExperience.Attributes.ExperienceType & ExperienceType.Fun) == 0;

            Func<DemoEntry, bool> languageAndSpeechPredicate = d => ((d.KioskExperience.Attributes.TechnologyArea & TechnologyAreaType.Language) != 0 ||
                                                                     (d.KioskExperience.Attributes.TechnologyArea & TechnologyAreaType.Speech) != 0) &&
                                                                     (d.KioskExperience.Attributes.ExperienceType & ExperienceType.Fun) == 0;

            Func<DemoEntry, bool> socialPhotoBoothsPredicate = d => (d.KioskExperience.Attributes.ExperienceType & ExperienceType.Fun) != 0;
            switch (DemoCollectionGroup)
            {
                case DemoCollectionType.All:
                    this.Title = "Demo Gallery";
                    this.SubTitle = string.Empty;
                    this.IsMainPage = true;

                    this.RecentlyUsedDemos.Clear();
                    this.RecentlyUsedDemos.AddRange(demoEntries.Where(recentlyUsedPredicate).OrderByDescending(x => x.LastOpenDateInUTC).Take(recentlyUsedDemoNumber));

                    this.NewOrUpdatedDemos.Clear();
                    this.NewOrUpdatedDemos.AddRange(demoEntries.Where(x => IsNewDemo(x.KioskExperience)));

                    this.VisionDemos.Clear();
                    this.VisionDemos.AddRange(demoEntries.Where(visionPredicate));

                    this.SearchDemos.Clear();
                    this.SearchDemos.AddRange(demoEntries.Where(searchPredicate));

                    this.LanguageAndSpeechDemos.Clear();
                    this.LanguageAndSpeechDemos.AddRange(demoEntries.Where(languageAndSpeechPredicate));

                    this.SocialPhotoBoothsDemos.Clear();
                    this.SocialPhotoBoothsDemos.AddRange(demoEntries.Where(socialPhotoBoothsPredicate));
                    break;

                case DemoCollectionType.Favorites:
                    var favoriteDemos = this.config.Entries.Where(d => d.IsFavorite).OrderByDescending(x => x.KioskExperience.Attributes.DateAdded, StringComparer.OrdinalIgnoreCase);
                    demoEntries = demoEntries.Where(d => favoriteDemos.Any(f => f.Id == d.Id));
                    this.Title = "Favorite demos";
                    OpenGalleryDemoView(demoEntries);
                    break;

                case DemoCollectionType.RecentlyUsed:
                    demoEntries = demoEntries.Where(recentlyUsedPredicate).OrderByDescending(x => x.LastOpenDateInUTC).Take(recentlyUsedDemoNumber);
                    OpenGalleryDemoView(demoEntries, "Your recent demos");
                    break;

                case DemoCollectionType.NewOrUpdated:
                    demoEntries = demoEntries.Where(x => IsNewDemo(x.KioskExperience));
                    OpenGalleryDemoView(demoEntries, "What's new");
                    break;

                case DemoCollectionType.Vision:
                    demoEntries = demoEntries.Where(visionPredicate);
                    OpenGalleryDemoView(demoEntries, "Vision");
                    break;

                case DemoCollectionType.Search:
                    demoEntries = demoEntries.Where(searchPredicate);
                    OpenGalleryDemoView(demoEntries, "Decision and Search");
                    break;

                case DemoCollectionType.LanguageAndSpeech:
                    demoEntries = demoEntries.Where(languageAndSpeechPredicate);
                    OpenGalleryDemoView(demoEntries, "Language and Speech");
                    break;

                case DemoCollectionType.SocialPhotoBooths:
                    demoEntries = demoEntries.Where(socialPhotoBoothsPredicate);
                    OpenGalleryDemoView(demoEntries, "Social Photo Booths");
                    break;
            }
            this.numberOfActiveDemos = demoEntries.Count();
        }

        private void OpenGalleryDemoView(IEnumerable<DemoEntry> demoEntries, string galleryName = "")
        {
            this.GalleryDemos.Clear();
            this.GalleryDemos.AddRange(demoEntries);

            this.SubTitle = galleryName;
            this.IsMainPage = false;
        }

        private async Task ProcessDemoConfigChanges(bool refreshUI = true)
        {
            await configFileAccessLock.WaitAsync();
            try
            {
                // Save to file
                await SaveConfigToFileAsync();
            }
            finally
            {
                configFileAccessLock.Release();
            }

            if (refreshUI)
            {
                // Update UI
                this.UpdateActiveDemos();
            }
        }

        private async Task SaveConfigToFileAsync()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DemoLauncherConfig));
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("DemoLauncherConfig.xml", CreationCollisionOption.ReplaceExisting);

            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                serializer.Serialize(stream, this.config);
            }
        }

        private void SearchTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (this.config != null)
            {
                this.UpdateActiveDemos();
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.IsSearchBarEnable = !string.IsNullOrEmpty(this.searchTextBox?.Text);
        }

        private async void OnFavoriteDemoToggled(object sender, EventArgs e)
        {
            await this.ProcessDemoConfigChanges(refreshUI: DemoCollectionGroup == DemoCollectionType.Favorites);
        }

        private async void OnDemoOpened(object sender, DemoEntry demoEntry)
        {
            demoEntry.LastOpenDateInUTC = DateTime.UtcNow;
            await this.ProcessDemoConfigChanges();
        }

        private void OnDemoCollectionSeeAllButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton hyperlinkButton)
            {
                string commandParameter = hyperlinkButton.CommandParameter.ToString();
                Enum.TryParse<DemoCollectionType>(commandParameter, out DemoCollectionType collectionGroup);
                DemoCollectionGroup = collectionGroup;
                this.UpdateActiveDemos();
            }
        }

        private void OnDemoGalleryHyperLinkButtonClicked(object sender, RoutedEventArgs e)
        {
            SwitchToMainGalleryView();
        }

        public void SwitchToMainGalleryView()
        {
            DemoCollectionGroup = DemoCollectionType.All;
            this.UpdateActiveDemos();
        }

        private bool IsNewDemo(KioskExperience kioskExperience, int maxDays = 90)
        {
            if (kioskExperience?.Attributes != null)
            {
                DateTime.TryParse(kioskExperience.Attributes?.DateAdded ?? string.Empty, out DateTime demoDateAdded);
                if ((DateTime.Now - demoDateAdded).TotalDays <= maxDays)
                {
                    return true;
                }

                DateTime.TryParse(kioskExperience.Attributes?.DateUpdated ?? string.Empty, out DateTime demoDateUpdated);
                if ((DateTime.Now - demoDateUpdated).TotalDays <= maxDays)
                {
                    return true;
                }
            }
            return false;
        }

        private async void OnSearchAppBarButtonClicked(object sender, RoutedEventArgs e)
        {
            this.IsSearchBarEnable = true;
            await Task.Delay(5); // autoSuggestBox is initialize
            this.searchTextBox.Focus(FocusState.Programmatic);
        }
    }

    [XmlType]
    [XmlRoot]
    public class DemoLauncherConfig
    {
        [XmlArrayItem]
        public List<DemoEntry> Entries { get; set; }

        public static Task<DemoLauncherConfig> FromFileAsync(string filePath)
        {
            return Task.Run(() =>
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    var xs = new XmlSerializer(typeof(DemoLauncherConfig));
                    return (DemoLauncherConfig)xs.Deserialize(fileStream);
                }
            });
        }
    }

    [XmlType]
    public class DemoEntry
    {
        public string Id { get; set; }

        public bool Enabled { get; set; }

        public bool IsFavorite { get; set; }

        public DateTime LastOpenDateInUTC { get; set; }

        [XmlIgnore]
        public KioskExperience KioskExperience { get; set; }
    }

    public enum DemoCollectionType
    {
        All = 0,
        Favorites = 10,
        RecentlyUsed = 20,
        NewOrUpdated = 30,
        Vision = 40,
        Search = 50,
        LanguageAndSpeech = 60,
        SocialPhotoBooths = 70
    }
}
