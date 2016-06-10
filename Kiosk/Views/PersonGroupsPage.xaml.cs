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

using ServiceHelpers;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Views
{
    public sealed partial class PersonGroupsPage : Page
    {
        public PersonGroupsPage()
        {
            this.InitializeComponent();
        }

        private async Task LoadPersonGroupsFromService()
        {
            this.progressControl.IsActive = true;

            try
            {
                IEnumerable<PersonGroup> personGroups = await FaceServiceHelper.GetPersonGroupsAsync(SettingsHelper.Instance.WorkspaceKey);
                this.DataContext = personGroups.OrderBy(pg => pg.Name);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure dowloading groups");
            }

            this.progressControl.IsActive = false;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.Frame.Navigate(
                typeof(PersonGroupDetailsPage),
                e.ClickedItem,
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private async void OnAddPersonGroupButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(SettingsHelper.Instance.WorkspaceKey))
                {
                    throw new InvalidOperationException("Before you can create groups you need to define a Workspace Key in the Settings Page.");
                }

                await FaceServiceHelper.CreatePersonGroupAsync(Guid.NewGuid().ToString(), this.personGroupNameTextBox.Text, SettingsHelper.Instance.WorkspaceKey);
                await this.LoadPersonGroupsFromService();
                this.personGroupNameTextBox.Text = "";
                this.addPersonGroupFlyout.Hide();
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure creating group");
            }
        }

        private void OnCancelAddPersonGroupButtonClicked(object sender, RoutedEventArgs e)
        {
            this.personGroupNameTextBox.Text = "";
            this.addPersonGroupFlyout.Hide();
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            await this.LoadPersonGroupsFromService();
        }
    }
}
