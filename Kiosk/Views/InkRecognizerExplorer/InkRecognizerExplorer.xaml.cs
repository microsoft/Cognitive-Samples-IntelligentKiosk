// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: 
// http://www.microsoft.com/cognitive
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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views.InkRecognizerExplorer
{
    [KioskExperience(Title = "Ink Recognizer Explorer", ImagePath = "ms-appx:/Assets/InkRecognizerExplorer.png")]
    public sealed partial class InkRecognizerExplorer : Page
    {
        public InkRecognizerExplorer()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(SettingsHelper.Instance.InkRecognizerApiKey))
            {
                navView.IsEnabled = false;
                await new MessageDialog("Missing Ink Recognizer API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                frame.SourcePageType = typeof(InkMirror);
                navView.SelectedItem = navView.MenuItems[0];
            }

            base.OnNavigatedTo(e);
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var item = sender.SelectedItem as NavigationViewItem;

            switch (item.Name)
            {
                case "inkMirror":
                    frame.Navigate(typeof(InkMirror));
                    break;
                case "formFiller":
                    frame.Navigate(typeof(FormFiller));
                    break;
            }
        }
    }
}
