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

using Microsoft.Azure.Storage.Blob;
using ServiceHelpers;
using ServiceHelpers.Models.FormRecognizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Views.FormRecognizer
{
    public sealed partial class FormRecognizerScenarioSetup : UserControl, INotifyPropertyChanged
    {
        public static readonly string StorageAccount = ""; // STORAGE ACCOUNT FOR CREATING CUSTOM FORM RECOGNIZER MODEL
        public static readonly string StorageKey = "";     // STORAGE KEY FOR CREATING CUSTOM FORM RECOGNIZER MODEL

        private FormRecognizerService formRecognizerService;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int MinFilesCount { get; } = 5;

        public ObservableCollection<StorageFile> LocalFileCollection { get; private set; } = new ObservableCollection<StorageFile>();

        private FormRecognizerScenarioSetupState scenarioSetupState = FormRecognizerScenarioSetupState.NotStarted;
        public FormRecognizerScenarioSetupState ScenarioSetupState
        {
            get { return scenarioSetupState; }
            set
            {
                scenarioSetupState = value;
                NotifyPropertyChanged("ScenarioSetupState");
            }
        }

        private string scenarioCreationErrorDetails;
        public string ScenarioCreationErrorDetails
        {
            get { return scenarioCreationErrorDetails; }
            set
            {
                scenarioCreationErrorDetails = value;
                NotifyPropertyChanged(nameof(ScenarioCreationErrorDetails));
            }
        }

        public event EventHandler<FormRecognizerViewModel> ModelCreated;

        public FormRecognizerScenarioSetup()
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.LocalFileCollection.CollectionChanged += LocalFileCollectionChanged;
        }

        public void OpenScenarioSetupForm(FormRecognizerService formRecognizerService)
        {
            this.hostGrid.Height = Window.Current.Bounds.Height;
            this.hostGrid.Width = Window.Current.Bounds.Width;

            this.formRecognizerService = formRecognizerService;

            this.newFormScenario.IsOpen = true;
        }

        private void OnCloseScenarioFormButtonClicked(object sender, RoutedEventArgs e)
        {
            LocalFileCollection.Clear();
            this.scenarioNameTextBox.Text = string.Empty;
            ScenarioSetupState = FormRecognizerScenarioSetupState.NotStarted;

            this.newFormScenario.IsOpen = false;
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateSetupForm();
        }

        private void FileListView_OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void FileListView_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var storageItems = await e.DataView.GetStorageItemsAsync();
                LocalFileCollection.AddRange(storageItems.Select(x => (StorageFile)x).ToList());
            }
        }

        public void LocalFileCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ValidateSetupForm();
        }

        private async void OnBrowseFilesButtonClicked(object sender, RoutedEventArgs e)
        {
            string[] inputFormExtensions = FormRecognizerExplorer.ImageExtensions.Concat(FormRecognizerExplorer.PdfExtensions).ToArray();
            IReadOnlyList<StorageFile> selectedFiles = await Util.PickMultipleFilesAsync(inputFormExtensions);
            if (selectedFiles != null && selectedFiles.Any())
            {
                LocalFileCollection.AddRange(selectedFiles);
            }
        }

        private void OnDeleteFileButtonClicked(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (button?.DataContext is StorageFile curFile)
            {
                LocalFileCollection.Remove(curFile);
            }
        }

        private async void OnCreateScenarioButtonClicked(object sender, RoutedEventArgs e)
        {
            Guid modelId = await CreateNewScenarioAsync();
            if (modelId != Guid.Empty)
            {
                var suggestionFiles = new List<Tuple<string, Uri>>();
                foreach (StorageFile file in LocalFileCollection.Take(MinFilesCount))
                {
                    StorageFile copyFile = await FormRecognizerDataLoader.CopyFileToLocalModelFolderAsync(file, modelId);
                    suggestionFiles.Add(new Tuple<string, Uri>(copyFile.Name, new Uri(copyFile.Path)));
                }

                this.ModelCreated?.Invoke(this, new FormRecognizerViewModel()
                {
                    Id = modelId,
                    Name = this.scenarioNameTextBox.Text,
                    IsPrebuiltModel = false,
                    SuggestionSamples = suggestionFiles
                });
            }
        }

        private async Task<Guid> CreateNewScenarioAsync()
        {
            CloudBlobContainer formRecognizerCloudContainer = null;

            try
            {
                ScenarioSetupState = FormRecognizerScenarioSetupState.Running;

                // create cloud storage container and upload local files
                formRecognizerCloudContainer = AzureBlobHelper.GetCloudBlobContainer(StorageAccount, StorageKey, Guid.NewGuid().ToString());
                await AzureBlobHelper.UploadStorageFilesToContainerAsync(LocalFileCollection, formRecognizerCloudContainer);
                string containerFullSASUrl = AzureBlobHelper.GetContainerSasToken(formRecognizerCloudContainer, sharedAccessStartTimeInMinutes: 5, sharedAccessExpiryTimeInMinutes: 5);

                // create and train the custom model
                Guid modelId = await this.formRecognizerService.TrainCustomModelAsync(containerFullSASUrl);
                ModelResultResponse model = await this.formRecognizerService.GetCustomModelAsync(modelId);

                // check creation status
                string status = (model?.ModelInfo?.Status ?? string.Empty).ToLower();
                switch (status)
                {
                    case "created":
                    case "ready":
                        ScenarioSetupState = FormRecognizerScenarioSetupState.Completed;
                        return modelId;

                    case "invalid":
                    default:
                        ScenarioSetupState = FormRecognizerScenarioSetupState.Error;
                        break;
                }
            }
            catch (Exception ex)
            {
                ScenarioCreationErrorDetails = ex.Message;
                ScenarioSetupState = FormRecognizerScenarioSetupState.Error;
            }
            finally
            {
                if (formRecognizerCloudContainer != null)
                {
                    await formRecognizerCloudContainer.DeleteIfExistsAsync();
                }
            }

            return Guid.Empty;
        }

        private void OnTryAgainButtonClicked(object sender, RoutedEventArgs e)
        {
            ScenarioSetupState = FormRecognizerScenarioSetupState.NotStarted;
            ValidateSetupForm();
        }

        private void ValidateSetupForm()
        {
            bool isValidForm = LocalFileCollection?.Count >= MinFilesCount && !string.IsNullOrEmpty(this.scenarioNameTextBox.Text);
            this.scenarioCreateButton.IsEnabled = isValidForm;
        }


        private void OnPopUpOpened(object sender, object e)
        {
            Window.Current.SizeChanged += WindowSizeChanged;
        }

        private void OnPopUpClosed(object sender, object e)
        {
            Window.Current.SizeChanged -= WindowSizeChanged;
        }

        private void WindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            this.hostGrid.Height = e.Size.Height;
            this.hostGrid.Width = e.Size.Width;
        }
    }

    public enum FormRecognizerScenarioSetupState
    {
        NotStarted,
        Running,
        Completed,
        Error
    }
}
