using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using IntelligentKioskSample.Model;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using ServiceHelpers;

namespace IntelligentKioskSample.ViewModels
{
    public class GreetingKioskViewModel : INotifyPropertyChanged
    {
        private const string CongnitiveSubscriptionKey = "8bbdda653b824a4d9a2a93ced873e97e";
        private const string CognitiveEndpoint = "https://australiaeast.api.cognitive.microsoft.com/vision/v1.0";
        private const string StorageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=pactsa;AccountKey=kyJuQ0XxMLMqPZs29A6utilQLq8ozl8EMPuni0Fjcm4CoOMZ0oY/nGg8f4o1KDGd+fzQB5HqMUmNp4WsvkkvFA==;EndpointSuffix=core.windows.net";


        private readonly List<PactWorker> _allFiles;
        private PactWorker _worker;
        private readonly CloudStorageAccount _storageAccount;
        public int NewPersonIteration { get; private set; }
        public int ImageSaveCount { get; private set; }
        public bool SavedToQueue;

        private readonly VisionServiceClient _visionClient;
        private string _greetingText;
        private string _pactWorkerText;
        private string _metaText;
        private SolidColorBrush _greetingColour;
        private SolidColorBrush _symbolColour;
        private Symbol _symbol;


        public Symbol Symbol
        {
            get => _symbol;
            set => Set(ref _symbol, value);
        }

        public SolidColorBrush GreetingColour
        {
            get => _greetingColour;
            set => Set(ref _greetingColour, value);
        }

        public SolidColorBrush SymbolColour
        {
            get => _symbolColour;
            set => Set(ref _symbolColour, value);
        }

        public string GreetingText
        {
            get => _greetingText;
            set => Set(ref _greetingText, value);
        }

        public string PactWorkerText
        {
            get => _pactWorkerText;
            set => Set(ref _pactWorkerText, value);
        }

        public string MetaText
        {
            get => _metaText;
            set => Set(ref _metaText, value);
        }


        public int Authorization => _worker?.Authorization ?? -1;

        public string Role => _worker?.Role;

        public GreetingKioskViewModel()
        {
            //insert keys and storage account values 
            _visionClient = new VisionServiceClient(CongnitiveSubscriptionKey, CognitiveEndpoint);
            _storageAccount = CloudStorageAccount.Parse(StorageAccountConnectionString);

            //Generate dummy users 
            _allFiles = new WorkerDataHelper().Initialise();

            //RESET ITERATION
            NewPersonIteration = 0;

            GreetingText = "Step in front of the camera to start";
            PactWorkerText = _allFiles.Count.ToString();
            MetaText = _worker?.Name;

            Symbol = Symbol.Contact;
            GreetingColour = new SolidColorBrush(Colors.White);
            SymbolColour = new SolidColorBrush(Colors.White);
        }

        //puts block on seperate thread 
        public async Task ProcessCameraCapture(ImageAnalyzer e)
        {
            if (e == null)
            {
                UpdateUiForNoFacesDetected();
                return;
            }

            await e.DetectFacesAsync();

            if (e.DetectedFaces.Any())
            {
                //Identify who is in the frame
                await e.IdentifyFacesAsync();
                var metadata = await GetGreettingFromFacesAsync(e);

                //change to List and foreach 
                GreetingText = metadata[0];
                PactWorkerText = metadata[1];

                var tags = await TaggingAnalysisFunction(await e.GetImageStreamCallback());
                var safetyText = GetSafetyTextFromComputerVision(tags);
                MetaText = safetyText;
                bool isSafe;

                if (safetyText.Contains("UNDETECTED"))
                {
                    isSafe = false;

                    //this.MetaText = await Task.Run(() => CheckSafetyGearAsync(e.GetImageStreamCallback())); ;
                }
                else
                {
                    isSafe = true;
                }

                if (e.IdentifiedPersons.Any() && !metadata[2].Contains("User not authorized") && isSafe)
                {
                    //user is authenticated and safe 
                    GreetingColour = new SolidColorBrush(Colors.GreenYellow);
                    SymbolColour = new SolidColorBrush(Colors.GreenYellow);
                    Symbol = Symbol.Comment;
                }
                else if (metadata[2].Contains("User not authorized"))
                {
                    //user not authorized 
                    GreetingColour = new SolidColorBrush(Colors.Red);
                    SymbolColour = new SolidColorBrush(Colors.Red);
                    Symbol = Symbol.View;
                }
                else
                {
                    //user has no safety gear
                    GreetingColour = new SolidColorBrush(Colors.Orange);
                    SymbolColour = new SolidColorBrush(Colors.Orange);
                    Symbol = Symbol.Remove;
                    await SaveToQueueAsync(metadata[3] +
                                           " has attempted to enter the workroom but is not wearing their safety gear: " +
                                           DateTime.Now.ToString("dd yyyy HHmm"));
                }
            }
            else
            {
                UpdateUiForNoFacesDetected();
            }
        }

        private string GetSafetyTextFromComputerVision(IReadOnlyCollection<Tag> tags)
        {
            var tagstring = "Safety Hat = UNDETECTED \r\n";
            var vestString = "Safety vest = UNDETECTED \r\n";
            var debugString = "";
            foreach (var tag in tags)
            {
                debugString += tag.Name + " ";
            }

            if (tags.Any(t => t.Name == "hat") || tags.Any(t => t.Name == "helmet") ||
                tags.Any(t => t.Name == "headdress"))
            {
                tagstring = "Safety Hat = CHECK \r\n";
            }

            if (tags.Any(t => t.Name == "orange") || tags.Any(t => t.Name == "yellow"))
            {
                vestString = "Safety Vest = CHECK \r\n";
            }

            return tagstring + vestString + debugString;
        }

        private async Task<List<Tag>> TaggingAnalysisFunction(Stream imageStream)
        {
            // Submit image to API.
            try
            {
                var analysisResult = await _visionClient.GetTagsAsync(imageStream);
                return analysisResult.Tags.ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Enumerable.Empty<Tag>().ToList();
        }

        private async Task SaveImage(Stream imageStream, string containertxt, string message)
        {
            var blobClient = _storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            var container = blobClient.GetContainerReference(containertxt);

            // Create the container if it doesn't already exist.
            if (!await container.ExistsAsync())
            {
                await container.CreateIfNotExistsAsync();

                //if it's a new container - send notification to Admin portal 
                await SaveToQueueAsync(message);
            }

            // Retrieve reference to a blob named "myblob".
            var blockBlob = container.GetBlockBlobReference("new-person-" + DateTime.Now.ToString("h:mm:ss tt"));

            // Create or overwrite the "myblob" blob with contents from a local file.
            await blockBlob.UploadFromStreamAsync(imageStream);
        }

        private async Task SaveToQueueAsync(string message)
        {
            // Create the queue client.
            var queueClient = _storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            var queue = queueClient.GetQueueReference("client-alerts");


            // Create the queue if it doesn't already exist.
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var queuemessage = new CloudQueueMessage(message);

            await queue.AddMessageAsync(queuemessage);
        }

        private async Task<string[]> GetGreettingFromFacesAsync(ImageAnalyzer img)
        {
            var strings = new string[4];


            if (img.IdentifiedPersons.Any())
            {
                var names = img.IdentifiedPersons.Count() > 1
                    ? string.Join(", ", img.IdentifiedPersons.Select(p => p.Person.Name))
                    : img.IdentifiedPersons.First().Person.Name;

                var message = "";
                var message2 = "";
                foreach (var worker in _allFiles)
                {
                    if (!img.IdentifiedPersons.Any(x => x.Person.Name.Equals(worker.Name)))
                        continue;

                    strings[3] = worker.Name;
                    _worker = worker;
                    OnPropertyChanged(nameof(Authorization));
                    OnPropertyChanged(nameof(Role));

                    message += $"Role: {worker.Role}{Environment.NewLine}Mobile No: {worker.MobileNo}{Environment.NewLine}";

                    switch (worker.Authorization)
                    {
                        case 2:
                            SavedToQueue = false;
                            message2 += "Auth Level: Room Access" + "\r\nObjects: unauthorized to use objects";
                            break;
                        case 3:
                            SavedToQueue = false;
                            message2 += "Auth Level: Full Access" + "\r\nObjects: " + worker.Objects;
                            break;
                        default:
                            message2 += "User not authorized to be in room";
                            if (!SavedToQueue)
                            {
                                SavedToQueue = true;
                                await SaveToQueueAsync(
                                    worker.Name +
                                    " has attempted to enter workroom but is not authorized. Call now " +
                                    worker.MobileNo);
                            }
                            break;
                    }
                }
                strings[1] = message;
                strings[2] = message2;

                if (img.DetectedFaces.Count() > img.IdentifiedPersons.Count())
                {
                    strings[0] = string.Format("Welcome, {0} and company!", names);

                    return strings;
                }
                strings[0] = string.Format("Welcome, {0}", names);

                return strings;
            }
            if (img.DetectedFaces.Count() > 1)
            {
                strings[0] =
                    "Unrecognised groupd of people. Please enter the frame one by one and wait to be onboarded by the floor admin";
                return strings;
            }
            if (ImageSaveCount < 5)
            {
                try
                {
                    //checks for new person every minute 
                    await SaveImage(await img.GetImageStreamCallback(),
                        "new-person-detected-" + DateTime.Now.ToString("dd-yyyy-HHmm"),
                        "New Person Detected " + DateTime.Now.ToString("dd yyyy HHmm"));
                    ImageSaveCount++;
                }
                catch (Exception)
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

        private void UpdateUiForNoFacesDetected()
        {
            GreetingText = "Step in front of the camera to start";
            GreetingColour = new SolidColorBrush(Colors.White);
            SymbolColour = new SolidColorBrush(Colors.White);
            PactWorkerText = "";
            MetaText = "";
            Symbol = Symbol.Contact;
        }

        private ICommand _buttonCommand;

        public ICommand ButtonCommand
        {
            get
            {
                return _buttonCommand ??
                    (_buttonCommand = new CommandHandler(() => 
                        {
                            ImageSaveCount = 0;
                            NewPersonIteration++;
                        }, true));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool Set<T>(ref T field, T newValue = default(T), [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }
            
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        public class CommandHandler : ICommand
        {
            private readonly Action _action;
            private readonly bool _canExecute;
            public CommandHandler(Action action, bool canExecute)
            {
                _action = action;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                _action();
            }
        }
    }
}