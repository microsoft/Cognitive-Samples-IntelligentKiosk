using Microsoft.ProjectOxford.Common.Contract;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public sealed partial class VideoTrack : UserControl
    {
        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(
            "DisplayText",
            typeof(string),
            typeof(VideoTrack),
            new PropertyMetadata("")
            );

        public static readonly DependencyProperty CroppedFaceProperty =
            DependencyProperty.Register(
            "CroppedFace",
            typeof(ImageSource),
            typeof(VideoTrack),
            new PropertyMetadata(null)
            );

        public string DisplayText
        {
            get { return (string)GetValue(DisplayTextProperty); }
            set { SetValue(DisplayTextProperty, (string)value); }
        }

        public ImageSource CroppedFace
        {
            get { return (ImageSource)GetValue(CroppedFaceProperty); }
            set { SetValue(CroppedFaceProperty, (ImageSource)value); }
        }

        public VideoTrack()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private int duration;
        public int Duration
        {
            set
            {
                this.duration = value;

                this.chart.Children.Clear();
            }
        } 

        public void SetVideoFrameState(int videoFrameTimestampInSeconds, EmotionScores emotion)
        {
            EmotionToColoredBar emotionResponse = new EmotionToColoredBar();
            emotionResponse.UpdateEmotion(emotion);
            emotionResponse.Tag = videoFrameTimestampInSeconds;
            emotionResponse.Width = Math.Max(this.chart.ActualWidth / this.duration, 0.5);
            emotionResponse.HorizontalAlignment = HorizontalAlignment.Left;

            emotionResponse.Margin = new Thickness
            {
                Left = ((double) videoFrameTimestampInSeconds / this.duration) * this.chart.ActualWidth
            };

            this.chart.Children.Add(emotionResponse);
        }

        private void ChartSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = Math.Max(this.chart.ActualWidth / this.duration, 0.5);

            foreach (var item in this.chart.Children)
            {
                var element = (FrameworkElement)item;
                element.Width = width;
                element.Margin = new Thickness
                {
                    Left = ((double) ((int)element.Tag) / this.duration) * this.chart.ActualWidth
                };
            }
        }
    }
}
