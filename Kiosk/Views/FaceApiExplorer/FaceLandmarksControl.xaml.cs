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

using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Face = Microsoft.Azure.CognitiveServices.Vision.Face.Models;


namespace IntelligentKioskSample.Views.FaceApiExplorer
{
    public sealed partial class FaceLandmarksControl : UserControl
    {
        public FaceLandmarksControl()
        {
            this.InitializeComponent();
        }

        public void DisplayFaceLandmarks(Face.FaceRectangle faceRect, Face.FaceLandmarks landmarks, double scaleX, double scaleY)
        {
            // Mouth (6)
            AddFacialLandmark(faceRect, landmarks.MouthLeft, scaleX, scaleY, this.mouthleft);
            AddFacialLandmark(faceRect, landmarks.MouthRight, scaleX, scaleY, this.mouthright);
            AddFacialLandmark(faceRect, landmarks.UpperLipBottom, scaleX, scaleY, this.upperlipbottom);
            AddFacialLandmark(faceRect, landmarks.UpperLipTop, scaleX, scaleY, this.upperliptop);
            AddFacialLandmark(faceRect, landmarks.UnderLipBottom, scaleX, scaleY, this.underlipbottom);
            AddFacialLandmark(faceRect, landmarks.UnderLipTop, scaleX, scaleY, this.underliptop);

            // Eyes (10)
            AddFacialLandmark(faceRect, landmarks.EyeLeftBottom, scaleX, scaleY, eyeleftbottom);
            AddFacialLandmark(faceRect, landmarks.EyeLeftTop, scaleX, scaleY, eyelefttop);
            AddFacialLandmark(faceRect, landmarks.EyeLeftInner, scaleX, scaleY, eyeleftinner);
            AddFacialLandmark(faceRect, landmarks.EyeLeftOuter, scaleX, scaleY, eyeleftouter);
            AddFacialLandmark(faceRect, landmarks.EyeRightBottom, scaleX, scaleY, eyerightbottom);
            AddFacialLandmark(faceRect, landmarks.EyeRightTop, scaleX, scaleY, eyerighttop);
            AddFacialLandmark(faceRect, landmarks.EyeRightInner, scaleX, scaleY, eyerightinner);
            AddFacialLandmark(faceRect, landmarks.EyeRightOuter, scaleX, scaleY, eyerightouter);
            AddFacialLandmark(faceRect, landmarks.PupilLeft, scaleX, scaleY, pupilleft);
            AddFacialLandmark(faceRect, landmarks.PupilRight, scaleX, scaleY, pupilright);

            // nose (7)
            AddFacialLandmark(faceRect, landmarks.NoseLeftAlarOutTip, scaleX, scaleY, noseleftalarouttip);
            AddFacialLandmark(faceRect, landmarks.NoseLeftAlarTop, scaleX, scaleY, noseleftalartop);
            AddFacialLandmark(faceRect, landmarks.NoseRightAlarOutTip, scaleX, scaleY, noserightalarouttip);
            AddFacialLandmark(faceRect, landmarks.NoseRightAlarTop, scaleX, scaleY, noserightalartop);
            AddFacialLandmark(faceRect, landmarks.NoseRootLeft, scaleX, scaleY, noserootleft);
            AddFacialLandmark(faceRect, landmarks.NoseRootRight, scaleX, scaleY, noserootright);
            AddFacialLandmark(faceRect, landmarks.NoseTip, scaleX, scaleY, nosetip);


            // eyebrows (4)
            AddFacialLandmark(faceRect, landmarks.EyebrowLeftInner, scaleX, scaleY, eyebrowleftinner);
            AddFacialLandmark(faceRect, landmarks.EyebrowLeftOuter, scaleX, scaleY, eyebrowleftouter);
            AddFacialLandmark(faceRect, landmarks.EyebrowRightInner, scaleX, scaleY, eyebrowrightinner);
            AddFacialLandmark(faceRect, landmarks.EyebrowRightOuter, scaleX, scaleY, eyebrowrightouter);

            this.mainGrid.Visibility = Visibility.Visible;
            this.FaceLandmarksStoryboard.Begin();
        }

        public void HideFaceLandmarks()
        {
            this.mainGrid.Visibility = Visibility.Collapsed;
            this.FaceLandmarksStoryboard.Stop();
        }

        private void AddFacialLandmark(Face.FaceRectangle faceRect, Face.Coordinate coordinate, double scaleX, double scaleY, Rectangle rect)
        {
            double dotSize = 3;
            rect.Fill = new SolidColorBrush(Colors.White);
            rect.Width = dotSize;
            rect.Height = dotSize;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
            rect.Margin = new Thickness(
                ((coordinate.X - faceRect.Left) * scaleX) - dotSize / 2,
                ((coordinate.Y - faceRect.Top) * scaleY) - dotSize / 2,
                0, 0);
        }
    }
}
