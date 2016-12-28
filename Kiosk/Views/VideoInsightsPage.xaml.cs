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

using IntelligentKioskSample.Controls;
using KioskRuntimeComponent;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Emotion.Contract;
using Newtonsoft.Json.Linq;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Media.Effects;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{

    [KioskExperience(Title = "Realtime Video Insights", ImagePath = "ms-appx:/Assets/realtimeFromVideo.png", ExperienceType = ExperienceType.Other)]
    public sealed partial class VideoInsightsPage : Page
    {
        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;
        private Guid currentVideoId;

        private Dictionary<Guid, Visitor> peopleInVideo = new Dictionary<Guid, Visitor>();
        private DemographicsData demographics;
        private Dictionary<Guid, int> pendingIdentificationAttemptCount = new Dictionary<Guid, int>();
        private HashSet<int> processedFrames = new HashSet<int>();

        public event PropertyChangedEventHandler PropertyChanged;

        public VideoInsightsPage()
        {
            this.InitializeComponent();

            this.DataContext = this;
        }

        private void StartProcessingLoop()
        {
            this.isProcessingLoopInProgress = true;

            if (this.processingLoopTask == null || this.processingLoopTask.Status != TaskStatus.Running)
            {
                this.processingLoopTask = Task.Run(() => this.ProcessingLoop());
            }
        }

        private async void ProcessingLoop()
        {
            while (this.isProcessingLoopInProgress)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (!this.isProcessingPhoto && 
                        FrameRelayVideoEffect.LatestSoftwareBitmap != null && 
                        (this.videoPlayer.CurrentState == MediaElementState.Playing || this.videoPlayer.CurrentState == MediaElementState.Paused))
                    {
                        this.isProcessingPhoto = true;
                        try
                        {
                            await this.ProcessCurrentVideoFrame();
                        }
                        catch
                        { }
                        finally
                        {
                            this.isProcessingPhoto = false;
                        }
                    }

                    if (this.videoPlayer.CurrentState == MediaElementState.Playing || this.videoPlayer.CurrentState == MediaElementState.Paused)
                    {
                        this.timelineTip.Margin = new Thickness
                        {
                            Left = (this.videoPlayer.Position.TotalSeconds / this.videoPlayer.NaturalDuration.TimeSpan.TotalSeconds) * (this.timelineTipHost.ActualWidth - 20)
                        };
                    }
                });
            }
        }

        private async Task ProcessCurrentVideoFrame()
        {
            int frameNumber = (int)this.videoPlayer.Position.TotalSeconds;

            if (this.processedFrames.Contains(frameNumber))
            {
                return;
            }

            Guid videoIdBeforeProcessing = this.currentVideoId;

            var analyzer = new ImageAnalyzer(await Util.GetPixelBytesFromSoftwareBitmapAsync(FrameRelayVideoEffect.LatestSoftwareBitmap));

            DateTime start = DateTime.Now;

            // Compute Emotion, Age and Gender
            await Task.WhenAll(analyzer.DetectEmotionAsync(), analyzer.DetectFacesAsync(detectFaceAttributes: true));

            // Compute Face Identification and Unique Face Ids
            await Task.WhenAll(analyzer.IdentifyFacesAsync(), analyzer.FindSimilarPersistedFacesAsync());

            foreach (var item in analyzer.SimilarFaceMatches)
            {
                if (videoIdBeforeProcessing != this.currentVideoId)
                {
                    // Media source changed while we were processing. Make sure we are in a clear state again.
                    await this.ResetStateAsync();
                    break;
                }

                bool demographicsChanged = false;
                Visitor personInVideo;
                if (this.peopleInVideo.TryGetValue(item.SimilarPersistedFace.PersistedFaceId, out personInVideo))
                {
                    personInVideo.Count++;

                    if (this.pendingIdentificationAttemptCount.ContainsKey(item.SimilarPersistedFace.PersistedFaceId))
                    {
                        // This is a face we haven't identified yet. See how many times we have tried it, if we need to do it again or stop trying
                        if (this.pendingIdentificationAttemptCount[item.SimilarPersistedFace.PersistedFaceId] <= 5)
                        {
                            string personName = await GetDisplayTextForPersonAsync(analyzer, item);
                            if (string.IsNullOrEmpty(personName))
                            {
                                // Increment the times we have tried and failed to identify this person
                                this.pendingIdentificationAttemptCount[item.SimilarPersistedFace.PersistedFaceId]++;
                            }
                            else
                            {
                                // Bingo! Let's remove it from the list of pending identifications
                                this.pendingIdentificationAttemptCount.Remove(item.SimilarPersistedFace.PersistedFaceId);

                                VideoTrack existingTrack = (VideoTrack)this.peopleListView.Children.FirstOrDefault(f => (Guid)((FrameworkElement)f).Tag == item.SimilarPersistedFace.PersistedFaceId);
                                if (existingTrack != null)
                                {
                                    existingTrack.DisplayText = string.Format("{0}, {1}", personName, Math.Floor(item.Face.FaceAttributes.Age));
                                }
                            }
                        }
                        else
                        {
                            // Give up
                            this.pendingIdentificationAttemptCount.Remove(item.SimilarPersistedFace.PersistedFaceId);
                        }
                    }
                }
                else
                {
                    // New person... let's catalog it.

                    // Crop the face, enlarging the rectangle so we frame it better
                    double heightScaleFactor = 1.8;
                    double widthScaleFactor = 1.8;
                    Rectangle biggerRectangle = new Rectangle
                    {
                        Height = Math.Min((int)(item.Face.FaceRectangle.Height * heightScaleFactor), FrameRelayVideoEffect.LatestSoftwareBitmap.PixelHeight),
                        Width = Math.Min((int)(item.Face.FaceRectangle.Width * widthScaleFactor), FrameRelayVideoEffect.LatestSoftwareBitmap.PixelWidth)
                    };
                    biggerRectangle.Left = Math.Max(0, item.Face.FaceRectangle.Left - (int)(item.Face.FaceRectangle.Width * ((widthScaleFactor - 1) / 2)));
                    biggerRectangle.Top = Math.Max(0, item.Face.FaceRectangle.Top - (int)(item.Face.FaceRectangle.Height * ((heightScaleFactor - 1) / 1.4)));

                    var croppedImage = await Util.GetCroppedBitmapAsync(analyzer.GetImageStreamCallback, biggerRectangle);

                    if (croppedImage == null || biggerRectangle.Height == 0 && biggerRectangle.Width == 0)
                    {
                        // Couldn't get a shot of this person
                        continue;
                    }

                    demographicsChanged = true;

                    string personName = await GetDisplayTextForPersonAsync(analyzer, item);
                    if (string.IsNullOrEmpty(personName))
                    {
                        personName = item.Face.FaceAttributes.Gender;

                        // Add the person to the list of pending identifications so we can try again on some future frames
                        this.pendingIdentificationAttemptCount.Add(item.SimilarPersistedFace.PersistedFaceId, 1);
                    }

                    personInVideo = new Visitor { UniqueId = item.SimilarPersistedFace.PersistedFaceId };
                    this.peopleInVideo.Add(item.SimilarPersistedFace.PersistedFaceId, personInVideo);
                    this.demographics.Visitors.Add(personInVideo);

                    // Update the demographics stats. 
                    this.UpdateDemographics(item);

                    VideoTrack videoTrack = new VideoTrack
                    {
                        Tag = item.SimilarPersistedFace.PersistedFaceId,
                        CroppedFace = croppedImage,
                        DisplayText = string.Format("{0}, {1}", personName, Math.Floor(item.Face.FaceAttributes.Age)),
                        Duration = (int)this.videoPlayer.NaturalDuration.TimeSpan.TotalSeconds,
                    };

                    videoTrack.Tapped += this.TimelineTapped;

                    this.peopleListView.Children.Add(videoTrack);
                }

                // Update the timeline for this person
                VideoTrack track = (VideoTrack) this.peopleListView.Children.FirstOrDefault(f => (Guid)((FrameworkElement)f).Tag == item.SimilarPersistedFace.PersistedFaceId);
                if (track != null)
                {
                    Emotion matchingEmotion = CoreUtil.FindFaceClosestToRegion(analyzer.DetectedEmotion, item.Face.FaceRectangle);
                    if (matchingEmotion == null)
                    {
                        matchingEmotion = new Emotion { Scores = new Scores { Neutral = 1 } };
                    }

                    track.SetVideoFrameState(frameNumber, matchingEmotion.Scores);
                }

                if (demographicsChanged)
                {
                    this.ageGenderDistributionControl.UpdateData(this.demographics);
                }

                this.overallStatsControl.UpdateData(this.demographics);
            }

            debugText.Text = string.Format("Latency: {0:0}ms", (DateTime.Now - start).TotalMilliseconds);

            this.processedFrames.Add(frameNumber);
        }

        private void UpdateDemographics(SimilarFaceMatch item)
        {
            AgeDistribution genderBasedAgeDistribution = null;
            if (string.Compare(item.Face.FaceAttributes.Gender, "male", StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.demographics.OverallMaleCount++;
                genderBasedAgeDistribution = this.demographics.AgeGenderDistribution.MaleDistribution;
            }
            else
            {
                this.demographics.OverallFemaleCount++;
                genderBasedAgeDistribution = this.demographics.AgeGenderDistribution.FemaleDistribution;
            }

            if (item.Face.FaceAttributes.Age < 16)
            {
                genderBasedAgeDistribution.Age0To15++;
            }
            else if (item.Face.FaceAttributes.Age < 20)
            {
                genderBasedAgeDistribution.Age16To19++;
            }
            else if (item.Face.FaceAttributes.Age < 30)
            {
                genderBasedAgeDistribution.Age20s++;
            }
            else if (item.Face.FaceAttributes.Age < 40)
            {
                genderBasedAgeDistribution.Age30s++;
            }
            else if (item.Face.FaceAttributes.Age < 50)
            {
                genderBasedAgeDistribution.Age40s++;
            }
            else
            {
                genderBasedAgeDistribution.Age50sAndOlder++;
            }
        }

        private static async Task<string> GetDisplayTextForPersonAsync(ImageAnalyzer analyzer, SimilarFaceMatch item)
        {
            // See if we identified this person against a trained model
            IdentifiedPerson identifiedPerson = analyzer.IdentifiedPersons.FirstOrDefault(p => p.FaceId == item.Face.FaceId);

            if (identifiedPerson != null)
            {
                return identifiedPerson.Person.Name;
            }

            if (identifiedPerson == null)
            {
                // Let's see if this is a celebrity
                if (analyzer.AnalysisResult == null)
                {
                    await analyzer.IdentifyCelebrityAsync();
                }

                if (analyzer.AnalysisResult?.Categories != null)
                {
                    foreach (var category in analyzer.AnalysisResult.Categories.Where(c => c.Detail != null))
                    {
                        dynamic detail = JObject.Parse(category.Detail.ToString());
                        if (detail.celebrities != null)
                        {
                            foreach (var celebrity in detail.celebrities)
                            {
                                uint left = UInt32.Parse(celebrity.faceRectangle.left.ToString());
                                uint top = UInt32.Parse(celebrity.faceRectangle.top.ToString());
                                uint height = UInt32.Parse(celebrity.faceRectangle.height.ToString());
                                uint width = UInt32.Parse(celebrity.faceRectangle.width.ToString());

                                if (Util.AreFacesPotentiallyTheSame(new BitmapBounds { Height = height, Width = width, X = left, Y = top }, item.Face.FaceRectangle))
                                {
                                    return celebrity.name.ToString();
                                }
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private async Task ResetStateAsync()
        {
            this.peopleListView.Children.Clear();
            this.peopleInVideo.Clear();
            await FaceListManager.ResetFaceLists();

            this.demographics = new DemographicsData
            {
                AgeGenderDistribution = new AgeGenderDistribution { FemaleDistribution = new AgeDistribution(), MaleDistribution = new AgeDistribution() },
                Visitors = new List<Visitor>()
            };

            this.pendingIdentificationAttemptCount.Clear();
            this.processedFrames.Clear();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.EnterKioskMode();

            if (string.IsNullOrEmpty(SettingsHelper.Instance.EmotionApiKey) || 
                string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey) ||
                string.IsNullOrEmpty(SettingsHelper.Instance.VisionApiKey))
            {
                await new MessageDialog("Missing Face, Emotion or Vision API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }

            FaceListManager.FaceListsUserDataFilter = SettingsHelper.Instance.WorkspaceKey + "_RealTimeFromVideo";
            await FaceListManager.Initialize();

            base.OnNavigatedTo(e);
        }

        private void EnterKioskMode()
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
            }
        }

        private async void FromFileClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Downloads, ViewMode = PickerViewMode.Thumbnail };
            fileOpenPicker.FileTypeFilter.Add(".mp4");

            var pickedFile = await fileOpenPicker.PickSingleFileAsync();
            if (pickedFile != null)
            {

                //Set the stream source to the MediaElement control
                await this.StartVideoAsync(pickedFile);
                StartProcessingLoop();
            }
        }

        private async Task StartVideoAsync(StorageFile file)
        {
            try
            {
                if (this.videoPlayer.CurrentState == MediaElementState.Playing)
                {
                    this.videoPlayer.Stop();
                }

                FrameRelayVideoEffect.ResetState();

                this.currentVideoId = Guid.NewGuid();
                await this.ResetStateAsync();

                MediaClip clip = await MediaClip.CreateFromFileAsync(file);
                clip.VideoEffectDefinitions.Add(new VideoEffectDefinition(typeof(FrameRelayVideoEffect).FullName));

                MediaComposition compositor = new MediaComposition();
                compositor.Clips.Add(clip);

                this.videoPlayer.SetMediaStreamSource(compositor.GenerateMediaStreamSource());
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error starting playback.");
            }
        }

        private void TimelineTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (this.videoPlayer.CurrentState == MediaElementState.Playing || this.videoPlayer.CurrentState == MediaElementState.Paused)
            {
                var position = e.GetPosition(this.timelineRectangle);
                if (position.X > 0)
                {
                    this.videoPlayer.Position = TimeSpan.FromSeconds((position.X / this.timelineRectangle.ActualWidth) * this.videoPlayer.NaturalDuration.TimeSpan.TotalSeconds);
                }
            }
        }
    }

    public class DurationToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((Duration)value).TimeSpan.ToString("h\\:mm\\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new Duration(TimeSpan.Parse(value.ToString()));
        }
    }
}