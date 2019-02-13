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

using IntelligentKioskSample.Controls;
using IntelligentKioskSample.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample
{
    /// <summary>
    /// The "chrome" layer of the app that provides top-level navigation with
    /// proper keyboarding navigation.
    /// </summary>
    public sealed partial class AppShell : Page
    {
        // Declare the top level nav items
        private List<NavMenuItem> navlist = new List<NavMenuItem>(
            new NavMenuItem[]
            {
                new NavMenuItem()
                {
                    Glyph = "\uECA5",
                    Label = "Demo Gallery",
                    DestPage = typeof(DemoLauncherPage)
                },

                new NavMenuItem()
                {
                    Glyph = "\uE8D4",
                    Label = "Face Identification Setup",
                    DestPage = typeof(FaceIdentificationSetup)
                },

                new NavMenuItem()
                {
                    Glyph = "\uE052",
                    Label = "Custom Vision Setup",
                    DestPage = typeof(CustomVisionSetup)
                }
            });

        public static AppShell Current = null;

        /// <summary>
        /// Initializes a new instance of the AppShell, sets the static 'Current' reference,
        /// adds callbacks for Back requests and changes in the SplitView's DisplayMode, and
        /// provide the nav menu list with the data to display.
        /// </summary>
        public AppShell()
        {
            this.InitializeComponent();

            this.Loaded += (sender, args) =>
            {
                Current = this;

                this.NavigateToStartingPage();
            };

            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;

            navView.MenuItemsSource = navlist;
        }

        public void NavigateToStartingPage()
        {
            NavMenuItem navMenuItem = null;
            Type destPage = null;

            navMenuItem = navlist.First();
            destPage = navMenuItem.DestPage;

            if (navMenuItem != null)
            {
                navView.SelectedItem = navMenuItem;
            }

            NavigateToPage(destPage);
        }

        public void NavigateToPage(Type destPage, object parameter = null)
        {
            if (this.AppFrame.CurrentSourcePageType != destPage)
            {
                AppFrame.Navigate(destPage, parameter, new Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
            }
        }

        public Frame AppFrame { get { return this.frame; } }

        #region BackRequested Handlers

        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            bool handled = e.Handled;
            this.BackRequested(ref handled);
            e.Handled = handled;
        }

        private void BackRequested(ref bool handled)
        {
            // Get a hold of the current frame so that we can inspect the app back stack.

            if (this.AppFrame == null)
                return;

            // Check to see if this is the top-most page on the app back stack.
            if (this.AppFrame.CanGoBack && !handled)
            {
                // If not, set the event to handled and go back to the previous page in the app.
                handled = true;
                this.AppFrame.GoBack();
            }
        }

        #endregion

        #region Navigation

        private void OnNavigatedToPage(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack && !(e.Content is DemoLauncherPage) ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;

            // After a successful navigation, update the state of the pane
            if (e.Content is Page && e.Content != null)
            {
                var control = (Page)e.Content;

                if (!(control is DemoLauncherPage || control is FaceIdentificationSetup || control is SettingsPage || control is CustomVisionSetup))
                {
                    navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                    navView.SelectedItem = null;
                }
                else
                {
                    navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;

                    var menuItem = navlist.FirstOrDefault(item => item.DestPage == control.GetType());
                    if (menuItem != null && menuItem != navView.SelectedItem)
                    {
                        navView.SelectedItem = menuItem;
                    }
                }

                navView.IsPaneOpen = false;
            }
        }

        #endregion

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateToPage(typeof(SettingsPage));
            }
            else
            {
                var item = (NavMenuItem)args.SelectedItemContainer?.DataContext;
                if (item?.DestPage != null &&
                    item.DestPage != this.AppFrame.CurrentSourcePageType)
                {
                    NavigateToPage(item.DestPage, item.Arguments);
                }
            }
        }

    }
}
