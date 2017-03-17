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

using RPi.SenseHat.Demo.Demos;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class DemoLauncherPage : Page
    {
        private int senseHatFocusIndex;
        private SingleColorScrollText senseHatScrollText;

        public DemoLauncherPage()
        {
            this.InitializeComponent();

            this.DataContext = KioskExperiences.Experiences;
        }

        private void OnDemoClick(object sender, ItemClickEventArgs e)
        {
            this.Frame.Navigate(((KioskExperience)e.ClickedItem).PageType);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.ConfigureSenseHat();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SenseHatInputManager.Instance.EnterPressed -= SenseHatEnterPressed;
            SenseHatInputManager.Instance.LeftPressed -= SenseHatLeftPressed;
            SenseHatInputManager.Instance.RightPressed -= SenseHatRightPressed;
            this.senseHatScrollText.StopScroll();

            base.OnNavigatedFrom(e);
        }

        private void ConfigureSenseHat()
        {
            SenseHatInputManager.Instance.EnterPressed += SenseHatEnterPressed;
            SenseHatInputManager.Instance.LeftPressed += SenseHatLeftPressed;
            SenseHatInputManager.Instance.RightPressed += SenseHatRightPressed;

            this.senseHatScrollText = new SingleColorScrollText("Launcher");
            this.senseHatScrollText.StartScroll();
        }

        private async void SenseHatEnterPressed(object sender, EventArgs e)
        {
            this.senseHatScrollText.StopScroll();

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.Frame.Navigate(KioskExperiences.Experiences.ElementAt(senseHatFocusIndex).PageType);
            });
        }

        private void SenseHatRightPressed(object sender, EventArgs e)
        {
            this.senseHatFocusIndex = (this.senseHatFocusIndex + 1) % KioskExperiences.Experiences.Count();
            this.UpdateSenseHatScrollText();
        }

        private void SenseHatLeftPressed(object sender, EventArgs e)
        {
            this.senseHatFocusIndex = this.senseHatFocusIndex - 1;
            if (this.senseHatFocusIndex < 0)
            {
                this.senseHatFocusIndex = KioskExperiences.Experiences.Count() - 1;
            }

            this.UpdateSenseHatScrollText();
        }

        private void UpdateSenseHatScrollText()
        {
            this.senseHatScrollText.StopScroll();
            this.senseHatScrollText = new SingleColorScrollText(KioskExperiences.Experiences.ElementAt(senseHatFocusIndex).Attributes.Title);
            this.senseHatScrollText.StartScroll();
        }

    }
}
