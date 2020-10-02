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

using IntelligentKioskSample.Views;
using IntelligentKioskSample.Views.DemoLauncher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample
{
    /// <summary>
    /// The "chrome" layer of the app that provides top-level navigation with
    /// proper keyboarding navigation.
    /// </summary>
    public sealed partial class AppShell : Page
    {
        bool prevFullScreen;

        // Declare the top level nav items
        private NavMenuItem prevNavMenuItem;
        private List<NavMenuItem> navlist = new List<NavMenuItem>(
            new NavMenuItem[]
            {
                new NavMenuItem()
                {
                    Id = "DemoGallery",
                    Glyph = "\uECA5",
                    Label = "Demo Gallery",
                    DestPage = typeof(DemoLauncherPage)
                },

                new NavMenuItem()
                {
                    Id = "Favorites",
                    Glyph = "\uE735",
                    Label = "Favorite demos",
                    DestPage = typeof(DemoLauncherPage),
                    Arguments = "Favorites"
                },

                new NavMenuItem()
                {
                    Id = "FaceIdentificationSetup",
                    Glyph = "\uE8D4",
                    Label = "Face Identification Setup",
                    DestPage = typeof(FaceIdentificationSetup)
                },

                new NavMenuItem()
                {
                    Id = "CustomVisionSetup",
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

            SetUpCustomTitleBar();
        }

        #region Custom Title Bar

        void SetUpCustomTitleBar()
        {
            // Set the custom TitleBar colors
            var appView = ApplicationView.GetForCurrentView();
            var titleBar = appView.TitleBar;
            titleBar.BackgroundColor =
                titleBar.InactiveBackgroundColor =
                titleBar.ButtonBackgroundColor =
                titleBar.ButtonInactiveBackgroundColor =
                ((SolidColorBrush)Application.Current.Resources["TitleBarButtonBackgroundBrush"]).Color;
            titleBar.ForegroundColor =
                titleBar.InactiveForegroundColor =
                titleBar.ButtonForegroundColor =
                titleBar.ButtonInactiveForegroundColor =
                titleBar.ButtonHoverForegroundColor =
                titleBar.ButtonPressedForegroundColor =
                ((SolidColorBrush)Application.Current.Resources["TitleBarButtonForegroundBrush"]).Color;
            titleBar.ButtonHoverBackgroundColor = ((SolidColorBrush)Application.Current.Resources["TitleBarButtonHoverBrush"]).Color;
            titleBar.ButtonPressedBackgroundColor = ((SolidColorBrush)Application.Current.Resources["TitleBarButtonPressedBrush"]).Color;

            //setup event handlers (see example: https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/TitleBar)
            var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.IsVisibleChanged += UpdateTitleBar_Visibility;
            coreTitleBar.LayoutMetricsChanged += UpdateTitleBar_Layout;
            Window.Current.SizeChanged += Current_SizeChanged;
            coreTitleBar.ExtendViewIntoTitleBar = true; //hides the built in title bar
            Window.Current.SetTitleBar(TitleBarHandle); //allow moveing the window by this control

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            //connect to setting changes to update startup mode
            SettingsHelper.Instance.PropertyChanged += Settings_PropertyChanged;

            //setup the title bar correctly if already in started in full screen mode
            if (ApplicationView.GetForCurrentView().IsFullScreenMode)
            {
                UpdateTitleBar_Visibility(Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar);
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsHelper.StartupFullScreenMode))
            {
                //set the startup mode for next time when the app launches
                ApplicationView.PreferredLaunchWindowingMode = SettingsHelper.Instance.StartupFullScreenMode ? ApplicationViewWindowingMode.FullScreen : ApplicationViewWindowingMode.Auto;
            }
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            var fullScreen = ApplicationView.GetForCurrentView().IsFullScreenMode;
            if (prevFullScreen == true && fullScreen == false && TitleBar.Visibility == Visibility.Visible)
            {
                UpdateTitleBar_Visibility(Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar);
            }

            this.prevFullScreen = fullScreen;
        }

        private void UpdateTitleBar_Layout(Windows.ApplicationModel.Core.CoreApplicationViewTitleBar sender, object args = null)
        {
            //set padding
            TitleBar.Padding = new Thickness(sender.SystemOverlayLeftInset, 0, sender.SystemOverlayRightInset, 0);
        }

        private void UpdateTitleBar_Visibility(Windows.ApplicationModel.Core.CoreApplicationViewTitleBar sender, object args = null)
        {
            //set visibility
            var visible = sender.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            if (TitleBar.Visibility != visible)
            {
                TitleBar.Visibility = visible;
            }

            //set rowspan if in fullscreen
            var rowSpan = Grid.GetRowSpan(TitleBar);
            var fullScreen = ApplicationView.GetForCurrentView().IsFullScreenMode;
            var newRowSpan = fullScreen ? 2 : 1;
            if (rowSpan != newRowSpan)
            {
                Grid.SetRowSpan(TitleBar, newRowSpan);
            }

            //toggle fullscreen button
            var fullScreenButtonVisible = fullScreen ? Visibility.Collapsed : Visibility.Visible;
            if (FullScreenButton.Visibility != fullScreenButtonVisible)
            {
                FullScreenButton.Visibility = fullScreenButtonVisible;
            }
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            //enter full screen mode
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        #endregion

        public void NavigateToStartingPage()
        {
            KioskExperience startingExp = KioskExperiences.Experiences.FirstOrDefault(item => item.Attributes.Id == SettingsHelper.Instance.StartingPage);
            if (startingExp == null)
            {
                NavigateToPage(typeof(DemoLauncherPage));
                this.navView.SelectedItem = this.navlist.First();
            }
            else
            {
                NavigateToExperience(startingExp);
            }
        }

        public void NavigateToPage(Type destPage, object parameter = null)
        {
            AppFrame.Navigate(destPage, parameter, new Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
        }

        public void NavigateToExperience(KioskExperience experience)
        {
            if (this.AppFrame.CurrentSourcePageType != experience.PageType)
            {
                NavigateToPage(experience.PageType);
            }
        }

        public Frame AppFrame { get { return this.frame; } }

        public Grid AppOverlay { get { return this.appOverlay; } }

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

                var kioskDemoAtribute = control.GetType().GetTypeInfo().GetCustomAttribute<KioskExperienceAttribute>();
                if (kioskDemoAtribute != null)
                {
                    navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                    navView.SelectedItem = null;
                }
                else
                {
                    navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;

                    var menuItem = navlist.FirstOrDefault(item => item.DestPage == control.GetType());
                    if (control is DemoLauncherPage && (string)e.Parameter == "Favorites")
                    {
                        menuItem = navlist.FirstOrDefault(item => item.Id == "Favorites");
                    }

                    if (menuItem != null && menuItem != navView.SelectedItem)
                    {
                        navView.SelectedItem = menuItem;
                    }
                }

                navView.IsPaneOpen = false;

                // wait for load event to inject the help element into the UI
                control.Loaded += Page_Loaded;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Page page = ((Page)sender);
            page.Loaded -= Page_Loaded;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateToPage(typeof(SettingsPage));
                this.prevNavMenuItem = null;
            }
            else
            {
                var item = (NavMenuItem)args.SelectedItemContainer?.DataContext;
                if (item?.DestPage != null && item.Id != this.prevNavMenuItem?.Id)
                {
                    NavigateToPage(item.DestPage, item.Arguments);
                }
                this.prevNavMenuItem = item;
            }
        }

        private void OnNavViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // See if the user is clicking on the Demo Gallery button when the current view is already the Demo Gallery. In that case,
            // we will use this as an opportunity to force the gallery to switch back to the main gallery content (if needed)
            NavMenuItem item = sender.SelectedItem as NavMenuItem;
            if (item?.Id == "DemoGallery" && this.prevNavMenuItem?.Id == item.Id)
            {
                DemoLauncherPage launcher = AppFrame.Content as DemoLauncherPage;
                if (launcher != null && !launcher.IsMainPage)
                {
                    launcher.SwitchToMainGalleryView();
                }
            }
        }

        #endregion
    }
}
