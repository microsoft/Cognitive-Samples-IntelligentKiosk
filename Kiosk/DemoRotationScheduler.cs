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

using IntelligentKioskSample.Controls.Animation;
using IntelligentKioskSample.Views;
using IntelligentKioskSample.Views.DemoLauncher;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace IntelligentKioskSample
{
    class DemoRotationScheduler
    {
        private static DemoRotationScheduler instance = new DemoRotationScheduler();
        private KioskExperience[] kioskRotation;
        private int nextKioskExperienceIndex;
        private Timer timer;

        public static DemoRotationScheduler Instance
        {
            get { return instance; }
        }

        public async void Start()
        {
            // Stop current scheduler
            Stop();

            // Load the demo rotation
            DemoLauncherConfig config = await SettingsPage.LoadDemoRotationConfigFromFileAsync();
            kioskRotation = config.Entries.Where(entry => entry.Enabled).Select(entry => KioskExperiences.Experiences.First(exp => exp.Attributes.Id == entry.Id)).ToArray();

            if (kioskRotation.Length > 0)
            {
                // Shuffled the demo order
                Random random = new Random();
                for (int i = 0; i < kioskRotation.Length; i++)
                {
                    int r = random.Next(0, kioskRotation.Length);
                    KioskExperience temp = kioskRotation[r];
                    kioskRotation[r] = kioskRotation[i];
                    kioskRotation[i] = temp;
                }

                // Start the scheduler
                nextKioskExperienceIndex = 0;
                timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromMinutes(SettingsHelper.Instance.DemoRotationTimePerDemo));
            }
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        private async void TimerCallback(object state)
        {
            KioskExperience nextKioskExperience = kioskRotation[nextKioskExperienceIndex];

            nextKioskExperienceIndex = (nextKioskExperienceIndex + 1) % kioskRotation.Length;

            await AppShell.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                // Fade in the transition overlay
                AppShell.Current.AppOverlay.Opacity = 0;
                AppShell.Current.AppOverlay.Visibility = Visibility.Visible;
                await new OpacityAnimation(1, TimeSpan.FromMilliseconds(1000)).Activate(AppShell.Current.AppOverlay);

                // Move out of the page into the Demo Launcher page. We do this so we shutdown the camera in the current experience before we go to the next one.
                AppShell.Current.NavigateToPage(typeof(DemoLauncherPage));

                // Wait some time
                await Task.Delay(2000);

                // Move to the new experience
                AppShell.Current.NavigateToExperience(nextKioskExperience);

                // Fade out the transition overlay
                await new OpacityAnimation(0, TimeSpan.FromMilliseconds(1000)).Activate(AppShell.Current.AppOverlay);
                AppShell.Current.AppOverlay.Visibility = Visibility.Collapsed;
            });
        }
    }
}
