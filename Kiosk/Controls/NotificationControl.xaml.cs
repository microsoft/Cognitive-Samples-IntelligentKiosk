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

using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace IntelligentKioskSample.Controls
{
    public sealed partial class NotificationControl : UserControl
    {
        public NotificationControl()
        {
            this.InitializeComponent();
        }

        public void ShowNotification(NotificationParams notificationParams)
        {
            this.notificationTextBlock.Text = notificationParams?.Message ?? string.Empty;
            this.notificationPanel.Background = notificationParams?.BackgroundColor != null
                ? new SolidColorBrush(notificationParams.BackgroundColor) : new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));

            if (notificationParams.Link?.Uri != null)
            {
                Hyperlink hyperlink = new Hyperlink
                {
                    NavigateUri = notificationParams.Link.Uri
                };
                Run run = new Run
                {
                    Text = !string.IsNullOrEmpty(notificationParams.Link.Text) ? notificationParams.Link.Text : notificationParams.Link.Uri.AbsoluteUri
                };
                hyperlink.Inlines.Add(run);
                this.notificationTextBlock.Inlines.Add(hyperlink);
            }
            this.notificationPanel.Visibility = Visibility.Visible;
        }

        public void HideNotification()
        {
            this.notificationPanel.Visibility = Visibility.Collapsed;
        }

        private void OnNotificationPanelCloseButtonClicked(object sender, RoutedEventArgs e)
        {
            HideNotification();
        }
    }

    public class NotificationParams
    {
        public string Message { get; set; }
        public CustomHyperlink Link { get; set; }
        public Color BackgroundColor { get; set; } = Color.FromArgb(255, 0, 120, 215);
    }

    public class CustomHyperlink
    {
        public Uri Uri { get; set; }
        public string Text { get; set; }

        public CustomHyperlink(Uri uri, string text = "")
        {
            this.Uri = uri;
            this.Text = text;
        }
    }
}
