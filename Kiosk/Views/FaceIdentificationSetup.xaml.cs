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
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IntelligentKioskSample.Views
{
    public sealed partial class FaceIdentificationSetup : Page
    {
        private bool needsTraining = false;

        public ObservableCollection<PersonGroup> PersonGroups { get; set; } = new ObservableCollection<PersonGroup>();
        public PersonGroup CurrentPersonGroup { get; set; }
        public ObservableCollection<Person> PersonsInCurrentGroup { get; set; } = new ObservableCollection<Person>();
        public ObservableCollection<PersistedFace> SelectedPersonFaces { get; set; } = new ObservableCollection<PersistedFace>();
        public Person SelectedPerson { get; set; }

        public FaceIdentificationSetup()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = this;

            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey))
            {
                await new MessageDialog("If you would like to configure the kiosk to recognize individuals, please enter a valid Face API key and Workspace Name in the Settings Page.").ShowAsync();
            }
            else
            {
                await this.LoadPersonGroupsFromService();
            }

            this.UpdateTrainingUIState();

            base.OnNavigatedTo(e);
        }

        protected async override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (this.needsTraining)
            {
                e.Cancel = true;
                await Util.ConfirmActionAndExecute("It looks like you made modifications but didn't train the model afterwards. Would you like to train now?", async () => await this.TrainGroupsAsync());
            }

            base.OnNavigatingFrom(e);
        }

        private void UpdateTrainingUIState()
        {
            bool canTrain = false, canCreatePersonGroups = false;
            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey))
            {
                canCreatePersonGroups = false;
                canTrain = false;
            }
            else
            {
                if (!this.PersonGroups.Any())
                {
                    canCreatePersonGroups = true;
                    canTrain = false;
                }
                else
                {
                    canCreatePersonGroups = true;
                    canTrain = true;
                }
            }

            this.trainButton.IsEnabled = canTrain;
            this.bulkTrainFromBingButton.IsEnabled = canTrain;
            this.bulkTrainFromFolderButton.IsEnabled = canTrain;

            this.addPersonGroupButton.IsEnabled = canCreatePersonGroups;
        }

        #region Group management

        private async Task LoadPersonGroupsFromService(bool autoSelectFirstGroup = true)
        {
            this.progressControl.IsActive = true;

            try
            {
                this.PersonGroups.Clear();
                IEnumerable<PersonGroup> personGroups = await FaceServiceHelper.ListPersonGroupsAsync(SettingsHelper.Instance.WorkspaceKey);
                this.PersonGroups.AddRange(personGroups.OrderBy(pg => pg.Name));

                if (this.personGroupsListView.Items.Any() && autoSelectFirstGroup)
                {
                    this.personGroupsListView.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading Person Groups");
            }

            this.progressControl.IsActive = false;
        }

        private async void OnAddPersonGroupButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(SettingsHelper.Instance.WorkspaceKey))
                {
                    throw new InvalidOperationException("Before you can create groups you need to define a Workspace Name in the Settings Page.");
                }

                Guid personGroupGuid = Guid.NewGuid();
                await FaceServiceHelper.CreatePersonGroupAsync(personGroupGuid.ToString(), this.personGroupNameTextBox.Text, SettingsHelper.Instance.WorkspaceKey);
                PersonGroup newGroup = new PersonGroup { Name = this.personGroupNameTextBox.Text, PersonGroupId = personGroupGuid.ToString(), RecognitionModel = FaceServiceHelper.LatestRecognitionModelName };

                this.PersonGroups.Add(newGroup);
                this.personGroupsListView.SelectedValue = newGroup;

                this.personGroupNameTextBox.Text = "";
                this.addPersonGroupFlyout.Hide();

                this.needsTraining = true;
                this.UpdateTrainingUIState();
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

        private async void OnDeletePersonGroupClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete person group?", async () => { await DeletePersonGroupAsync(); });
        }

        private async Task DeletePersonGroupAsync()
        {
            try
            {
                await FaceServiceHelper.DeletePersonGroupAsync(this.CurrentPersonGroup.PersonGroupId);
                this.PersonGroups.Remove(this.CurrentPersonGroup);

                this.PersonsInCurrentGroup.Clear();
                this.SelectedPersonFaces.Clear();

                this.UpdateTrainingUIState();
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting person group");
            }
        }

        private async void OnGroupSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.CurrentPersonGroup = (PersonGroup)this.personGroupsListView.SelectedValue;

            if (this.CurrentPersonGroup != null)
            {
                await this.LoadPersonsInCurrentGroup();
            }
        }

        #endregion

        #region People magagement

        private async void OnPersonSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.personGroupPeopleListView.SelectedValue != null)
            {
                this.SelectedPerson = this.personGroupPeopleListView.SelectedValue as Person;
                await this.LoadPersonFacesFromService();
            }
            else
            {
                this.SelectedPersonFaces.Clear();
            }
        }

        private async Task LoadPersonsInCurrentGroup()
        {
            this.PersonsInCurrentGroup.Clear();

            try
            {
                IList<Person> personsInGroup = await FaceServiceHelper.GetPersonsAsync(this.CurrentPersonGroup.PersonGroupId);
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

        private async void OnAddPersonButtonClicked(object sender, RoutedEventArgs e)
        {
            await this.AddPerson(this.personNameTextBox.Text);
        }

        private async void OnPersonNameQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await this.AddPerson(args.ChosenSuggestion != null ? args.ChosenSuggestion.ToString() : args.QueryText);
        }

        private async Task AddPerson(string name)
        {
            name = Util.CapitalizeString(name);

            await this.CreatePersonAsync(name);
            this.personGroupPeopleListView.SelectedValue = this.PersonsInCurrentGroup.FirstOrDefault(p => p.Name == name);
            trainingImageCollectorFlyout.ShowAt(this.addFacesButton);
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
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
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
        }

        private async Task CreatePersonAsync(string name)
        {
            try
            {
                Person person = await FaceServiceHelper.CreatePersonAsync(this.CurrentPersonGroup.PersonGroupId, name);
                this.PersonsInCurrentGroup.Add(new Person { Name = name, PersonId = person.PersonId });
                this.needsTraining = true;
                this.DismissFlyout();
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure creating person");
            }
        }

        private async void OnDeletePersonClicked(object sender, RoutedEventArgs e)
        {
            await Util.ConfirmActionAndExecute("Delete person?", async () => { await DeletePersonAsync(); });
        }

        private async Task DeletePersonAsync()
        {
            try
            {
                await FaceServiceHelper.DeletePersonAsync(this.CurrentPersonGroup.PersonGroupId, this.SelectedPerson.PersonId);
                this.PersonsInCurrentGroup.Remove(this.SelectedPerson);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting person");
            }
        }


        #endregion

        #region Face management

        private async Task LoadPersonFacesFromService()
        {
            this.progressControl.IsActive = true;

            this.SelectedPersonFaces.Clear();

            try
            {
                Person latestVersionOfCurrentPerson = await FaceServiceHelper.GetPersonAsync(this.CurrentPersonGroup.PersonGroupId, this.SelectedPerson.PersonId);
                this.SelectedPerson.PersistedFaceIds = latestVersionOfCurrentPerson.PersistedFaceIds;

                if (this.SelectedPerson.PersistedFaceIds != null)
                {
                    foreach (Guid face in this.SelectedPerson.PersistedFaceIds)
                    {
                        PersistedFace personFace = await FaceServiceHelper.GetPersonFaceAsync(this.CurrentPersonGroup.PersonGroupId, this.SelectedPerson.PersonId, face);
                        this.SelectedPersonFaces.Add(personFace);
                    }
                }
            }
            catch (Exception e)
            {
                await Util.GenericApiCallExceptionHandler(e, "Failure downloading person faces");
            }

            this.progressControl.IsActive = false;
        }

        private void OnImageSearchCanceled(object sender, EventArgs e)
        {
            this.trainingImageCollectorFlyout.Hide();
        }

        private async void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            await AddTrainingImages(args);
        }

        private async void OnCameraFrameCaptured(object sender, IEnumerable<ImageAnalyzer> e)
        {
            await this.AddTrainingImages(e, dismissImageCollectorFlyout: false);
        }

        private async Task AddTrainingImages(IEnumerable<ImageAnalyzer> args, bool dismissImageCollectorFlyout = true)
        {
            this.progressControl.IsActive = true;

            if (dismissImageCollectorFlyout)
            {
                this.trainingImageCollectorFlyout.Hide();
            }

            bool foundError = false;
            Exception lastError = null;
            foreach (var item in args)
            {
                try
                {
                    PersistedFace addResult;
                    if (item.GetImageStreamCallback != null)
                    {
                        addResult = await FaceServiceHelper.AddPersonFaceFromStreamAsync(
                            this.CurrentPersonGroup.PersonGroupId,
                            this.SelectedPerson.PersonId,
                            imageStreamCallback: item.GetImageStreamCallback,
                            userData: item.LocalImagePath,
                            targetFaceRect: null);
                    }
                    else
                    {
                        addResult = await FaceServiceHelper.AddPersonFaceFromUrlAsync(
                            this.CurrentPersonGroup.PersonGroupId,
                            this.SelectedPerson.PersonId,
                            imageUrl: item.ImageUrl,
                            userData: item.ImageUrl,
                            targetFaceRect: null);
                    }

                    if (addResult != null)
                    {
                        this.SelectedPersonFaces.Add(new PersistedFace { PersistedFaceId = addResult.PersistedFaceId, UserData = item.GetImageStreamCallback != null ? item.LocalImagePath : item.ImageUrl });
                        this.needsTraining = true;
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

            this.progressControl.IsActive = false;
        }

        private void OnImageSearchFlyoutOpened(object sender, object e)
        {
            this.bingSearchControl.TriggerSearch(this.SelectedPerson.Name);
        }

        private async void OnDeleteFaceClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in this.selectedPersonFacesGridView.SelectedItems.ToArray())
                {
                    PersistedFace personFace = (PersistedFace)item;
                    await FaceServiceHelper.DeletePersonFaceAsync(this.CurrentPersonGroup.PersonGroupId, this.SelectedPerson.PersonId, personFace.PersistedFaceId);
                    this.SelectedPersonFaces.Remove(personFace);

                    this.needsTraining = true;
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure deleting images");
            }
        }

        private async void OnImageDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            PersistedFace dataContext = sender.DataContext as PersistedFace;

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

        #endregion

        #region Batch processing

        private async void OnConfirmImportButtonClicked(object sender, RoutedEventArgs e)
        {
            this.addPeopleInBatchesFlyout.Hide();
            this.commandBar.IsOpen = false;

            this.progressControl.IsActive = true;

            try
            {
                // UWP TextBox: new line is a '\r' symbol instead '\r\n'
                string[] names = new string[] { };
                if (!string.IsNullOrEmpty(this.importNamesTextBox?.Text))
                {
                    string newLineSymbol = this.importNamesTextBox.Text.Contains(Environment.NewLine) ? Environment.NewLine : "\r";
                    names = this.importNamesTextBox.Text.Split(newLineSymbol);
                }
                foreach (var name in names)
                {
                    string personName = Util.CapitalizeString(name.Trim());
                    if (string.IsNullOrEmpty(personName) || this.PersonsInCurrentGroup.Any(p => p.Name == personName))
                    {
                        continue;
                    }

                    Person newPerson = await FaceServiceHelper.CreatePersonAsync(this.CurrentPersonGroup.PersonGroupId, personName);

                    IEnumerable<string> faceUrls = await BingSearchHelper.GetImageSearchResults(string.Format("{0} {1} {2}", this.importImageSearchKeywordPrefix.Text, name, this.importImageSearchKeywordSufix.Text), count: 2);
                    foreach (var url in faceUrls)
                    {
                        try
                        {
                            ImageAnalyzer imageWithFace = new ImageAnalyzer(url);

                            await imageWithFace.DetectFacesAsync();

                            if (imageWithFace.DetectedFaces.Count() == 1)
                            {
                                await FaceServiceHelper.AddPersonFaceFromUrlAsync(this.CurrentPersonGroup.PersonGroupId, newPerson.PersonId, imageWithFace.ImageUrl, imageWithFace.ImageUrl, imageWithFace.DetectedFaces.First().FaceRectangle);
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore errors with any particular image and continue
                        }

                        // Force a delay to reduce the chance of hitting API call rate limits 
                        await Task.Delay(250);
                    }

                    this.needsTraining = true;

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
                StorageFolder autoTrainFolder = await folderPicker.PickSingleFolderAsync();

                if (autoTrainFolder != null)
                {
                    await ImportFromFolderAndFilesAsync(autoTrainFolder);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Error picking the target folder.");
            }
        }

        private async Task ImportFromFolderAndFilesAsync(StorageFolder autoTrainFolder)
        {
            this.commandBar.IsOpen = false;

            this.progressControl.IsActive = true;

            List<string> errors = new List<string>();

            try
            {
                foreach (var folder in await autoTrainFolder.GetFoldersAsync())
                {
                    string personName = Util.CapitalizeString(folder.Name.Trim());
                    if (string.IsNullOrEmpty(personName) || this.PersonsInCurrentGroup.Any(p => p.Name == personName))
                    {
                        continue;
                    }

                    Person newPerson = await FaceServiceHelper.CreatePersonAsync(this.CurrentPersonGroup.PersonGroupId, personName);

                    foreach (var photoFile in await folder.GetFilesAsync())
                    {
                        try
                        {
                            await FaceServiceHelper.AddPersonFaceFromStreamAsync(
                                this.CurrentPersonGroup.PersonGroupId,
                                newPerson.PersonId,
                                imageStreamCallback: photoFile.OpenStreamForReadAsync,
                                userData: photoFile.Path,
                                targetFaceRect: null);

                            // Force a delay to reduce the chance of hitting API call rate limits 
                            await Task.Delay(250);
                        }
                        catch (Exception)
                        {
                            errors.Add(photoFile.Path);
                        }
                    }

                    this.needsTraining = true;

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

        #endregion

        #region Training processing

        private async void OnStartTrainingClicked(object sender, RoutedEventArgs e)
        {
            await TrainGroupsAsync();
        }

        private async Task TrainGroupsAsync()
        {
            this.progressControl.IsActive = true;

            bool trainingSucceeded = true;
            try
            {
                foreach (var group in this.PersonGroups)
                {
                    await FaceServiceHelper.TrainPersonGroupAsync(group.PersonGroupId);

                    while (true)
                    {
                        TrainingStatus trainingStatus = await FaceServiceHelper.GetPersonGroupTrainingStatusAsync(group.PersonGroupId);

                        if (trainingStatus.Status != TrainingStatusType.Running)
                        {
                            if (trainingStatus.Status == TrainingStatusType.Failed)
                            {
                                trainingSucceeded = false;
                            }

                            break;
                        }
                        await Task.Delay(500);
                    }
                }

                this.needsTraining = false;
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure requesting training");
            }

            this.progressControl.IsActive = false;

            if (!trainingSucceeded)
            {
                await new MessageDialog("Training failed. Make sure you have at least one person per group and at least one face per person.").ShowAsync();
            }
        }

        #endregion

        private async void OnMigrateToLatestFaceRecognitionModelButtonClicked(object sender, RoutedEventArgs e)
        {
            string personGroupName = CurrentPersonGroup.Name;
            FaceIdentificationModelUpdateDialog dialog = new FaceIdentificationModelUpdateDialog(CurrentPersonGroup);
            await dialog.ShowAsync();

            // refresh page if user successfully updated the current model
            if (dialog.PersonGroupUpdated)
            {
                await this.LoadPersonGroupsFromService(autoSelectFirstGroup: false);

                // select new person group
                PersonGroup selectedPersonGroup = this.PersonGroups.FirstOrDefault(x => x.Name.Equals(personGroupName));
                if (this.personGroupsListView.Items.Any())
                {
                    int selectedItemIndex = this.personGroupsListView.Items.IndexOf(selectedPersonGroup);
                    this.personGroupsListView.SelectedIndex = selectedItemIndex != -1 ? selectedItemIndex : 0;
                }
            }
        }
    }

    public class ReverseLatestFaceRecognitionModelNameToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && string.Equals(value.ToString(), FaceServiceHelper.LatestRecognitionModelName, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
