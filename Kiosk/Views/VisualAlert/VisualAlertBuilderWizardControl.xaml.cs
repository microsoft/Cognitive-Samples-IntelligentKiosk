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
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace IntelligentKioskSample.Views.VisualAlert
{
    public sealed partial class VisualAlertBuilderWizardControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int MinPositiveImageCount { get; } = 5;
        public int MinNegativeImageCount { get; } = 5;

        public ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>> PositiveSubjectImageCollection { get; set; } = new ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>>();
        public ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>> SelectedPositiveSubjectImageCollection { get; set; } = new ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>>();
        public ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>> NegativeSubjectImageCollection { get; set; } = new ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>>();
        public ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>> SelectedNegativeSubjectImageCollection { get; set; } = new ObservableCollection<Tuple<BitmapImage, ImageAnalyzer>>();

        private string subjectName = string.Empty;
        public string SubjectName
        {
            get { return subjectName; }
            set
            {
                subjectName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SubjectName"));
            }
        }

        private VisualAlertBuilderStepType builderStepType;
        public VisualAlertBuilderStepType BuilderStepType
        {
            get { return builderStepType; }
            set
            {
                builderStepType = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BuilderStepType"));
                UpdateWizardView();
            }
        }

        public event EventHandler<VisualAlertModelData> WizardCompleted;
        public event EventHandler<Tuple<VisualAlertBuilderStepType, VisualAlertModelData>> WizardStepChanged;

        public VisualAlertBuilderWizardControl()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        public void StartWizard()
        {
            SubjectName = string.Empty;

            PositiveSubjectImageCollection.Clear();
            NegativeSubjectImageCollection.Clear();
            SelectedPositiveSubjectImageCollection.Clear();
            SelectedNegativeSubjectImageCollection.Clear();

            BuilderStepType = VisualAlertBuilderStepType.NewSubject;
        }

        public async void AddNewImage(ImageAnalyzer img)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                await bitmap.SetSourceAsync((await img.GetImageStreamCallback()).AsRandomAccessStream());
                var imageItem = new Tuple<BitmapImage, ImageAnalyzer>(bitmap, img);

                switch (BuilderStepType)
                {
                    case VisualAlertBuilderStepType.AddPositiveImages:
                        PositiveSubjectImageCollection.Add(imageItem);
                        if (this.positiveImagesGrid.SelectionMode == ListViewSelectionMode.Multiple)
                        {
                            this.positiveImagesGrid.SelectedItems.Add(imageItem);
                        }
                        break;

                    case VisualAlertBuilderStepType.AddNegativeImages:
                        NegativeSubjectImageCollection.Add(imageItem);
                        if (this.negativeImagesGrid.SelectionMode == ListViewSelectionMode.Multiple)
                        {
                            this.negativeImagesGrid.SelectedItems.Add(imageItem);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                if (SettingsHelper.Instance.ShowDebugInfo)
                {
                    await Util.GenericApiCallExceptionHandler(ex, "Error loading captured image.");
                }
            }
        }

        private void UpdateWizardView()
        {
            this.nextStepButton.Content = "Next";
            switch (BuilderStepType)
            {
                case VisualAlertBuilderStepType.NewSubject:
                    this.stepNumber.Text = "1";
                    this.nextStepButton.IsEnabled = !string.IsNullOrEmpty(SubjectName);
                    break;

                case VisualAlertBuilderStepType.AddPositiveImages:
                    this.nextStepButton.IsEnabled = PositiveSubjectImageCollection.Any();
                    this.imageCount.Text = $"{PositiveSubjectImageCollection.Count}";
                    this.imageCountTextBlock.Visibility = Visibility.Visible;
                    break;

                case VisualAlertBuilderStepType.AddNegativeImages:
                    this.stepNumber.Text = "2";

                    this.nextStepButton.IsEnabled = NegativeSubjectImageCollection.Any();
                    this.imageCount.Text = $"{NegativeSubjectImageCollection.Count}";
                    this.imageCountTextBlock.Visibility = Visibility.Visible;

                    if (this.positiveImagesGrid.SelectedItems != null)
                    {
                        SelectedPositiveSubjectImageCollection.Clear();
                        var selectedPosImages = this.positiveImagesGrid.SelectedItems.Cast<Tuple<BitmapImage, ImageAnalyzer>>().ToArray();
                        SelectedPositiveSubjectImageCollection.AddRange(selectedPosImages);
                    }
                    break;

                case VisualAlertBuilderStepType.TrainModel:
                    this.stepNumber.Text = "3";

                    this.nextStepButton.Content = "Train model";
                    this.imageCountTextBlock.Visibility = Visibility.Collapsed;

                    if (this.negativeImagesGrid.SelectedItems != null)
                    {
                        SelectedNegativeSubjectImageCollection.Clear();
                        var selectedNegImages = this.negativeImagesGrid.SelectedItems.Cast<Tuple<BitmapImage, ImageAnalyzer>>().ToArray();
                        SelectedNegativeSubjectImageCollection.AddRange(selectedNegImages);
                    }
                    break;
            }

            this.WizardStepChanged?.Invoke(this, new Tuple<VisualAlertBuilderStepType, VisualAlertModelData>(BuilderStepType,
                new VisualAlertModelData()
                {
                    Name = SubjectName,
                    PositiveImages = SelectedPositiveSubjectImageCollection.Select(x => x.Item2).ToList(),
                    NegativeImages = SelectedNegativeSubjectImageCollection.Select(x => x.Item2).ToList()
                }));
        }

        private void OnSubjectNameTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox != null && BuilderStepType == VisualAlertBuilderStepType.NewSubject)
            {
                this.nextStepButton.IsEnabled = !string.IsNullOrEmpty(textBox.Text);
            }
        }

        private void OnNextStepButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (BuilderStepType)
            {
                case VisualAlertBuilderStepType.NewSubject:
                    BuilderStepType = VisualAlertBuilderStepType.AddPositiveImages;
                    break;

                case VisualAlertBuilderStepType.AddPositiveImages:
                    BuilderStepType = VisualAlertBuilderStepType.AddNegativeImages;
                    break;

                case VisualAlertBuilderStepType.AddNegativeImages:
                    BuilderStepType = VisualAlertBuilderStepType.TrainModel;
                    break;

                case VisualAlertBuilderStepType.TrainModel:
                    this.WizardCompleted?.Invoke(this, new VisualAlertModelData()
                    {
                        Name = SubjectName,
                        PositiveImages = SelectedPositiveSubjectImageCollection.Select(x => x.Item2).ToList(),
                        NegativeImages = SelectedNegativeSubjectImageCollection.Select(x => x.Item2).ToList()
                    });
                    break;
            }
        }

        private void OnBackStepButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (BuilderStepType)
            {
                case VisualAlertBuilderStepType.AddPositiveImages:
                    BuilderStepType = VisualAlertBuilderStepType.NewSubject;
                    break;

                case VisualAlertBuilderStepType.AddNegativeImages:
                    BuilderStepType = VisualAlertBuilderStepType.AddPositiveImages;
                    break;

                case VisualAlertBuilderStepType.TrainModel:
                    BuilderStepType = VisualAlertBuilderStepType.AddNegativeImages;
                    break;
            }
        }

        private void OnPositiveImagesGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.positiveImagesGrid.SelectedItems != null)
            {
                int selectedCount = this.positiveImagesGrid.SelectedItems.Count;

                this.imageCount.Text = selectedCount.ToString();
                this.nextStepButton.IsEnabled = selectedCount >= MinPositiveImageCount;
            }
        }

        private void OnNegativeImagesGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.negativeImagesGrid.SelectedItems != null)
            {
                int selectedCount = this.negativeImagesGrid.SelectedItems.Count;

                this.imageCount.Text = selectedCount.ToString();
                this.nextStepButton.IsEnabled = selectedCount >= MinNegativeImageCount;
            }
        }
    }
}
