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

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Face = Microsoft.Azure.CognitiveServices.Vision.Face;

namespace IntelligentKioskSample.Views.DigitalAssetManagement
{
    public class ImageFiltersViewModel
    {
        List<FilterCollection> _allFilters;
        List<ImageInsightsViewModel> _allResults = new List<ImageInsightsViewModel>();

        public ObservableCollection<ImageInsightsViewModel> FilteredResults { get; set; } = new ObservableCollection<ImageInsightsViewModel>();
        public FilterCollection ActiveFilters { get; set; } = new FilterCollection() { Name = "Filters" };
        public FilterCollection TagFilters { get; set; } = new FilterCollection() { Name = "Tags" };
        public FilterCollection FaceFilters { get; set; } = new FilterCollection() { Name = "Unique faces" };
        public FilterCollection EmotionFilters { get; set; } = new FilterCollection() { Name = "Emotion" };
        public FilterCollection ObjectFilters { get; set; } = new FilterCollection() { Name = "Detected objects" };
        public FilterCollection LandmarkFilters { get; set; } = new FilterCollection() { Name = "Landmarks" };
        public FilterCollection CelebrityFilters { get; set; } = new FilterCollection() { Name = "Celebrities" };
        public FilterCollection BrandFilters { get; set; } = new FilterCollection() { Name = "Brands" };
        public FilterCollection WordFilters { get; set; } = new FilterCollection() { Name = "Extracted text" };
        public FilterCollection WordFiltersSelected { get; set; } = new FilterCollection() { Name = "Selected text" };
        public FilterCollection ModerationFilters { get; set; } = new FilterCollection() { Name = "Content moderation" };
        public FilterCollection ColorFilters { get; set; } = new FilterCollection() { Name = "Color" };
        public FilterCollection OrientationFilters { get; set; } = new FilterCollection() { Name = "Orientation" };
        public FilterCollection ImageTypeFilters { get; set; } = new FilterCollection() { Name = "Image type" };
        public FilterCollection SizeFilters { get; set; } = new FilterCollection() { Name = "Size" };
        public FilterCollection AgeFilters { get; set; } = new FilterCollection() { Name = "Age" };
        public FilterCollection GenderFilters { get; set; } = new FilterCollection() { Name = "Gender" };
        public FilterCollection PeopleFilters { get; set; } = new FilterCollection() { Name = "Number of people" };
        public FilterCollection FaceAttributesFilters { get; set; } = new FilterCollection() { Name = "Face attributes" };
        public FilterCollection FaceQualityFilters { get; set; } = new FilterCollection() { Name = "Face image quality" };
        public FilterCollection CustomVisionTagFilters { get; set; } = new FilterCollection() { Name = "Custom Vision tags" };
        public FilterCollection CustomVisionObjectFilters { get; set; } = new FilterCollection() { Name = "Custom Vision objects" };

        public ImageFiltersViewModel()
        {
            //set fields
            _allFilters = new List<FilterCollection>() { TagFilters, FaceFilters, EmotionFilters, ObjectFilters, LandmarkFilters, CelebrityFilters, BrandFilters, WordFilters, ModerationFilters, ColorFilters, OrientationFilters, ImageTypeFilters, SizeFilters, AgeFilters, GenderFilters, PeopleFilters, FaceAttributesFilters, FaceQualityFilters, CustomVisionTagFilters, CustomVisionObjectFilters };
        }

        public void Clear()
        {
            FilteredResults.Clear();
            _allResults.Clear();
            ActiveFilters.Clear();
            foreach (var filter in _allFilters)
            {
                filter.Clear();
            }
        }

        public void ApplyFilters()
        {
            var activeFilters = _allFilters.SelectMany(i => i.Where(e => e.IsChecked));
            FilteredResults.AddRemoveRange(activeFilters.SelectMany(i => i.Parents).Distinct());
            if (FilteredResults.Count == 0 && _allFilters.SelectMany(i => i.Where(e => e.IsChecked)).Count() == 0)
            {
                FilteredResults.AddRemoveRange(_allResults);
            }
            ActiveFilters.AddRemoveRange(activeFilters);
        }

        public void ApplyWordsFilter(string text)
        {
            //select filter words
            var words = text.Trim().Split(' ');
            var hasChanged = false;
            foreach (var filter in WordFilters.Where(i => i.IsChecked && !words.Contains((string)i.Key, StringComparer.OrdinalIgnoreCase)))
            {
                filter.IsChecked = false;
                hasChanged = true;
            }
            foreach (var filter in WordFilters.Where(i => words.Contains((string)i.Key, StringComparer.OrdinalIgnoreCase)))
            {
                filter.IsChecked = true;
                hasChanged = true;
            }

            if (hasChanged)
            {
                //update filter
                ApplyFilters();

                //update selected filters
                WordFiltersSelected.AddRemoveRange(WordFilters.Where(i => i.IsChecked));
            }
        }

        public void AddImagesCompleted()
        {
            foreach (var filter in _allFilters)
            {
                //sort
                var reordered = filter.OrderByDescending(i => i.Parents.Count).ThenBy(i => i.Key).AsQueryable();

                //apply gender related filters
                if (!SettingsHelper.Instance.ShowAgeAndGender && filter.Count != 0 && filter.First().Key is string)
                {
                    reordered = reordered.Where(i => !Util.ContainsGenderRelatedKeyword(i.Key as string));
                }

                var items = reordered.ToList();
                filter.Clear();
                filter.AddRange(items);
            }
        }

        public async Task AddImage(ImageInsights insights)
        {
            // Load image from file
            BitmapImage bitmapImage = new BitmapImage();
            if (insights.ImageUri.IsFile)
            {
                await bitmapImage.SetSourceAsync((await (await StorageFile.GetFileFromPathAsync(insights.ImageUri.AbsoluteUri)).OpenStreamForReadAsync()).AsRandomAccessStream());
            }
            else
            {
                bitmapImage.UriSource = insights.ImageUri;
            }

            //load smaller image - for performace
            bitmapImage.DecodePixelHeight = 270;

            // Create the view models
            ImageInsightsViewModel insightsViewModel = new ImageInsightsViewModel() { Insights = insights, ImageSource = bitmapImage };

            //tags
            foreach (var entity in insights.VisionInsights?.Tags ?? Array.Empty<string>())
            {
                AddFilter(TagFilters, entity, entity, insightsViewModel);
            }

            //faces
            foreach (var entity in insights.FaceInsights ?? Array.Empty<FaceInsights>())
            {
                var key = entity.UniqueFaceId == Guid.Empty ? Guid.NewGuid() : entity.UniqueFaceId;
                var imageScale = bitmapImage.PixelHeight < bitmapImage.DecodePixelHeight ? 1d : (double)bitmapImage.DecodePixelHeight / (double)bitmapImage.PixelHeight;
                var filter = AddFilter(FaceFilters, entity, key, insightsViewModel, bitmapImage, entity.FaceRectangle.ToRect().Scale(imageScale).Inflate(2));

                //rescale face rect if image has been rescaled
                if (filter.Count == 1 && bitmapImage.PixelHeight == 0)
                {
                    bitmapImage.ImageOpened += (sender, e) =>
                    {
                        var bitmap = sender as BitmapImage;
                        if (bitmap.DecodePixelHeight != 0 && bitmap.DecodePixelHeight < bitmap.PixelHeight)
                        {
                            var imageFilter = filter as ImageFilterViewModel;
                            imageFilter.ImageCrop = entity.FaceRectangle.ToRect().Scale((double)bitmap.DecodePixelHeight / (double)bitmap.PixelHeight).Inflate(2);
                        }
                    };
                }
            }

            //emotions
            insightsViewModel.Emotions = insights.FaceInsights?.Select(i => Util.EmotionToRankedList(i.FaceAttributes.Emotion).First().Key).Distinct().ToArray() ?? Array.Empty<string>();
            foreach (var entity in insightsViewModel.Emotions)
            {
                AddFilter(EmotionFilters, entity, entity, insightsViewModel);
            }

            //objects
            foreach (var entity in insights.VisionInsights?.Objects ?? Array.Empty<string>())
            {
                AddFilter(ObjectFilters, entity, entity, insightsViewModel);
            }

            //landmarks
            foreach (var entity in insights.VisionInsights?.Landmarks ?? Array.Empty<string>())
            {
                AddFilter(LandmarkFilters, entity, entity, insightsViewModel);
            }

            //celebrities
            foreach (var entity in insights.VisionInsights?.Celebrities ?? Array.Empty<string>())
            {
                AddFilter(CelebrityFilters, entity, entity, insightsViewModel);
            }

            //brands
            foreach (var entity in insights.VisionInsights?.Brands ?? Array.Empty<string>())
            {
                AddFilter(BrandFilters, entity, entity, insightsViewModel);
            }

            //moderation
            insightsViewModel.Moderation = GetAdultFlags(insights.VisionInsights?.Adult).ToArray();
            foreach (var entity in insightsViewModel.Moderation)
            {
                AddFilter(ModerationFilters, entity, entity, insightsViewModel);
                insightsViewModel.BlurImage = true; //set blur flag
            }

            //words
            foreach (var entity in insights.VisionInsights?.Words ?? Array.Empty<string>())
            {
                AddFilter(WordFilters, entity, entity, insightsViewModel);
            }

            //color
            insightsViewModel.Color = GetColorFlags(insights.VisionInsights?.Color).ToArray();
            foreach (var entity in insightsViewModel.Color)
            {
                AddFilter(ColorFilters, entity, entity, insightsViewModel);
            }

            //orientation
            insightsViewModel.Orientation = GetOrientation(insights.VisionInsights?.Metadata);
            if (insightsViewModel.Orientation != null)
            {
                AddFilter(OrientationFilters, insightsViewModel.Orientation, insightsViewModel.Orientation, insightsViewModel);
            }

            //image type
            insightsViewModel.ImageType = GetImageTypeFlags(insights.VisionInsights?.ImageType).ToArray();
            foreach (var entity in insightsViewModel.ImageType)
            {
                AddFilter(ImageTypeFilters, entity, entity, insightsViewModel);
            }

            //size
            insightsViewModel.Size = GetSize(insights.VisionInsights?.Metadata);
            if (insightsViewModel.Size != null)
            {
                AddFilter(SizeFilters, insightsViewModel.Size, insightsViewModel.Size, insightsViewModel);
            }

            //People
            insightsViewModel.People = GetPeopleFlags(insights.VisionInsights?.Objects, insights.FaceInsights).ToArray();
            foreach (var entity in insightsViewModel.People)
            {
                AddFilter(PeopleFilters, entity, entity, insightsViewModel);
            }

            //face attributes
            insightsViewModel.FaceAttributes = GetFaceAttributesFlags(insights.FaceInsights).ToArray();
            foreach (var entity in insightsViewModel.FaceAttributes)
            {
                AddFilter(FaceAttributesFilters, entity, entity, insightsViewModel);
            }

            //face quality
            insightsViewModel.FaceQualtity = GetFaceQualityFlags(insights.FaceInsights).ToArray();
            foreach (var entity in insightsViewModel.FaceQualtity)
            {
                AddFilter(FaceQualityFilters, entity, entity, insightsViewModel);
            }

            //Custom Vision tags
            insightsViewModel.CustomVisionTags = GetCustomVisionTags(insights.CustomVisionInsights).ToArray();
            foreach (var entity in insightsViewModel.CustomVisionTags)
            {
                AddFilter(CustomVisionTagFilters, entity, entity, insightsViewModel);
            }

            //Custom Vision objects
            insightsViewModel.CustomVisionObjects = GetCustomVisionObjects(insights.CustomVisionInsights).ToArray();
            foreach (var entity in insightsViewModel.CustomVisionObjects)
            {
                AddFilter(CustomVisionObjectFilters, entity, entity, insightsViewModel);
            }

            if (SettingsHelper.Instance.ShowAgeAndGender) //only if age and gender is allowed
            {
                //Age
                insightsViewModel.Age = GetAgeFlags(insights.FaceInsights).ToArray();
                foreach (var entity in insightsViewModel.Age)
                {
                    AddFilter(AgeFilters, entity, entity, insightsViewModel);
                }

                //Gender
                insightsViewModel.Gender = GetGenderFlags(insights.FaceInsights).ToArray();
                foreach (var entity in insightsViewModel.Gender)
                {
                    AddFilter(GenderFilters, entity, entity, insightsViewModel);
                }
            }

            //add viewmodel to collection
            _allResults.Add(insightsViewModel);
            FilteredResults.Add(insightsViewModel);
        }

        TextFilterViewModel AddFilter(ICollection<TextFilterViewModel> filters, object entity, object key, ImageInsightsViewModel parent, ImageSource imageSource = null, Rect? imageCrop = null)
        {
            var filter = filters.FirstOrDefault(i => i.Key.Equals(key));
            if (filter == null)
            {
                //construct filter
                if (imageSource == null)
                {
                    filter = new TextFilterViewModel() { Entity = entity, Key = key };
                }
                else
                {
                    filter = new ImageFilterViewModel() { Entity = entity, Key = key, ImageSource = imageSource, ImageCrop = imageCrop ?? new Rect() };
                }

                filters.Add(filter);
            }
            if (!filter.Parents.Contains(parent))
            {
                filter.AddParent(parent);
            }
            return filter;
        }

        IEnumerable<string> GetAdultFlags(AdultInfo info)
        {
            var result = new List<string>();
            if (info != null)
            {
                if (info.IsAdultContent)
                {
                    result.Add("Adult");
                }
                if (info.IsRacyContent)
                {
                    result.Add("Racy");
                }
                if (info.IsGoryContent)
                {
                    result.Add("Gore");
                }
            }
            return result;
        }

        IEnumerable<string> GetColorFlags(ColorInfo color)
        {
            var result = new List<string>();
            if (color != null)
            {
                if (color.IsBWImg)
                {
                    result.Add("Black & White");
                    return result;
                }

                result.Add(color.DominantColorForeground);
                result.Add(color.DominantColorBackground);
            }
            return result.Distinct();
        }

        string GetOrientation(ImageMetadata metadata)
        {
            //validate
            if (metadata == null || metadata.Height == 0 || metadata.Width == 0)
            {
                return null;
            }

            var aspectRatio = (double)metadata.Height / (double)metadata.Width;
            return aspectRatio > 1 ? "Vertical" : "Horizontal";
        }

        IEnumerable<string> GetImageTypeFlags(ImageType imageType)
        {
            var result = new List<string>();
            if (imageType != null)
            {
                if (imageType.ClipArtType > 0)
                {
                    result.Add("Clip Art");
                }
                if (imageType.LineDrawingType > 0)
                {
                    result.Add("Line Drawing");
                }
            }
            return result;
        }

        string GetSize(ImageMetadata metadata)
        {
            //validate
            if (metadata == null || metadata.Height == 0 || metadata.Width == 0)
            {
                return null;
            }

            //get image size
            if (metadata.Height * metadata.Width >= 800000)
            {
                return "Large";
            }
            else if (metadata.Height * metadata.Width >= 120000)
            {
                return "Medium";
            }
            else if (metadata.Height * metadata.Width > 65536)
            {
                return "Small";
            }
            return "Icon";
        }

        IEnumerable<string> GetPeopleFlags(string[] objects, FaceInsights[] faces)
        {
            var result = new List<string>();

            //from person objects
            var objectCount = -1;
            if (objects != null)
            {
                objectCount = objects.Where(i => i == "person").Count();
            }

            //from faces
            var faceCount = -1;
            if (faces != null)
            {
                faceCount = faces.Length;
            }

            //pick the highest
            var peopleCount = objectCount > faceCount ? objectCount : faceCount;

            //create the results
            if (peopleCount == 0)
            {
                result.Add("contains no people");
            }
            else if (peopleCount > 0)
            {
                result.Add("contains a person");
            }
            if (peopleCount == 1)
            {
                result.Add("1 person");
            }
            else if (peopleCount == 2)
            {
                result.Add("2 people");
            }
            else if (peopleCount == 3)
            {
                result.Add("3 people");
            }
            else if (peopleCount >= 4)
            {
                result.Add("4 or more people");
            }
            return result;
        }

        IEnumerable<string> GetAgeFlags(FaceInsights[] faces)
        {
            var result = new List<string>();
            if (faces != null)
            {
                foreach (var face in faces)
                {
                    if (face.FaceAttributes.Age < 4)
                    {
                        result.Add("Infants");
                    }
                    else if (face.FaceAttributes.Age < 13)
                    {
                        result.Add("Children");
                    }
                    else if (face.FaceAttributes.Age < 20)
                    {
                        result.Add("Teenagers");
                    }
                    else if (face.FaceAttributes.Age < 30)
                    {
                        result.Add("20s");
                    }
                    else if (face.FaceAttributes.Age < 40)
                    {
                        result.Add("30s");
                    }
                    else if (face.FaceAttributes.Age < 50)
                    {
                        result.Add("40s");
                    }
                    else if (face.FaceAttributes.Age < 60)
                    {
                        result.Add("50s");
                    }
                    else if (face.FaceAttributes.Age < 70)
                    {
                        result.Add("60s");
                    }
                    else
                    {
                        result.Add("70s and older");
                    }
                }
            }
            return result.Distinct();
        }

        IEnumerable<string> GetGenderFlags(FaceInsights[] faces)
        {
            return faces?.Select(i => i.FaceAttributes.Gender.GetValueOrDefault(Face.Models.Gender.Genderless).ToString()).Distinct() ?? Array.Empty<string>();
        }

        IEnumerable<string> GetFaceAttributesFlags(FaceInsights[] faces)
        {
            var result = new List<string>();
            var threshhold = .49;
            if (faces != null)
            {
                foreach (var face in faces)
                {
                    //accessories
                    foreach (var accessory in face.FaceAttributes.Accessories?.Where(i => i.Confidence >= threshhold).Select(i => i.Type.ToString()) ?? Array.Empty<string>())
                    {
                        result.Add(accessory);
                    }
                    //Beard
                    if ((face.FaceAttributes.FacialHair?.Beard ?? 0) >= threshhold)
                    {
                        result.Add("Beard");
                    }
                    //Moustache
                    if ((face.FaceAttributes.FacialHair?.Moustache ?? 0) >= threshhold)
                    {
                        result.Add("Moustache");
                    }
                    //Sideburns
                    if ((face.FaceAttributes.FacialHair?.Sideburns ?? 0) >= threshhold)
                    {
                        result.Add("Sideburns");
                    }
                    //Bald
                    if ((face.FaceAttributes.Hair?.Bald ?? 0) >= threshhold)
                    {
                        result.Add("Bald");
                    }
                    //HairColor
                    var hairColor = (face.FaceAttributes.Hair?.HairColor ?? Array.Empty<HairColor>()).Where(i => i.Color != HairColorType.Unknown && i.Color != HairColorType.Other).OrderByDescending(i => i.Confidence).Select(i => i.Color.ToString()).FirstOrDefault();
                    if (hairColor != null)
                    {
                        result.Add(hairColor + " Hair");
                    }
                    //Hair invisible
                    if (face.FaceAttributes.Hair?.Invisible ?? false)
                    {
                        result.Add("Hair isn't visible");
                    }
                    //Eye Makup
                    if (face.FaceAttributes.Makeup?.EyeMakeup ?? false)
                    {
                        result.Add("Eye Makup");
                    }
                    //Lip Makup
                    if (face.FaceAttributes.Makeup?.LipMakeup ?? false)
                    {
                        result.Add("Lip Makup");
                    }
                    //glasses
                    var glasses = face.FaceAttributes.Glasses.GetValueOrDefault(Face.Models.GlassesType.NoGlasses);
                    if (glasses != GlassesType.NoGlasses)
                    {
                        switch (glasses)
                        {
                            case Face.Models.GlassesType.ReadingGlasses:
                                result.Add("Reading Glasses");
                                break;
                            case Face.Models.GlassesType.Sunglasses:
                                result.Add("Sunglasses");
                                break;
                            case Face.Models.GlassesType.SwimmingGoggles:
                                result.Add("Swimming Goggles");
                                break;
                            default:
                                result.Add(glasses.ToString());
                                break;
                        }
                    }
                    //head pose
                    if (face.FaceAttributes.HeadPose.Pitch.Between(-10, 10) && face.FaceAttributes.HeadPose.Roll.Between(-10, 10) && face.FaceAttributes.HeadPose.Yaw.Between(-10, 10))
                    {
                        result.Add("Facing Camera");
                    }
                    if (!face.FaceAttributes.HeadPose.Yaw.Between(-37, 37))
                    {
                        result.Add("Profile");
                    }
                    //Occlusion
                    if (face.FaceAttributes.Occlusion?.EyeOccluded ?? false)
                    {
                        result.Add("Eye isn't visible");
                    }
                    if (face.FaceAttributes.Occlusion?.ForeheadOccluded ?? false)
                    {
                        result.Add("Forehead isn't visible");
                    }
                    if (face.FaceAttributes.Occlusion?.MouthOccluded ?? false)
                    {
                        result.Add("Mouth isn't visible");
                    }
                }
            }
            return result.Distinct();
        }

        IEnumerable<string> GetFaceQualityFlags(FaceInsights[] faces)
        {
            var result = new List<string>();
            if (faces != null)
            {
                foreach (var face in faces)
                {
                    //Blur
                    var blur = face.FaceAttributes.Blur?.BlurLevel;
                    if (blur != null)
                    {
                        result.Add(blur.ToString() + " Blur");
                    }
                    //Exposure
                    var exposure = face.FaceAttributes.Exposure?.ExposureLevel;
                    if (exposure != null)
                    {
                        switch (exposure)
                        {
                            case ExposureLevel.UnderExposure:
                                result.Add("Under Exposure");
                                break;
                            case ExposureLevel.GoodExposure:
                                result.Add("Good Exposure");
                                break;
                            case ExposureLevel.OverExposure:
                                result.Add("Over Exposure");
                                break;
                        }
                    }
                    //Noise
                    var noise = face.FaceAttributes.Noise?.NoiseLevel;
                    if (noise != null)
                    {
                        result.Add(noise.ToString() + " Noise Level");
                    }
                }
            }
            return result.Distinct();
        }

        IEnumerable<string> GetCustomVisionTags(CustomVisionInsights[] customVision)
        {
            return customVision?.Where(i => !i.IsObjectDetection).SelectMany(i => i.Predictions.Where(e => e.Probability >= .6).Select(e => e.Name)) ?? Enumerable.Empty<string>();
        }

        IEnumerable<string> GetCustomVisionObjects(CustomVisionInsights[] customVision)
        {
            return customVision?.Where(i => i.IsObjectDetection).SelectMany(i => i.Predictions.Where(e => e.Probability >= .6).Select(e => e.Name)) ?? Enumerable.Empty<string>();
        }
    }

    public class ImageInsightsViewModel
    {
        public ImageInsights Insights { get; set; }
        public ImageSource ImageSource { get; set; }
        public bool BlurImage { get; set; }
        public string[] Emotions { get; set; }
        public string[] Moderation { get; set; }
        public string[] Color { get; set; }
        public string Orientation { get; set; }
        public string[] ImageType { get; set; }
        public string Size { get; set; }
        public string[] People { get; set; }
        public string[] FaceAttributes { get; set; }
        public string[] FaceQualtity { get; set; }
        public string[] CustomVisionTags { get; set; }
        public string[] CustomVisionObjects { get; set; }
        public string[] Age { get; set; }
        public string[] Gender { get; set; }
    }

    public class TextFilterViewModel : INotifyPropertyChanged
    {
        bool _isChecked;
        List<ImageInsightsViewModel> _parents = new List<ImageInsightsViewModel>();

        public event PropertyChangedEventHandler PropertyChanged;

        public object Entity { get; set; }
        public object Key { get; set; }
        public IReadOnlyList<ImageInsightsViewModel> Parents => _parents;
        public int Count { get; private set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }

        public void AddParent(ImageInsightsViewModel parent)
        {
            _parents.Add(parent);
            Count++;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }

    public class ImageFilterViewModel : TextFilterViewModel
    {
        Rect _imageCrop;

        public ImageSource ImageSource { get; set; }
        public Rect ImageCrop
        {
            get => _imageCrop;
            set
            {
                _imageCrop = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(ImageCrop)));
            }
        }
    }

    public class FilterCollection : ObservableCollection<TextFilterViewModel>
    {
        bool _isShowingAll;
        bool _showAllEnabled;
        string _name;

        public int ShowAllCount { get; set; } = 20;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(new PropertyChangedEventArgs(nameof(Name))); }
        }

        public bool IsShowingAll
        {
            get => _isShowingAll;
            set { _isShowingAll = value; OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsShowingAll))); }
        }

        public bool ShowAllEnabled
        {
            get => _showAllEnabled;
            protected set { _showAllEnabled = value; OnPropertyChanged(new PropertyChangedEventArgs(nameof(ShowAllEnabled))); }
        }

        protected override void InsertItem(int index, TextFilterViewModel item)
        {
            base.InsertItem(index, item);

            if (Count > ShowAllCount)
            {
                ShowAllEnabled = true;
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();

            ShowAllEnabled = false;
            IsShowingAll = false;
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);

            if (Count <= ShowAllCount)
            {
                ShowAllEnabled = false;
            }
        }
    }


    public static class Extensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }

        public static bool Between(this double value, double lowest, double highest)
        {
            return value >= lowest && value <= highest;
        }

        public static void AddRemoveRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            //get items to remove
            var toRemove = list.Except(items).ToArray();

            //get items to add
            var toAdd = items.Except(list).ToArray();

            //remove items
            foreach (var item in toRemove)
            {
                list.Remove(item);
            }

            //add items
            list.AddRange(toAdd);
        }
    }
}
