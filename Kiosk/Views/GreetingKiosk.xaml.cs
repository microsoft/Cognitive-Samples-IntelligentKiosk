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
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.WindowsAzure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Data.SqlClient;
using IntelligentKioskSample.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace IntelligentKioskSample.Views
{

    [KioskExperience(Title = "Greeting Kiosk", ImagePath = "ms-appx:/Assets/GreetingKiosk.jpg", ExperienceType = ExperienceType.Kiosk)]
    public sealed partial class GreetingKiosk : Page
    {
        private Task processingLoopTask;
        private bool isProcessingLoopInProgress;
        private bool isProcessingPhoto;
        private List<PactWorker> allFiles;
        private PactWorker Worker;
        public CloudStorageAccount storageAccount;
        public int NewPersonIteration { get; private set; }
        public int imageSaveCount { get; private set; }
        public bool savedToQueue = false;

        private VisionServiceClient _visionClient = null;

        public GreetingKiosk()
        {
            //insert keys and storage account values 
            _visionClient = new VisionServiceClient("<YOURKEYHERE>", "https://southeastasia.api.cognitive.microsoft.com/vision/v1.0");
            storageAccount = CloudStorageAccount.Parse("<STORAGEACCOUNT>");
            
            //Generate dummy users 
            allFiles = new WorkerDataHelper().Initialise();

            //RESET ITERATION
            NewPersonIteration = 0;

            this.InitializeComponent();
            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.FilterOutSmallFaces = true;
            this.cameraControl.HideCameraControls();
            this.cameraControl.CameraAspectRatioChanged += CameraControl_CameraAspectRatioChanged;
        }


        private void CameraControl_CameraAspectRatioChanged(object sender, EventArgs e)
        {
            this.UpdateCameraHostSize();
        }

        private void StartProcessingLoop()
        {
            this.isProcessingLoopInProgress = true;

            if (this.processingLoopTask == null || this.processingLoopTask.Status != TaskStatus.Running)
            {
                this.processingLoopTask = Task.Run(() => this.ProcessingLoop());
            }
        }


        private async void ProcessingLoop()
        {
            while (this.isProcessingLoopInProgress)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (!this.isProcessingPhoto)
                    {
                        this.isProcessingPhoto = true;
                        if (this.cameraControl.NumFacesOnLastFrame == 0)
                        {
                            await this.ProcessCameraCapture(null);
                        }
                        else
                        {
                            await this.ProcessCameraCapture(await this.cameraControl.CaptureFrameAsync());
                        }
                    }
                });

                //simplify timing 
                //add counter 
                await Task.Delay(this.cameraControl.NumFacesOnLastFrame == 0 ? 100 : 1000);
            }
        }

        private async void CurrentWindowActivationStateChanged(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if ((e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated ||
                e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.PointerActivated) &&
                this.cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Shutdown)
            {
                // When our Window loses focus due to user interaction Windows shuts it down, so we 
                // detect here when the window regains focus and trigger a restart of the camera.
                await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
            }
        }
        //puts block on seperate thread 
        private async Task ProcessCameraCapture(ImageAnalyzer e)
        {
            if (e == null)
            {
                this.UpdateUIForNoFacesDetected();
                this.isProcessingPhoto = false;
                return;
            }

            DateTime start = DateTime.Now;

            await e.DetectFacesAsync();

            if (e.DetectedFaces.Any())
            {
                //Identify who is in the frame
                await e.IdentifyFacesAsync();
                string[] metadata = this.GetGreettingFromFaces(e);

                //change to List and foreach 
                this.greetingTextBlock.Text = metadata[0];
                this.PactWorkerTextBlock.Text = metadata[1];

                List<Tag> tags = await TaggingAnalysisFunction(e.GetImageStreamCallback());
                string SafetyText = GetSafetyTextFromComputerVision(tags);
                this.MetaTextBlock.Text = SafetyText;
                bool isSafe;

                if (SafetyText.Contains("UNDETECTED"))
                {
                    isSafe = false;
                   
                    //this.MetaTextBlock.Text = await Task.Run(() => CheckSafetyGearAsync(e.GetImageStreamCallback())); ;
                }
                else
                {
                    isSafe = true;
                }

                if (e.IdentifiedPersons.Any() && !metadata[2].Contains("User not authorized") && isSafe)
                {
                    //user is authenticated and safe 
                    this.greetingTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.GreenYellow);
                    this.greetingSymbol.Foreground = new SolidColorBrush(Windows.UI.Colors.GreenYellow);
                    this.greetingSymbol.Symbol = Symbol.Comment;
                }
                else if (metadata[2].Contains("User not authorized"))
                {
                    //user not authorized 
                    this.greetingTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                    this.greetingSymbol.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                    this.greetingSymbol.Symbol = Symbol.View;
                }
                else
                {
                    //user has no safety gear
                    this.greetingTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Orange);
                    this.greetingSymbol.Foreground = new SolidColorBrush(Windows.UI.Colors.Orange);
                    this.greetingSymbol.Symbol = Symbol.Remove;
                    SaveToQueueAsync(metadata[3] + " has attempted to enter the workroom but is not wearing their safety gear: " + DateTime.Now.ToString("dd yyyy HH:mm"));
                }
            }
            else
            {
                this.UpdateUIForNoFacesDetected();
            }

            TimeSpan latency = DateTime.Now - start;
            this.faceLantencyDebugText.Text = string.Format("Face API latency: {0}ms", (int)latency.TotalMilliseconds);

            this.isProcessingPhoto = false;
        }

        private string GetSafetyTextFromComputerVision(List<Tag> tags)
        {
           
            string tagstring = "Safety Hat = UNDETECTED \r\n";
            string vestString = "Safety vest = UNDETECTED \r\n";
            string debugString = "";
            foreach (Tag tag in tags)
            {
                debugString += tag.Name + " ";
            }

            if (tags.Any(t => t.Name == "hat") || tags.Any(t => t.Name == "helmet") || tags.Any(t => t.Name == "headdress"))
            {
                tagstring = "Safety Hat = CHECK \r\n";
            }

            if (tags.Any(t => t.Name == "orange") || tags.Any(t => t.Name == "yellow"))
            {
                vestString = "Safety Vest = CHECK \r\n";
            }

            return tagstring + vestString + debugString;
        }

        private async Task<List<Tag>> TaggingAnalysisFunction(Task<Stream> task)
        {
            // Submit image to API. 
            var analysis = await _visionClient.GetTagsAsync(task.Result);

            List<Tag> tags = new List<Tag>();
            foreach (Tag tag in analysis.Tags)
            {
                tags.Add(tag);
            }

            // Output. 
            return tags;
        }



        private async void  SaveImage(Task<Stream> task, String containertxt, String message)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(containertxt);

            // Create the container if it doesn't already exist.
            if (!await container.ExistsAsync())
            {
                await container.CreateIfNotExistsAsync();

                //if it's a new container - send notification to Admin portal 
                SaveToQueueAsync(message);
                
            }

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("new-person-"+DateTime.Now.ToString("h:mm:ss tt"));

            // Create or overwrite the "myblob" blob with contents from a local file.
            await blockBlob.UploadFromStreamAsync(task.Result);
        }

        private async Task SaveToQueueAsync( String message)
        {
            // Create the queue client.
            var queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            var queue = queueClient.GetQueueReference("client-alerts");

           
     
            // Create the queue if it doesn't already exist.
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var queuemessage = new CloudQueueMessage(message);
            
            await queue.AddMessageAsync(queuemessage);
        }

        private string[] GetGreettingFromFaces(ImageAnalyzer img)
        {
            string[] strings = new string[4];
           
            
            if (img.IdentifiedPersons.Any())
            {
                string names = img.IdentifiedPersons.Count() > 1 ? string.Join(", ", img.IdentifiedPersons.Select(p => p.Person.Name)) : img.IdentifiedPersons.First().Person.Name;

                string message = "";
                string message2 = "";
                foreach (PactWorker worker in allFiles)
                {
                    if (worker.Name.Equals(img.IdentifiedPersons.First().Person.Name))
                    {
                        strings[3] = worker.Name;
                        Worker = worker;

                        
                        message = "Role: " + worker.Role
                            + "\r\nMobile No:" + worker.MobileNo;

                        if (worker.Authorization == 2)
                        {
                            savedToQueue = false;
                            message2 = "Auth Level: Room Access" + "\r\nObjects: unauthorized to use objects";
                        }
                        else if (worker.Authorization == 3)
                        {
                            savedToQueue = false;
                            message2 = "Auth Level: Full Access" + "\r\nObjects: " + worker.Objects;
                        }
                        else
                        {
                            message2 = "User not authorized to be in room";
                            if (!savedToQueue)
                            {
                                savedToQueue = true;
                                SaveToQueueAsync(worker.Name + " has attempted to enter workroom but is not authorized. Call now " + worker.MobileNo);
                            }

                        }
                    }
                }
                strings[1] = message;
                strings[2] = message2;

                if (img.DetectedFaces.Count() > img.IdentifiedPersons.Count())
                {
                    strings[0] = string.Format("Welcome, {0} and company!", names);

                    return strings;
                }
                else
                {

                    strings[0] = string.Format("Welcome, {0}", names);

                    return strings;
                }
            }
            else
            {           

                if (img.DetectedFaces.Count() > 1)
                {
                    strings[0] = "Unrecognised groupd of people. Please enter the frame one by one and wait to be onboarded by the floor admin";
                    return strings;
                }
                else
                {
                    if (imageSaveCount < 5)
                    {
                        try
                        {
                            //checks for new person every minute 
                            SaveImage(img.GetImageStreamCallback(), "new-person-detected-" + DateTime.Now.ToString("dd-yyyy-HHmm"), "New Person Detected " + DateTime.Now.ToString("dd yyyy HH:mm"));
                            imageSaveCount++;
                        }
                        catch(Exception)
                        {
                            // error handling
                        }
                        
                    }
                    strings[0] = "Unrecognised person. Please wait to be onboarded by the floor admin.";
                    strings[1] = "";
                    strings[2] = "";
                    strings[3] = "";
                    return strings;
                }

                
            }
        }


        private void UpdateUIForNoFacesDetected()
        {
            this.greetingTextBlock.Text = "Step in front of the camera to start";
            this.greetingTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            this.greetingSymbol.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            this.PactWorkerTextBlock.Text = "";
            this.MetaTextBlock.Text = "";
            this.greetingSymbol.Symbol = Symbol.Contact;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            EnterKioskMode();

            if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey))
            {
                await new MessageDialog("Missing Face API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            }
            else
            {
                await this.cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
                this.StartProcessingLoop();
            }

            base.OnNavigatedTo(e);
        }

        private void EnterKioskMode()
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            this.isProcessingLoopInProgress = false;
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
            this.cameraControl.CameraAspectRatioChanged -= CameraControl_CameraAspectRatioChanged;

            await this.cameraControl.StopStreamAsync();
            base.OnNavigatingFrom(e);
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateCameraHostSize();
        }

        private void UpdateCameraHostSize()
        {
            this.cameraHostGrid.Width = this.cameraHostGrid.ActualHeight * (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }

        private async void buttonClick(object sender, RoutedEventArgs e)
        {
            imageSaveCount = 0;
            NewPersonIteration++;
        }
    }
}