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
using ServiceHelpers;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Controls
{
    public sealed partial class FaceIdentificationModelUpdateDialog : ContentDialog
    {
        readonly PersonGroup personGroup;

        public bool PersonGroupUpdated { get; private set; } = false;

        public FaceIdentificationModelUpdateDialog(PersonGroup personGroup)
        {
            this.personGroup = personGroup;
            this.InitializeComponent();
        }

        private async void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            try
            {
                this.PrimaryButtonText = string.Empty;
                this.SecondaryButtonText = string.Empty;

                Progress<FaceIdentificationModelUpdateStatus> progress = new Progress<FaceIdentificationModelUpdateStatus>(ProgressChanged);
                await FaceServiceHelper.UpdatePersonGroupsWithNewRecModelAsync(this.personGroup, SettingsHelper.Instance.WorkspaceKey, progress);
            }
            finally
            {
                this.progressPanel.Visibility = Visibility.Collapsed;
                this.SecondaryButtonText = "Close";
            }
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            // ignore Close events except Secondary button
            if (args.Result != ContentDialogResult.Secondary)
            {
                args.Cancel = true;
            }
        }

        private void ProgressChanged(FaceIdentificationModelUpdateStatus status)
        {
            switch (status.State)
            {
                case FaceIdentificationModelUpdateState.Complete:
                case FaceIdentificationModelUpdateState.CompletedWithSomeEmptyPeople:
                    this.updateLabel.Text = "Complete";
                    this.progressBar.Value = this.progressBar.Maximum;
                    this.subtitle.Text = status.State == FaceIdentificationModelUpdateState.Complete 
                        ? "Your Face Identification Group has been successfully updated" 
                        : "We updated the group, but some of the people in it no longer have training images. This could indicate the kiosk no longer has access to the original training images from when this group was initially created. Please review all the people in the group, making sure each member has at least one training image. Once that is done, click on the Train Group button to retrain the group.";
                    this.PersonGroupUpdated = true;
                    break;

                case FaceIdentificationModelUpdateState.Running:
                    this.progressBar.Value = status.Total != 0 ? (double)status.Count / status.Total : this.progressBar.Value;
                    break;

                case FaceIdentificationModelUpdateState.Error:
                    this.updateLabel.Text = "Error";
                    this.subtitle.Text = "We failed to update this group. You might need to re-create it manually.";
                    this.PersonGroupUpdated = false;
                    break;
            }
        }
    }
}
