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
using IntelligentKioskSample.Controls;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PersonGroupDetailsPage : Page, INotifyPropertyChanged
    {
        private StorageFolder autoTrainFolder;

        public PersonGroup CurrentPersonGroup { get; set; }
        public ObservableCollection<Person> PersonsInCurrentGroup { get; set; }

        public PersonGroupDetailsPage()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.CurrentPersonGroup = e.Parameter as PersonGroup;
            this.PersonsInCurrentGroup = new ObservableCollection<Person>();

            await this.LoadPersonsInCurrentGroup();

            this.DataContext = this;

            base.OnNavigatedTo(e);
        }

        private async Task LoadPersonsInCurrentGroup()
        {
            this.PersonsInCurrentGroup.Clear();

            try
            {
                Person[] personsInGroup = await FaceServiceHelper.GetPersonsAsync(this.CurrentPersonGroup.PersonGroupId);
                foreach (Person person in personsInGroup.OrderBy(p => p.Name))
                {
                    this.PersonsInCurrentGroup.Add(person);
                }
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure loading people in the group");
            }
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.Frame.Navigate(
                typeof(PersonDetailsPage),
                new Tuple<PersonGroup, Person>(this.CurrentPersonGroup, e.ClickedItem as Person),
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private async void OnAddPersonButtonClicked(object sender, RoutedEventArgs e)
        {
            await this.CreatePersonAsync(this.personNameTextBox.Text);
        }

        private void DismissFlyout()
        {
            this.addPersonFlyout.Hide();
            this.personNameTextBox.Text = "";
        }

        private void OnCancelAddPersonButtonClicked(object sender, RoutedEventArgs e)
        {
            this.DismissFlyout();
        }

        private async void OnPersonNameTextBoxChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                this.personNameTextBox.ItemsSource = await BingSearchHelper.GetAutoSuggestResults(this.personNameTextBox.Text);
            }
            catch (HttpRequestException)
            {
                // default to no suggestions
                this.personNameTextBox.ItemsSource = null;
            }
        }

        private async void OnPersonNameQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await CreatePersonAsync(args.ChosenSuggestion != null ? args.ChosenSuggestion.ToString() : args.QueryText);
        }

        private async Task CreatePersonAsync(string name)
        {
            try
            {
                await FaceServiceHelper.CreatePersonAsync(this.CurrentPersonGroup.PersonGroupId, Util.CapitalizeString(name));
                await this.LoadPersonsInCurrentGroup();

                this.DismissFlyout();
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure creating person");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private async void OnDeletePersonGroupClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete person group?", async () => { await DeletePersonGroupAsync(); });
        }

        private async Task DeletePersonGroupAsync()
        {
            try
            {
                await FaceServiceHelper.DeletePersonGroupAsync(this.CurrentPersonGroup.PersonGroupId);
                this.Frame.GoBack();
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting person group");
            }
        }

        private async void OnStartTrainingClicked(object sender, RoutedEventArgs e)
        {
            this.progressControl.IsActive = true;

            TrainingStatus trainingStatus = null;
            try
            {
                await FaceServiceHelper.TrainPersonGroupAsync(this.CurrentPersonGroup.PersonGroupId);

                while (true)
                {
                    trainingStatus = await FaceServiceHelper.GetPersonGroupTrainingStatusAsync(this.CurrentPersonGroup.PersonGroupId);

                    if (trainingStatus.Status != Status.Running)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure requesting training");
            }

            this.progressControl.IsActive = false;

            if (trainingStatus.Status != Status.Succeeded)
            {
                await new MessageDialog("Training finished with failure.").ShowAsync();
            }
        }

        private async void OnConfirmImportButtonClicked(object sender, RoutedEventArgs e)
        {
            this.addPeopleInBatchesFlyout.Hide();
            this.commandBar.IsOpen = false;

            this.progressControl.IsActive = true;

            try
            {
                string[] names = this.importNamesTextBox.Text.Split('\n');
                foreach (var name in names)
                {
                    string personName = Util.CapitalizeString(name.Trim());
                    if (string.IsNullOrEmpty(personName) || this.PersonsInCurrentGroup.Any(p => p.Name == personName))
                    {
                        continue;
                    }

                    await FaceServiceHelper.CreatePersonAsync(this.CurrentPersonGroup.PersonGroupId, personName);
                    Person newPerson = (await FaceServiceHelper.GetPersonsAsync(this.CurrentPersonGroup.PersonGroupId)).First(p => p.Name == personName);

                    IEnumerable<string> faceUrls = await BingSearchHelper.GetImageSearchResults(string.Format("{0} {1} {2}", this.importImageSearchKeywordPrefix.Text, name, this.importImageSearchKeywordSufix.Text), count: 2);
                    foreach (var url in faceUrls)
                    {
                        try
                        {
                            ImageAnalyzer imageWithFace = new ImageAnalyzer(url);

                            await imageWithFace.DetectFacesAsync();

                            if (imageWithFace.DetectedFaces.Count() == 1)
                            {
                                await FaceServiceHelper.AddPersonFaceAsync(this.CurrentPersonGroup.PersonGroupId, newPerson.PersonId, imageWithFace.ImageUrl, imageWithFace.ImageUrl, imageWithFace.DetectedFaces.First().FaceRectangle);
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore errors with any particular images and continue
                        }

                        // Force a delay to reduce the chance of hitting API call rate limits 
                        await Task.Delay(250);
                    }

                    this.PersonsInCurrentGroup.Add(newPerson);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure during batch processing");
            }

            this.progressControl.IsActive = false;
        }

        private void OnCancelImportButtonClicked(object sender, RoutedEventArgs e)
        {
            this.addPeopleInBatchesFlyout.Hide();
        }

        private async void OnSelectFolderButtonClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Please select a root folder to start. The subfolder names will map to people names, and the photos inside those folders will map to their sample photos. Continue?", async () => { await PickFolderASync(); });
        }

        private async Task PickFolderASync()
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                folderPicker.FileTypeFilter.Add(".jpeg");
                folderPicker.FileTypeFilter.Add(".bmp");
                autoTrainFolder = await folderPicker.PickSingleFolderAsync();

                if (autoTrainFolder != null)
                {
                    await ImportFromFolderAndFilesAsync();
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error picking the target folder.");
            }
        }

        private async Task ImportFromFolderAndFilesAsync()
        {
            this.commandBar.IsOpen = false;

            this.progressControl.IsActive = true;

            List<string> errors = new List<string>();

            try
            {
                foreach (var folder in await autoTrainFolder.GetFoldersAsync())
                {
                    string personName = Util.CapitalizeString(folder.Name.Trim());
                    if (string.IsNullOrEmpty(personName))
                    {
                        continue;
                    }

                    if (!this.PersonsInCurrentGroup.Any(p => p.Name == personName))
                    {
                        await FaceServiceHelper.CreatePersonAsync(this.CurrentPersonGroup.PersonGroupId, personName);
                    }

                    Person newPerson = (await FaceServiceHelper.GetPersonsAsync(this.CurrentPersonGroup.PersonGroupId)).First(p => p.Name == personName);

                    foreach (var photoFile in await folder.GetFilesAsync())
                    {
                        try
                        {
                            await FaceServiceHelper.AddPersonFaceAsync(
                                this.CurrentPersonGroup.PersonGroupId,
                                newPerson.PersonId,
                                imageStreamCallback: photoFile.OpenStreamForReadAsync,
                                userData: photoFile.Path,
                                targetFace: null);

                            // Force a delay to reduce the chance of hitting API call rate limits 
                            await Task.Delay(250);
                        }
                        catch (Exception)
                        {
                            errors.Add(photoFile.Path);
                        }
                    }

                    this.PersonsInCurrentGroup.Add(newPerson);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure processing the folder and files");
            }

            if (errors.Any())
            {
                await new MessageDialog(string.Join("\n", errors), "Failure importing the folllowing photos").ShowAsync();
            }

            this.progressControl.IsActive = false;
        }
    }
}
