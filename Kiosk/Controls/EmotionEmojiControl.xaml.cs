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

using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Controls
{
    /// <summary>
    /// Interaction logic for EmotionEmojiControl.xaml
    /// </summary>
    public partial class EmotionEmojiControl : UserControl
    {
        public EmotionEmojiControl()
        {
            InitializeComponent();
        }

        public void UpdateEmotion(Emotion emotion)
        {
            var topEmotion = Util.EmotionToRankedList(emotion).First();
            string label = "", emoji = "";

            switch (topEmotion.Key)
            {
                case "Anger":
                    label = "Angry";
                    emoji = "\U0001f620";
                    break;
                case "Contempt":
                    label = "Contemptuous";
                    emoji = "\U0001f612";
                    break;
                case "Disgust":
                    label = "Disgusted";
                    emoji = "\U0001f627";
                    break;
                case "Fear":
                    label = "Afraid";
                    emoji = "\U0001f628";
                    break;
                case "Happiness":
                    label = "Happy";
                    emoji = "\U0001f60a";
                    break;
                case "Neutral":
                    label = "Neutral";
                    emoji = "\U0001f614";
                    break;
                case "Sadness":
                    label = "Sad";
                    emoji = "\U0001f622";
                    break;
                case "Surprise":
                    label = "Surprised";
                    emoji = "\U0001f632";
                    break;
                default:
                    break;
            }

            this.emotionEmoji.Text = emoji;
            this.emotionText.Text = label;
        }
    }
}