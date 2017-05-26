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

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using IntelligentKioskSample.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using ServiceHelpers;

namespace IntelligentKioskSample.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PersonDetailsPage : Page
    {
        public Person CurrentPerson { get; set; }
        public PersonGroup CurrentPersonGroup { get; set; }

        public ObservableCollection<PersonFace> PersonFaces { get; set; }
        public string HeaderText { get; set; }

        public PersonDetailsPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Tuple<PersonGroup, Person> pageParameter = e.Parameter as Tuple<PersonGroup, Person>;

            this.CurrentPerson = pageParameter.Item2;
            this.CurrentPersonGroup = pageParameter.Item1;
            this.HeaderText = string.Format("{0}/{1}", pageParameter.Item1.Name, pageParameter.Item2.Name);
            this.PersonFaces = new ObservableCollection<PersonFace>();
            this.bingSearchControl.DefaultSearchQuery = this.CurrentPerson.Name;

            this.DataContext = this;

            await this.LoadPersonFacesFromService();

            base.OnNavigatedTo(e);
        }

        private async Task LoadPersonFacesFromService()
        {
            this.progressControl.IsActive = true;

            this.PersonFaces.Clear();

            try
            {
                Person latestVersionOfCurrentPerson = await FaceServiceHelper.GetPersonAsync(this.CurrentPersonGroup.PersonGroupId, this.CurrentPerson.PersonId);
                this.CurrentPerson.PersistedFaceIds = latestVersionOfCurrentPerson.PersistedFaceIds;

                if (this.CurrentPerson.PersistedFaceIds != null)
                {
                    foreach (Guid face in this.CurrentPerson.PersistedFaceIds)
                    {
                        PersonFace personFace = await FaceServiceHelper.GetPersonFaceAsync(this.CurrentPersonGroup.PersonGroupId, this.CurrentPerson.PersonId, face);
                        this.PersonFaces.Add(personFace);
                    }
                }
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure downloading person faces");
            }

            this.progressControl.IsActive = false;
        }


        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            this.progressControl.IsActive = true;

            this.trainingImageCollectorFlyout.Hide();

            bool foundError = false;
            Exception lastError = null;
            foreach (var item in args)
            {
                try
                {
                    if (item.GetImageStreamCallback != null)
                    {
                        await FaceServiceHelper.AddPersonFaceAsync(
                            this.CurrentPersonGroup.PersonGroupId,
                            this.CurrentPerson.PersonId,
                            imageStreamCallback: item.GetImageStreamCallback,
                            userData: item.LocalImagePath,
                            targetFace: null);
                    }
                    else
                    {
                        await FaceServiceHelper.AddPersonFaceAsync(
                            this.CurrentPersonGroup.PersonGroupId,
                            this.CurrentPerson.PersonId,
                            imageUrl: item.ImageUrl,
                            userData: item.ImageUrl,
                            targetFace: null);
                    }
                }
                catch (Exception e)
                {
                    foundError = true;
                    lastError = e;
                }
            }

            if (foundError)
            {
                await Util.GenericApiCallExceptionHandler(lastError, "Failure adding one or more of the faces");
            }

            await this.LoadPersonFacesFromService();

            this.progressControl.IsActive = false;
        }

        private async void OnDeletePersonClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete person?", async () => { await DeletePersonAsync(); });
        }

        private async Task DeletePersonAsync()
        {
            try
            {
                await FaceServiceHelper.DeletePersonAsync(this.CurrentPersonGroup.PersonGroupId, this.CurrentPerson.PersonId);
                this.Frame.GoBack();
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting person");
            }
        }

        private void OnImageSearchCanceled(object sender, EventArgs e)
        {
            this.trainingImageCollectorFlyout.Hide();
        }

        private async void DeleteSelectedImagesClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in this.imagesGridView.SelectedItems)
                {
                    await FaceServiceHelper.DeletePersonFaceAsync(this.CurrentPersonGroup.PersonGroupId, this.CurrentPerson.PersonId, ((PersonFace)item).PersistedFaceId);
                }
                await this.LoadPersonFacesFromService();
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting images");
            }
        }

        private void ImageRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void OnImageDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            PersonFace dataContext = sender.DataContext as PersonFace;

            if (dataContext != null)
            {
                Image image = sender as Image;
                if (image != null)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    image.Source = bitmapImage;

                    try
                    {
                        if (Path.IsPathRooted(dataContext.UserData))
                        {
                            // local file
                            bitmapImage.SetSource(await (await StorageFile.GetFileFromPathAsync(dataContext.UserData)).OpenReadAsync());
                        }
                        else
                        {
                            // url
                            bitmapImage.UriSource = new Uri(dataContext.UserData);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
