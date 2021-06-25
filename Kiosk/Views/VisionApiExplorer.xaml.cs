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

using IntelligentKioskSample.Controls.Overlays;
using IntelligentKioskSample.Controls.Overlays.Primitives;
using IntelligentKioskSample.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "VisionAPIExplorer",
        DisplayName = "Vision API Explorer",
        Description = "Extract insights from images, including tags, text, and objects",
        ImagePath = "ms-appx:/Assets/DemoGallery/Vision API Explorer.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologyArea = TechnologyAreaType.Vision,
        TechnologiesUsed = TechnologyType.BingAutoSuggest | TechnologyType.BingImages | TechnologyType.Vision,
        DateUpdated = "2019/10/02",
        UpdatedDescription = "Now uses Read API for text recognition",
        DateAdded = "2017/02/08")]
    public sealed partial class VisionApiExplorer : Page, INotifyPropertyChanged
    {
        IList<IList<TextOverlayInfo>> _ocrLines;
        IList<FaceOverlayInfo> _celebrities;
        IList<FaceOverlayInfo> _faces;
        IList<ObjectOverlayInfo> _objects;
        IList<ObjectOverlayInfo> _brands;
        string _noItemsDescription;
        PivotItem _selectedTab;
        const string NoneDesc = "None";

        public event PropertyChangedEventHandler PropertyChanged;

        public IList<IList<TextOverlayInfo>> OcrLines { get => _ocrLines; set { _ocrLines = value; OnPropertyChanged(); } }
        public IList<FaceOverlayInfo> Celebrities { get => _celebrities; set => SetProperty(ref _celebrities, value); }
        public IList<FaceOverlayInfo> Faces { get => _faces; set => SetProperty(ref _faces, value); }
        public IList<ObjectOverlayInfo> Objects { get => _objects; set => SetProperty(ref _objects, value); }
        public IList<ObjectOverlayInfo> Brands { get => _brands; set => SetProperty(ref _brands, value); }
        public string NoItemsDescription { get => _noItemsDescription; set => SetProperty(ref _noItemsDescription, value); }

        public TabHeader SummaryTab = new TabHeader() { Name = "Summary" };
        public TabHeader FacesTab = new TabHeader() { Name = "Faces" };
        public TabHeader ObjectsTab = new TabHeader() { Name = "Objects" };
        public TabHeader TextTab = new TabHeader() { Name = "Text" };
        public PivotItem SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    SetMute(SelectedTab);
                }
            }
        }

        public static bool ShowAgeAndGender { get { return SettingsHelper.Instance.ShowAgeAndGender; } }

        public VisionApiExplorer()
        {
            this.InitializeComponent();
        }

        private void DisplayProcessingUI(bool reanalyze = true)
        {
            if (reanalyze)
            {
                this.tagsGridView.ItemsSource = new[] { new { Name = "Analyzing..." } };
                this.descriptionGridView.ItemsSource = new[] { new { Description = "Analyzing..." } };
                this.landmarksTextBlock.Text = "Analyzing...";
                this.colorInfoListView.ItemsSource = new[] { new { Description = "Analyzing..." } };
                NoItemsDescription = "Analyzing...";
                OcrLines = null;
                Celebrities = null;
                Faces = null;
                Objects = null;
                Brands = null;
                FacesTab.Summary = "Analyzing...";
                ObjectsTab.Summary = "Analyzing...";
                TextTab.Summary = "Analyzing...";
                Processing.IsActive = true;
            }

            OverlayPresenter.FaceInfo = null;
            OverlayPresenter.ObjectInfo = null;
            OverlayPresenter.TextInfo = null;
        }

        private async Task UpdateResultsAsync(ImageAnalyzer img)
        {
            //convert results to OverlayInfo
            var faces = img.AnalysisResult.Faces.Select(i => new FaceOverlayInfo(i, GetCelebrity(i, img.AnalysisResult)) { IsMuted = true }).ToArray();
            var objects = img.AnalysisResult.Objects.Select(i => new ObjectOverlayInfo(i) { IsMuted = true }).ToArray();
            var brands = img.AnalysisResult.Brands.Select(i => new ObjectOverlayInfo(i) { IsMuted = true }).ToArray();

            //extract crops from the image
            var stream = img.ImageUrl == null ? await img.GetImageStreamCallback() : new MemoryStream(await new HttpClient().GetByteArrayAsync(img.ImageUrl));

            using (stream)
            {
                foreach (var face in faces)
                {
                    face.EntityExt.Image = await Util.GetCroppedBitmapAsync(stream.AsRandomAccessStream(), face.Rect);
                }
                foreach (var obj in objects)
                {
                    obj.EntityExt.Image = await Util.GetCroppedBitmapAsync(stream.AsRandomAccessStream(), obj.Rect);
                }
                foreach (var brand in brands)
                {
                    brand.EntityExt.Image = await Util.GetCroppedBitmapAsync(stream.AsRandomAccessStream(), brand.Rect);
                }
            }

            //apply results
            Celebrities = faces.Where(i => i.IsCelebrity).ToArray();
            Faces = faces.Where(i => !i.IsCelebrity).ToArray();
            Objects = objects;
            Brands = brands;
            OverlayPresenter.FaceInfo = Faces;
            OverlayPresenter.ObjectInfo = objects.Concat(brands).ToArray();


            if (img.AnalysisResult.Tags == null || !img.AnalysisResult.Tags.Any())
            {
                this.tagsGridView.ItemsSource = new[] { new { Name = "No tags" } };
            }
            else
            {
                var tags = img.AnalysisResult.Tags.Select(t => new { Confidence = string.Format("({0}%)", Math.Round(t.Confidence * 100)), Name = t.Name });
                if (!ShowAgeAndGender)
                {
                    tags = tags.Where(t => !Util.ContainsGenderRelatedKeyword(t.Name));
                }

                this.tagsGridView.ItemsSource = tags;
            }

            if (img.AnalysisResult.Description == null || !img.AnalysisResult.Description.Captions.Any(d => d.Confidence >= 0.2))
            {
                this.descriptionGridView.ItemsSource = new[] { new { Description = "Not sure what that is" } };
            }
            else
            {
                var descriptions = img.AnalysisResult.Description.Captions.Select(d => new { Confidence = string.Format("({0}%)", Math.Round(d.Confidence * 100)), Description = d.Text });
                if (!ShowAgeAndGender)
                {
                    descriptions = descriptions.Where(t => !Util.ContainsGenderRelatedKeyword(t.Description));
                }

                if (descriptions.Any())
                {
                    this.descriptionGridView.ItemsSource = descriptions;
                }
                else
                {
                    this.descriptionGridView.ItemsSource = new[] { new { Description = "Please enable Age/Gender prediction in the Settings Page to see the results" } };
                }
            }

            var landmarkNames = this.GetLandmarkNames(img);
            if (landmarkNames == null || !landmarkNames.Any())
            {
                this.landmarksTextBlock.Text = NoneDesc;
            }
            else
            {
                this.landmarksTextBlock.Text = string.Join(", ", landmarkNames.OrderBy(name => name).Distinct());
            }

            if (img.AnalysisResult.Color == null)
            {
                this.colorInfoListView.ItemsSource = new[] { new { Description = "Not available" } };
            }
            else
            {
                this.colorInfoListView.ItemsSource = new[]
                {
                    new { Description = "Background", Colors = new string[] { img.AnalysisResult.Color.DominantColorBackground } },
                    new { Description = "Foreground", Colors = new string[] { img.AnalysisResult.Color.DominantColorForeground } },
                    new { Description = "Dominant", Colors = img.AnalysisResult.Color.DominantColors?.ToArray() },
                    new { Description = "Accent", Colors = new string[] { "#" + img.AnalysisResult.Color.AccentColor } }
                };
            }

            NoItemsDescription = NoneDesc;

            //update summaries
            FacesTab.Summary = SummaryBuilder((Faces?.Count(), "face", "faces"), (Celebrities?.Count(), "celebrity", "celebrities"));
            ObjectsTab.Summary = SummaryBuilder((Objects?.Count(), "object", "objects"), (Brands?.Count(), "brand", "brands"));

            //update muted items
            SetMute(SelectedTab);

            Processing.IsActive = false;
        }

        private CelebritiesModel GetCelebrity(FaceDescription face, ImageAnalysis result)
        {
            if (result.Categories != null)
            {
                foreach (var category in result.Categories.Where(c => c.Detail != null))
                {
                    if (category.Detail.Celebrities != null)
                    {
                        foreach (var celebrity in category.Detail.Celebrities)
                        {
                            int left = celebrity.FaceRectangle.Left;
                            int top = celebrity.FaceRectangle.Top;

                            if (Math.Abs(left - face.FaceRectangle.Left) <= 3 && Math.Abs(top - face.FaceRectangle.Top) <= 3)
                            {
                                return celebrity;
                            }
                        }
                    }
                }
            }
            return null;
        }

        string GetPlural((int? Count, string Desc, string PluralDesc) summary)
        {
            //plural form
            if (summary.Count != null && summary.Count.Value > 1)
            {
                return summary.PluralDesc;
            }

            //singular form
            return summary.Desc;
        }

        string SummaryBuilder(params (int? Count, string Desc, string PluralDesc)[] summary)
        {
            var strings = summary.Where(i => i.Count != null && i.Count != 0).Select(i => $"{i.Count} {GetPlural(i)}").ToArray();
            return strings.Length == 0 ? NoneDesc : string.Join(", ", strings);
        }

        private IEnumerable<string> GetLandmarkNames(ImageAnalyzer analyzer)
        {
            if (analyzer.AnalysisResult?.Categories != null)
            {
                foreach (var category in analyzer.AnalysisResult.Categories?.Where(c => c.Detail != null))
                {
                    if (category.Detail.Landmarks != null)
                    {
                        foreach (var landmark in category.Detail.Landmarks)
                        {
                            yield return landmark.Name;
                        }
                    }
                }
            }
        }

        private async Task UpdateActivePhoto(ImageAnalyzer img)
        {
            this.resultsDetails.Visibility = Visibility.Visible;
            var tasks = new List<Task>();

            if (img.AnalysisResult != null)
            {
                this.DisplayProcessingUI(reanalyze: false);
            }
            else
            {
                this.DisplayProcessingUI();
                tasks.Add(img.AnalyzeImageAsync());
            }

            if (img.TextOperationResult == null)
            {
                tasks.Add(img.RecognizeTextAsync());
            }

            await Task.WhenAll(tasks);

            if (img.AnalysisResult != null)
            {
                await this.UpdateResultsAsync(img);
            }
            if (img.TextOperationResult != null)
            {
                this.UpdateOcrTextBoxContent(img);
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await this.imagePicker.SetSuggestedImageList(
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/1.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/2.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/3.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/4.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/5.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/8.png"),
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/6.png"),
                new Uri("ms-appx:///Assets/DemoSamples/VisionApiExplorer/7.png")
            );

            if (string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey))
            {
                await new MessageDialog("Missing Computer Vision API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            ImageAnalyzer image = args.First();
            image.ShowDialogOnFaceApiErrors = true;

            //set image source
            if (image.ImageUrl != null)
            {
                OverlayPresenter.Source = new BitmapImage(new Uri(image.ImageUrl));
            }
            else if (image.GetImageStreamCallback != null)
            {
                var bitmap = new BitmapImage();
                var _ = bitmap.SetSourceAsync((await image.GetImageStreamCallback()).AsRandomAccessStream());
                OverlayPresenter.Source = bitmap;
            }

            await this.UpdateActivePhoto(image);
        }

        private void UpdateOcrTextBoxContent(ImageAnalyzer imageAnalyzer)
        {
            //convert results to OverlayInfo
            OcrLines = imageAnalyzer.TextOperationResult.Lines.Select(i => i.Words.Select(e => new TextOverlayInfo(e.Text, e.BoundingBox) { IsMuted = true }).ToArray()).ToArray();
            OverlayPresenter.TextInfo = OcrLines.SelectMany(i => i).ToArray();

            //update summaries
            TextTab.Summary = SummaryBuilder((OcrLines?.Sum(i => i.Count), "word", "words"));

            //update muted items
            SetMute(SelectedTab);

            Processing.IsActive = false;
        }

        void SetMute(PivotItem selectedPivot)
        {
            SetMute(selectedPivot.Header as TabHeader);
        }

        void SetMute(TabHeader selectedTab)
        {
            if (selectedTab == SummaryTab)
            {
                SetMute(OcrLines, true);
                SetMute(Celebrities, true);
                SetMute(Faces, true);
                SetMute(Objects, true);
                SetMute(Brands, true);
            }

            else if (selectedTab == FacesTab)
            {
                SetMute(OcrLines, true);
                SetMute(Celebrities, false);
                SetMute(Faces, false);
                SetMute(Objects, true);
                SetMute(Brands, true);
            }

            else if (selectedTab == ObjectsTab)
            {
                SetMute(OcrLines, true);
                SetMute(Celebrities, true);
                SetMute(Faces, true);
                SetMute(Objects, false);
                SetMute(Brands, false);
            }

            else if (selectedTab == TextTab)
            {
                SetMute(OcrLines, false);
                SetMute(Celebrities, true);
                SetMute(Faces, true);
                SetMute(Objects, true);
                SetMute(Brands, true);
            }
        }

        void SetMute(IEnumerable<IOverlayInfo> entities, bool mute)
        {
            //validate
            if (entities == null)
            {
                return;
            }

            foreach (var entity in entities)
            {
                entity.IsMuted = mute;
            }
        }

        void SetMute(IEnumerable<IEnumerable<IOverlayInfo>> entities, bool mute)
        {
            //validate
            if (entities == null)
            {
                return;
            }

            foreach (var entity in entities)
            {
                SetMute(entity, mute);
            }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void CopyOcrLines(object sender, RoutedEventArgs e)
        {
            if (OcrLines != null)
            {
                //get OCR text
                var ocrText = new StringBuilder();
                foreach (var line in OcrLines)
                {
                    ocrText.AppendLine(string.Join(" ", line.Select(i => i.EntityExt.Name)));
                }

                //send text to clipboard
                var dataPackage = new DataPackage();
                dataPackage.SetText(ocrText.ToString());
                Clipboard.SetContent(dataPackage);
            }

        }

        private void Summary_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            //highlight
            var border = sender as Border;
            var tab = border?.Tag as TabHeader;
            if (tab != null && tab.Summary != NoneDesc)
            {
                border.BorderBrush = new SolidColorBrush(Colors.White);
                SetMute(tab);
            }

        }

        private void Summary_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            //unhighlight
            var border = sender as Border;
            var tab = border?.Tag as TabHeader;
            if (tab != null && tab.Summary != NoneDesc)
            {
                border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                SetMute(SelectedTab);
            }
        }
    }
}
