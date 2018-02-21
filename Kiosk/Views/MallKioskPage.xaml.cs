using IntelligentKioskSample.Controls;
using IntelligentKioskSample.MallKioskPageConfig;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using ServiceHelpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace IntelligentKioskSample.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [KioskExperience(Title = "Mall Kiosk", ImagePath = "ms-appx:/Assets/mall.png", ExperienceType = ExperienceType.Kiosk)]
    public sealed partial class MallKioskPage : Page
    {
        private MallKioskDemoSettings kioskSettings;
        private Recommendation currentRecommendation;
        public ObservableCollection<EmotionExpressionCapture> EmotionFaces { get; set; } = new ObservableCollection<EmotionExpressionCapture>();

        public MallKioskPage()
        {
            this.InitializeComponent();

            this.cameraControl.ImageCaptured += CameraControl_ImageCaptured;
            this.cameraControl.CameraRestarted += CameraControl_CameraRestarted;
            this.cameraControl.FilterOutSmallFaces = true;

            this.speechToTextControl.SpeechRecognitionAndSentimentProcessed += OnSpeechRecognitionAndSentimentProcessed;

            this.emotionFacesGrid.DataContext = this;
        }

        private async void CameraControl_CameraRestarted(object sender, EventArgs e)
        {
            await this.ResetRecommendationUI();
        }

        private async Task ResetRecommendationUI()
        {
            this.webView.NavigateToString("");
            this.webView.Visibility = Visibility.Collapsed;

            // We induce a delay here to give the camera some time to start rendering before we hide the last captured photo.
            // This avoids a black flash.
            await Task.Delay(500);

            this.imageFromCameraWithFaces.DataContext = null;
            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;
        }

        private void OnSpeechRecognitionAndSentimentProcessed(object sender, SpeechRecognitionAndSentimentResult e)
        {
            if (this.currentRecommendation != null)
            {
                string alternativeUrl = null;

                // See if there is an alternative result based on keywords in the spoken text
                if (this.currentRecommendation.SpeechKeywordBehavior != null)
                {
                    BehaviorAction behaviorAction = this.currentRecommendation.SpeechKeywordBehavior.FirstOrDefault(behavior => e.SpeechRecognitionText.IndexOf(behavior.Key, StringComparison.OrdinalIgnoreCase) != -1);
                    if (behaviorAction != null)
                    {
                        alternativeUrl = behaviorAction.Url;
                    }
                }

                // If we didn't find an alternative based on keywords see if we have a generic one based on the sentiment of the spoken text
                if (string.IsNullOrEmpty(alternativeUrl) && this.currentRecommendation.SpeechSentimentBehavior != null)
                {
                    BehaviorAction behaviorAction = null;
                    if (e.TextAnalysisSentiment <= 0.33)
                    {
                        // look for an override for negative sentiment
                        behaviorAction = this.currentRecommendation.SpeechSentimentBehavior.FirstOrDefault(behavior => string.Compare(behavior.Key, "Negative", true) == 0);
                    }
                    else if (e.TextAnalysisSentiment >= 0.66)
                    {
                        // look for an override for positive sentiment
                        behaviorAction = this.currentRecommendation.SpeechSentimentBehavior.FirstOrDefault(behavior => string.Compare(behavior.Key, "Positive", true) == 0);
                    }

                    if (behaviorAction != null)
                    {
                        alternativeUrl = behaviorAction.Url;
                    }
                }

                if (!string.IsNullOrEmpty(alternativeUrl))
                {
                    webView.Navigate(new Uri(alternativeUrl));
                }
            }
        }

        private async void CameraControl_ImageCaptured(object sender, ImageAnalyzer e)
        {
            this.imageFromCameraWithFaces.DataContext = e;
            this.imageFromCameraWithFaces.Visibility = Visibility.Visible;

            // We induce a delay here to give the captured image some time to render before we hide the camera.
            // This avoids a black flash.
            await Task.Delay(50);

            await this.cameraControl.StopStreamAsync();

            e.FaceRecognitionCompleted += (s, args) =>
            {
                ShowRecommendations(e);
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsHelper.Instance.MallKioskDemoCustomSettings))
            {
                try
                {
                    string escapedContent = SettingsHelper.Instance.MallKioskDemoCustomSettings.Replace("&", "&amp;");
                    this.kioskSettings = await MallKioskDemoSettings.FromContentAsync(escapedContent);
                }
                catch (Exception ex)
                {
                    await Util.GenericApiCallExceptionHandler(ex, "Failure parsing custom recommendation URLs. Will use default values instead.");
                }
            }

            if (this.kioskSettings == null)
            {
                this.kioskSettings = await MallKioskDemoSettings.FromFileAsync("Views\\MallKioskDemoConfig\\MallKioskDemoSettings.xml");
            }

            EnterKioskMode();

            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey))
            {
                await new MessageDialog("Missing Face API Key. Please enter a key in the Settings page.", "Missing Face API Key").ShowAsync();
            }

            await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
            this.UpdateWebCamHostGridSize();
            this.StartEmotionProcessingLoop();

            base.OnNavigatedTo(e);
        }

        private void UpdateWebCamHostGridSize()
        {
            this.webCamHostGrid.Width = Math.Round(this.ActualWidth * 0.25);
            this.webCamHostGrid.Height = Math.Round(this.webCamHostGrid.Width / (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777));
        }

        private void EnterKioskMode()
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            this.isProcessingLoopInProgress = false;
            await this.cameraControl.StopStreamAsync();
            this.speechToTextControl.DisposeSpeechRecognizer();
            base.OnNavigatingFrom(e);
        }

        private void ShowRecommendations(ImageAnalyzer imageWithFaces)
        {
            Recommendation recommendation = null;
            this.currentRecommendation = null;

            int numberOfPeople = imageWithFaces.DetectedFaces.Count();
            if (numberOfPeople == 1)
            {
                // Single person
                IdentifiedPerson identifiedPerson = imageWithFaces.IdentifiedPersons.FirstOrDefault();
                if (identifiedPerson != null)
                {
                    // See if we have a personalized recommendation for this person.
                    recommendation = this.kioskSettings.PersonalizedRecommendations.FirstOrDefault(r => r.Id.Equals(identifiedPerson.Person.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (recommendation == null)
                {
                    // Didn't find a personalized recommendation (or we don't have anyone recognized), so default to 
                    // the age/gender-based generic recommendation
                    Face face = imageWithFaces.DetectedFaces.First();
                    recommendation = this.kioskSettings.GetGenericRecommendationForPerson((int)face.FaceAttributes.Age, face.FaceAttributes.Gender);
                }
            }
            else if (numberOfPeople > 1 && imageWithFaces.DetectedFaces.Any(f => f.FaceAttributes.Age <= 12) &&
                     imageWithFaces.DetectedFaces.Any(f => f.FaceAttributes.Age > 12))
            {
                // Group with at least one child
                recommendation = this.kioskSettings.GenericRecommendations.FirstOrDefault(r => r.Id == "ChildWithOneOrMoreAdults");
            }
            else if (numberOfPeople > 1 && !imageWithFaces.DetectedFaces.Any(f => f.FaceAttributes.Age <= 12))
            {
                // Group of adults without a child
                recommendation = this.kioskSettings.GenericRecommendations.FirstOrDefault(r => r.Id == "TwoOrMoreAdults");
            }

            if (recommendation != null)
            {
                webView.Navigate(new Uri(recommendation.Url));
                webView.Visibility = Visibility.Visible;
                this.currentRecommendation = recommendation;
            }
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateWebCamHostGridSize();
        }

        #region Real-time Emotion Feed

        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;
        private bool isEmotionResponseFlyoutOpened;

        private async void OnEmotionTrackingFlyoutOpened(object sender, object e)
        {
            await this.cameraControl.StartStreamAsync();
            this.isEmotionResponseFlyoutOpened = true;
        }

        private async void OnEmotionTrackingFlyoutClosed(object sender, object e)
        {
            this.isEmotionResponseFlyoutOpened = false;
            await this.ResetRecommendationUI();
        }

        private void StartEmotionProcessingLoop()
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
                    if (!this.isProcessingPhoto && this.isEmotionResponseFlyoutOpened)
                    {
                        this.isProcessingPhoto = true;
                        if (this.cameraControl.NumFacesOnLastFrame == 0)
                        {
                            await ProcessCameraCapture(null);
                        }
                        else
                        {
                            await this.ProcessCameraCapture(await this.cameraControl.CaptureFrameAsync());
                        }
                    }
                });

                await Task.Delay(1000);
            }
        }

        private async Task ProcessCameraCapture(ImageAnalyzer e)
        {
            if (e == null)
            {
                this.isProcessingPhoto = false;
                return;
            }

            // detect emotions
            await e.DetectFacesAsync(detectFaceAttributes: true);

            if (e.DetectedFaces.Any())
            {
                // Update the average emotion response
                EmotionScores averageScores = new EmotionScores
                {
                    Happiness = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Happiness),
                    Anger = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Anger),
                    Sadness = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Sadness),
                    Contempt = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Contempt),
                    Disgust = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Disgust),
                    Neutral = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Neutral),
                    Fear = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Fear),
                    Surprise = e.DetectedFaces.Average(f => f.FaceAttributes.Emotion.Surprise)
                };

                double positiveEmotionResponse = Math.Min(averageScores.Happiness + averageScores.Surprise, 1);
                double negativeEmotionResponse = Math.Min(averageScores.Sadness + averageScores.Fear + averageScores.Disgust + averageScores.Contempt, 1);
                double netResponse = ((positiveEmotionResponse - negativeEmotionResponse) * 0.5) + 0.5;

                this.sentimentControl.Sentiment = netResponse;

                // show captured faces and their emotion
                if (this.emotionFacesGrid.Visibility == Visibility.Visible)
                {
                    foreach (var face in e.DetectedFaces)
                    {
                        // Get top emotion on this face
                        var topEmotion = face.FaceAttributes.Emotion.ToRankedList().First();

                        // Crop this face
                        FaceRectangle rect = face.FaceRectangle;
                        double heightScaleFactor = 1.8;
                        double widthScaleFactor = 1.8;
                        FaceRectangle biggerRectangle = new FaceRectangle
                        {
                            Height = Math.Min((int)(rect.Height * heightScaleFactor), e.DecodedImageHeight),
                            Width = Math.Min((int)(rect.Width * widthScaleFactor), e.DecodedImageWidth)
                        };
                        biggerRectangle.Left = Math.Max(0, rect.Left - (int)(rect.Width * ((widthScaleFactor - 1) / 2)));
                        biggerRectangle.Top = Math.Max(0, rect.Top - (int)(rect.Height * ((heightScaleFactor - 1) / 1.4)));

                        ImageSource croppedImage = await Util.GetCroppedBitmapAsync(e.GetImageStreamCallback, biggerRectangle);

                        // Add the face and emotion to the collection of faces
                        if (croppedImage != null && biggerRectangle.Height > 0 && biggerRectangle.Width > 0)
                        {
                            if (this.EmotionFaces.Count >= 9)
                            {
                                this.EmotionFaces.Clear();
                            }

                            this.EmotionFaces.Add(new EmotionExpressionCapture { CroppedFace = croppedImage, TopEmotion = topEmotion.Key });
                        }
                    }
                }
            }

            this.isProcessingPhoto = false;
        }

        #endregion

        private void OnEmotionFacesToggleUnchecked(object sender, RoutedEventArgs e)
        {
            emotionFacesGrid.Visibility = Visibility.Collapsed;
        }

        private void OnEmotionFacesToggleChecked(object sender, RoutedEventArgs e)
        {
            emotionFacesGrid.Visibility = Visibility.Visible;
        }
    }

    public class EmotionExpressionCapture
    {
        public ImageSource CroppedFace { get; set; }
        public string TopEmotion { get; set; }
    }
}
