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
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Views
{
    [KioskExperience(Id = "FaceAPIExplorer",
        DisplayName = "Face API Explorer",
        Description = "See ages, genders, and landmarks from faces in an image",
        ImagePath = "ms-appx:/Assets/DemoGallery/Face API Explorer.jpg",
        ExperienceType = ExperienceType.Guided | ExperienceType.Business,
        TechnologiesUsed = TechnologyType.BingAutoSuggest | TechnologyType.BingImages | TechnologyType.Face | TechnologyType.Emotion,
        TechnologyArea = TechnologyAreaType.Vision,
        DateAdded = "2015/10/16")]
    public sealed partial class RecognitionPage : Page
    {
        public RecognitionPage()
        {
            this.InitializeComponent();

            this.imagePicker.SetSuggestedImageList(
                "https://intelligentkioskstore.blob.core.windows.net/faceapi-explorer/1.jpg",
                "https://intelligentkioskstore.blob.core.windows.net/faceapi-explorer/2.jpg",
                "https://intelligentkioskstore.blob.core.windows.net/faceapi-explorer/3.jpg",
                "https://intelligentkioskstore.blob.core.windows.net/faceapi-explorer/4.jpg",
                "https://intelligentkioskstore.blob.core.windows.net/faceapi-explorer/5.jpg"
            );
        }

        private void OnImageSearchCompleted(object sender, IEnumerable<ImageAnalyzer> args)
        {
            ImageAnalyzer image = args.First();
            image.ShowDialogOnFaceApiErrors = true;

            this.imageWithFacesControl.Visibility = Visibility.Visible;

            this.imageWithFacesControl.DataContext = image;
        }
    }
}
