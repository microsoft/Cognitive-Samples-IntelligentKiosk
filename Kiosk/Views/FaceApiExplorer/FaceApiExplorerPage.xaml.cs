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

using IntelligentKioskSample.Controls.Overlays.Primitives;
using IntelligentKioskSample.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.FaceApiExplorer
{
    [KioskExperience(Id = "Face API Explorer",
        DisplayName = "Face API Explorer",
        Description = "See ages, genders, and landmarks from faces in an image",
        ImagePath = "ms-appx:/Assets/DemoGallery/Face API Explorer.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.BingAutoSuggest | TechnologyType.BingImages | TechnologyType.Face | TechnologyType.Emotion,
        TechnologyArea = TechnologyAreaType.Vision,
        DateUpdated = "2019/06/04",
        DateAdded = "2015/10/16")]
    public sealed partial class FaceApiExplorerPage : Page, INotifyPropertyChanged
    {
        const string NoneDesc = "None";

        public static bool ShowAgeAndGender { get { return SettingsHelper.Instance.ShowAgeAndGender; } }

        public ObservableCollection<ImageCrop<DetectedFaceViewModel>> DetectedFaceCollection { get; set; } = new ObservableCollection<ImageCrop<DetectedFaceViewModel>>();

        public TabHeader AppearanceTab { get; set; } = new TabHeader() { Name = "Appearance" };
        public TabHeader EmotionTab { get; set; } = new TabHeader() { Name = "Emotion" };
        public TabHeader PoseTab { get; set; } = new TabHeader() { Name = "Pose" };
        public TabHeader ImageQualityTab { get; set; } = new TabHeader() { Name = "Image quality" };

        private PivotItem selectedTab;
        public PivotItem SelectedTab
        {
            get { return selectedTab; }
            set
            {
                selectedTab = value;
                NotifyPropertyChanged("SelectedTab");
            }
        }

        private ImageCrop<DetectedFaceViewModel> currentDetectedFace;
        public ImageCrop<DetectedFaceViewModel> CurrentDetectedFace
        {
            get { return currentDetectedFace; }
            set
            {
                currentDetectedFace = value;
                NotifyPropertyChanged("CurrentDetectedFace");

                UpdateResultDetails();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public FaceApiExplorerPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await this.imagePicker.SetSuggestedImageList(
                new Uri("ms-appx:///Assets/DemoSamples/FaceApiExplorer/1.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/FaceApiExplorer/2.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/FaceApiExplorer/3.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/FaceApiExplorer/4.jpg"),
                new Uri("ms-appx:///Assets/DemoSamples/FaceApiExplorer/5.jpg"));
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            ImageAnalyzer image = args.First();
            image.ShowDialogOnFaceApiErrors = true;

            //detect faces
            OverlayPresenter.ItemsSource = null;
            OverlayPresenter.Source = await image.GetImageSource();
            if (image.DetectedFaces == null)
            {
                ProgressIndicator.IsActive = true;
                DisplayProcessingUI();

                // detect faces
                await image.DetectFacesAsync(true, true, new[] { FaceAttributeType.Accessories, FaceAttributeType.Age, FaceAttributeType.Blur, FaceAttributeType.Emotion, FaceAttributeType.Exposure, FaceAttributeType.FacialHair, FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair, FaceAttributeType.HeadPose, FaceAttributeType.Makeup, FaceAttributeType.Noise, FaceAttributeType.Occlusion, FaceAttributeType.Smile });

                // try to identify the faces
                await image.IdentifyFacesAsync();

                ProgressIndicator.IsActive = false;
            }

            //show results
            OverlayPresenter.ItemsSource = image.DetectedFaces.Select(i => new OverlayInfo() { Rect = i.FaceRectangle.ToRect() }).ToArray();
            await UpdateResultsAsync(image);
        }

        private async Task UpdateResultsAsync(ImageAnalyzer img)
        {
            try
            {
                DetectedFaceCollection.Clear();

                if (img.DetectedFaces.Any())
                {
                    // extract crops from the image
                    IList<DetectedFaceViewModel> detectedFaceViewModels = GetDetectedFaceViewModels(img.DetectedFaces, img.IdentifiedPersons);
                    var stream = img.ImageUrl == null ? await img.GetImageStreamCallback() : new MemoryStream(await new HttpClient().GetByteArrayAsync(img.ImageUrl));

                    using (stream)
                    {
                        var faces = await Util.GetImageCrops(detectedFaceViewModels, i => i.FaceRectangle.ToRect(), stream.AsRandomAccessStream());
                        if (faces != null)
                        {
                            DetectedFaceCollection.AddRange(faces);
                        }
                    }
                }

                CurrentDetectedFace = DetectedFaceCollection.FirstOrDefault();
                this.notFoundGrid.Visibility = DetectedFaceCollection.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private void UpdateResultDetails()
        {
            DetectedFaceViewModel detectedFace = CurrentDetectedFace?.Entity;
            FaceAttributes faceAttributes = detectedFace?.FaceAttributes;
            if (faceAttributes != null)
            {
                if (ShowAgeAndGender)
                {
                    // gender
                    this.genderTextBlock.Text = faceAttributes.Gender.HasValue ? Util.UppercaseFirst(faceAttributes.Gender.ToString()) : NoneDesc;

                    // age
                    this.ageTextBlock.Text = faceAttributes.Age.HasValue ? faceAttributes.Age.ToString() : NoneDesc;
                }

                // hair color
                this.haircolorsGridView.ItemsSource = faceAttributes.Hair?.HairColor != null && faceAttributes.Hair.HairColor.Any()
                    ? faceAttributes.Hair.HairColor.Where(x => x.Confidence >= 0.6).Select(x => new { Confidence = string.Format("({0}%)", Math.Round(x.Confidence * 100)), HairColor = x.Color.ToString() })
                    : (object)(new[] { new { HairColor = NoneDesc } });

                // facial hair
                var facialHair = new List<KeyValuePair<string, double>>()
                {
                    new KeyValuePair<string, double>("Moustache", faceAttributes.FacialHair?.Moustache ?? 0),
                    new KeyValuePair<string, double>("Beard", faceAttributes.FacialHair?.Beard ?? 0),
                    new KeyValuePair<string, double>("Sideburns", faceAttributes.FacialHair?.Sideburns ?? 0)
                };
                if (facialHair.Any(x => x.Value > 0))
                {
                    this.facialHairGridView.ItemsSource = facialHair.Select(x => new { Value = 100 * x.Value, Type = x.Key });
                    this.facialHairGridView.Visibility = Visibility.Visible;
                    this.facialHairTextBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.facialHairTextBlock.Text = NoneDesc;
                    this.facialHairGridView.Visibility = Visibility.Collapsed;
                    this.facialHairTextBlock.Visibility = Visibility.Visible;
                }

                // glasses
                this.glassesTextBlock.Text = faceAttributes.Glasses.HasValue && faceAttributes.Glasses != GlassesType.NoGlasses ? faceAttributes.Glasses.ToString() : NoneDesc;

                // makeup
                var makeup = new List<string>()
                {
                    faceAttributes.Makeup != null && faceAttributes.Makeup.EyeMakeup ? "Eye" : string.Empty,
                    faceAttributes.Makeup != null && faceAttributes.Makeup.LipMakeup ? "Lip" : string.Empty
                };
                this.makeupTextBlock.Text = makeup.Any(x => !string.IsNullOrEmpty(x)) ? string.Join(", ", makeup.Where(x => !string.IsNullOrEmpty(x))) : NoneDesc;

                // accessories
                this.accessoriesGridView.ItemsSource = faceAttributes.Accessories != null && faceAttributes.Accessories.Any()
                    ? faceAttributes.Accessories.Where(x => x.Confidence >= 0.6).Select(x => new { Confidence = string.Format("({0}%)", Math.Round(x.Confidence * 100)), Accessory = x.Type.ToString() })
                    : (object)(new[] { new { Accessory = NoneDesc } });


                // emotion
                var emotionList = new List<KeyValuePair<string, double>>()
                {
                    new KeyValuePair<string, double>("Anger", faceAttributes.Emotion?.Anger ?? 0),
                    new KeyValuePair<string, double>("Contempt", faceAttributes.Emotion?.Contempt ?? 0),
                    new KeyValuePair<string, double>("Disgust", faceAttributes.Emotion?.Disgust ?? 0),
                    new KeyValuePair<string, double>("Fear", faceAttributes.Emotion?.Fear ?? 0),
                    new KeyValuePair<string, double>("Happiness", faceAttributes.Emotion?.Happiness ?? 0),
                    new KeyValuePair<string, double>("Neutral", faceAttributes.Emotion?.Neutral ?? 0),
                    new KeyValuePair<string, double>("Sadness", faceAttributes.Emotion?.Sadness ?? 0),
                    new KeyValuePair<string, double>("Surprise", faceAttributes.Emotion?.Surprise ?? 0)
                };
                var detectedEmotions = emotionList.Where(x => x.Value > 0).Select(x => new { Value = 100 * x.Value, Type = x.Key });
                string notDetectedEmotions = string.Join(", ", emotionList.Where(x => x.Value <= 0).Select(x => x.Key));
                if (detectedEmotions.Any())
                {
                    this.detectedEmotionGridView.ItemsSource = detectedEmotions;
                    this.detectedEmotionGridView.Visibility = Visibility.Visible;
                    this.detectedEmotionTextBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.detectedEmotionTextBlock.Text = NoneDesc;
                    this.detectedEmotionTextBlock.Visibility = Visibility.Visible;
                    this.detectedEmotionGridView.Visibility = Visibility.Collapsed;
                }
                this.notDetectedEmotionTextBlock.Text = !string.IsNullOrEmpty(notDetectedEmotions) ? notDetectedEmotions : NoneDesc;

                // pose
                double rollAngle = faceAttributes.HeadPose?.Roll ?? 0;
                string rollDirection = rollAngle > 0 ? "right" : "left";
                this.headTiltTextBlock.Text = rollAngle != 0 ? $"{Math.Abs(rollAngle)}° {rollDirection}" : string.Empty;
                this.headTiltControl.DrawFacePoseData(rollAngle, angleArr: new double[] { -60, -30, 0, 30, 60 });

                double pitchAngle = faceAttributes.HeadPose?.Pitch ?? 0;
                string pitchDirection = pitchAngle > 0 ? "up" : "down";
                this.chinAngleTextBlock.Text = pitchAngle != 0 ? $"{Math.Abs(pitchAngle)}° {pitchDirection}" : string.Empty;
                this.chinAngleControl.DrawFacePoseData(pitchAngle, angleArr: new double[] { 60, 30, 0, -30, -60 });

                double yawAngle = faceAttributes.HeadPose?.Yaw ?? 0;
                string yawDirection = yawAngle > 0 ? "right" : "left";
                this.faceRotationTextBlock.Text = yawAngle != 0 ? $"{Math.Abs(yawAngle)}° {yawDirection}" : string.Empty;
                this.faceRotationControl.DrawFacePoseData(yawAngle, angleArr: new double[] { 60, 30, 0, -30, -60 });

                // exposure
                this.expouseTextBlock.Text = faceAttributes.Exposure?.ExposureLevel != null ? Util.UppercaseFirst(faceAttributes.Exposure.ExposureLevel.ToString()) : NoneDesc;
                this.expouseProgressBar.Value = faceAttributes.Exposure != null ? 100 * faceAttributes.Exposure.Value : 0;

                // blur
                this.blurTextBlock.Text = faceAttributes.Blur?.BlurLevel != null ? Util.UppercaseFirst(faceAttributes.Blur.BlurLevel.ToString()) : NoneDesc;
                this.blurProgressBar.Value = faceAttributes.Blur != null ? 100 * faceAttributes.Blur.Value : 0;

                // noise
                this.noiseTextBlock.Text = faceAttributes.Noise?.NoiseLevel != null ? Util.UppercaseFirst(faceAttributes.Noise.NoiseLevel.ToString()) : NoneDesc;
                this.noiseProgressBar.Value = faceAttributes.Noise != null ? 100 * faceAttributes.Noise.Value : 0;

                // occlusion
                var occlusionList = new List<string>()
                {
                    faceAttributes.Occlusion != null && faceAttributes.Occlusion.ForeheadOccluded ? "Forehead" : string.Empty,
                    faceAttributes.Occlusion != null && faceAttributes.Occlusion.EyeOccluded ? "Eye" : string.Empty,
                    faceAttributes.Occlusion != null && faceAttributes.Occlusion.MouthOccluded ? "Mouth" : string.Empty
                };
                this.occlusionTextBlock.Text = occlusionList.Any(x => !string.IsNullOrEmpty(x)) ? string.Join(", ", occlusionList.Where(x => !string.IsNullOrEmpty(x))) : NoneDesc;
            }

            ShowFaceLandmarks();
        }

        private IList<DetectedFaceViewModel> GetDetectedFaceViewModels(IEnumerable<DetectedFace> detectedFaces, IEnumerable<IdentifiedPerson> identifiedPersons)
        {
            var result = new List<DetectedFaceViewModel>();
            foreach (var (face, index) in detectedFaces.Select((v, i) => (v, i)))
            {
                string faceTitle = $"Face {index + 1}";
                IdentifiedPerson identifiedPerson = identifiedPersons?.FirstOrDefault(x => x.FaceId == face.FaceId);
                if (identifiedPerson?.Person != null)
                {
                    faceTitle = $"{identifiedPerson.Person.Name} ({(uint)Math.Round(identifiedPerson.Confidence * 100)}%)";
                }
                else if (ShowAgeAndGender)
                {
                    var genderWithAge = new List<string>() { face.FaceAttributes.Gender?.ToString() ?? string.Empty, face.FaceAttributes.Age?.ToString() ?? string.Empty };
                    faceTitle = string.Join(", ", genderWithAge.Where(x => !string.IsNullOrEmpty(x)));
                }

                KeyValuePair<string, double> topEmotion = Util.EmotionToRankedList(face.FaceAttributes.Emotion).FirstOrDefault();
                var faceDescription = new List<string>()
                {
                    face.FaceAttributes.Hair.HairColor.Any() ? $"{face.FaceAttributes.Hair.HairColor.OrderByDescending(x => x.Confidence).First().Color} hair" : string.Empty,
                    topEmotion.Key != null ? $"{topEmotion.Key} expression" : string.Empty
                };
                result.Add(new DetectedFaceViewModel()
                {
                    FaceRectangle = face.FaceRectangle,
                    FaceAttributes = face.FaceAttributes,
                    FaceLandmarks = face.FaceLandmarks,
                    IdentifiedPerson = identifiedPerson,
                    FaceTitle = faceTitle,
                    FaceDescription = string.Join(", ", faceDescription.Where(x => !string.IsNullOrEmpty(x)))
                });
            }
            return result;
        }

        private void OnFaceImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowFaceLandmarks();
        }

        private void OnShowFaceLandmarksToggleChanged(object sender, RoutedEventArgs e)
        {
            ShowFaceLandmarks();
        }

        private void ShowFaceLandmarks()
        {
            DetectedFaceViewModel face = CurrentDetectedFace?.Entity;
            if (this.showFaceLandmarksToggle.IsOn && face != null)
            {
                double scaleX = this.faceImage.RenderSize.Width / face.FaceRectangle.Width;
                double scaleY = this.faceImage.RenderSize.Height / face.FaceRectangle.Height;

                this.faceLandmarksControl.DisplayFaceLandmarks(face.FaceRectangle, face.FaceLandmarks, scaleX, scaleY);
            }
            else
            {
                this.faceLandmarksControl.HideFaceLandmarks();
            }
        }

        private void DisplayProcessingUI()
        {
            CurrentDetectedFace = null;
            DetectedFaceCollection.Clear();
            this.progressRing.IsActive = true;
        }
    }

    public class DetectedFaceViewModel
    {
        public FaceRectangle FaceRectangle { get; set; }
        public FaceLandmarks FaceLandmarks { get; set; }
        public FaceAttributes FaceAttributes { get; set; }
        public IdentifiedPerson IdentifiedPerson { get; set; }
        public string FaceTitle { get; set; }
        public string FaceDescription { get; set; }
    }
}
