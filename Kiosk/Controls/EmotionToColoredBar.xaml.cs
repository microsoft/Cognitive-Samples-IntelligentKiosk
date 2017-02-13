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

using Microsoft.ProjectOxford.Common.Contract;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls
{
    public partial class EmotionToColoredBar: UserControl
    {
        private Dictionary<String, SolidColorBrush> emotionToColorMapping = new Dictionary<string, SolidColorBrush>();

        public static SolidColorBrush AngerColor = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x54, 0x2c));
        public static SolidColorBrush ContemptColor = new SolidColorBrush(Color.FromArgb(0xff, 0xce, 0x2d, 0x90));
        public static SolidColorBrush DisgustColor = new SolidColorBrush(Color.FromArgb(0xff, 0x8c, 0x43, 0xbd));
        public static SolidColorBrush FearColor = new SolidColorBrush(Color.FromArgb(0xff, 0xfe, 0xb5, 0x52));
        public static SolidColorBrush HappinessColor = new SolidColorBrush(Color.FromArgb(0xFF, 0x4f, 0xc7, 0x45));
        public static SolidColorBrush NeutralColor = new SolidColorBrush(Color.FromArgb(0xff, 0xaf, 0xaf, 0xaf));
        public static SolidColorBrush SadnessColor = new SolidColorBrush(Color.FromArgb(0xff, 0x47, 0x8b, 0xcb));
        public static SolidColorBrush SurpriseColor = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xf6, 0xd6));

        public EmotionToColoredBar()
        {
            emotionToColorMapping.Add("Anger", AngerColor);
            emotionToColorMapping.Add("Contempt", ContemptColor);
            emotionToColorMapping.Add("Disgust", DisgustColor);
            emotionToColorMapping.Add("Fear", FearColor);
            emotionToColorMapping.Add("Happiness", HappinessColor);
            emotionToColorMapping.Add("Neutral", NeutralColor);
            emotionToColorMapping.Add("Sadness", SadnessColor);
            emotionToColorMapping.Add("Surprise", SurpriseColor);

            InitializeComponent();
        }

        public void UpdateEmotion(EmotionScores scores)
        {
            EmotionData topEmotion = EmotionServiceHelper.ScoresToEmotionData(scores).OrderByDescending(d => d.EmotionScore).First();

            this.filledBar.Background = this.emotionToColorMapping[topEmotion.EmotionName];
            this.emptySpaceRowDefinition.Height = new GridLength(1 - topEmotion.EmotionScore, Windows.UI.Xaml.GridUnitType.Star);
            this.filledSpaceRowDefinition.Height = new GridLength(topEmotion.EmotionScore, Windows.UI.Xaml.GridUnitType.Star);
        }
    }
}