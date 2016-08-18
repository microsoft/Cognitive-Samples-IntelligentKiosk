using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Controls
{
    /// <summary>
    /// Interaction logic for SentimentControl.xaml
    /// </summary>
    public partial class SentimentControl : UserControl
    {
        public static readonly DependencyProperty SentimentProperty =
            DependencyProperty.Register(
            "Sentiment",
            typeof(double),
            typeof(SentimentControl),
            new PropertyMetadata(0.5, SentimentPropertyChangedCallback)
            );

        public double Sentiment
        {
            get { return (double)GetValue(SentimentProperty); }
            set { SetValue(SentimentProperty, (double)value); }
        }

        static void SentimentPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SentimentControl control = (SentimentControl)d;
            control.UpdateSentimentPointer();
        }

        public SentimentControl()
        {
            InitializeComponent();
        }

        private void OnSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            this.UpdateSentimentPointer();
        }

        private void UpdateSentimentPointer()
        {
            double totalLength = this.GuideLine.ActualWidth;
            this.Pointer.SetValue(Canvas.LeftProperty, totalLength * this.Sentiment);
            this.PointerText.Text = String.Format("{0:N2}", this.Sentiment);
        }
    }
}
