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
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace IntelligentKioskSample.Views.DemoLauncher
{
    public sealed partial class DemoCollectionControl : UserControl
    {
        public static readonly DependencyProperty EnableHorizontalViewProperty =
            DependencyProperty.Register(
            "EnableHorizontalView",
            typeof(bool),
            typeof(DemoCollectionControl),
            new PropertyMetadata(false));

        public bool EnableHorizontalView
        {
            get { return (bool)GetValue(EnableHorizontalViewProperty); }
            set { SetValue(EnableHorizontalViewProperty, value); }
        }

        public static readonly DependencyProperty DemoEntriesProperty =
            DependencyProperty.Register(
            "DemoEntries",
            typeof(ObservableCollection<DemoEntry>),
            typeof(DemoCollectionControl),
            new PropertyMetadata(null));

        public ObservableCollection<DemoEntry> DemoEntries
        {
            get { return (ObservableCollection<DemoEntry>)GetValue(DemoEntriesProperty); }
            set { SetValue(DemoEntriesProperty, value); }
        }

        public static readonly DependencyProperty DemoCollectionGroupProperty =
            DependencyProperty.Register(
            "DemoCollectionGroup",
            typeof(DemoCollectionType),
            typeof(DemoCollectionControl),
            new PropertyMetadata(DemoCollectionType.All));

        public DemoCollectionType DemoCollectionGroup
        {
            get { return (DemoCollectionType)GetValue(DemoCollectionGroupProperty); }
            set { SetValue(DemoCollectionGroupProperty, value); }
        }

        public event EventHandler FavoriteDemoToggled;
        public event EventHandler<DemoEntry> DemoOpened;

        public DemoCollectionControl()
        {
            this.InitializeComponent();
        }

        private void OnDemoClick(object sender, ItemClickEventArgs e)
        {
            var demoEntry = (DemoEntry)e.ClickedItem;
            AppShell.Current.NavigateToExperience(demoEntry.KioskExperience);
            this.DemoOpened?.Invoke(this, demoEntry);
        }

        private void OnFavoriteDemoToggleClicked(object sender, RoutedEventArgs e)
        {
            this.FavoriteDemoToggled?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddFavoritesDemosClicked(object sender, RoutedEventArgs e)
        {
            AppShell.Current.NavigateToPage(typeof(DemoLauncherPage));
        }
    }

    public class DemoDateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            KioskExperienceAttribute kioskExperienceAttribute = (KioskExperienceAttribute)value;
            int.TryParse(parameter != null ? (string)parameter : "90", out int days);

            DateTime.TryParse(kioskExperienceAttribute?.DateAdded, out DateTime demoDateAdded);
            if ((DateTime.Now - demoDateAdded).TotalDays <= days)
            {
                return "New";
            }
            DateTime.TryParse(kioskExperienceAttribute?.DateUpdated, out DateTime demoDateUpdated);
            if ((DateTime.Now - demoDateUpdated).TotalDays <= days)
            {
                return "Updated";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // not used
            return string.Empty;
        }
    }

    public class DemoExperienceTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ExperienceType experienceType = (ExperienceType)value;
            if ((experienceType & ExperienceType.Preview) != 0)
            {
                return "Preview";
            }
            else if ((experienceType & ExperienceType.Experimental) != 0)
            {
                return "Experimental";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // not used
            return string.Empty;
        }
    }
}
