# Intelligent Kiosk Sample
The Intelligent Kiosk Sample is a collection of demos showcasing workflows and experiences built on top of the Microsoft Cognitive Services. Most of the experiences are hands-free and autonomous, using the human faces in front of a web camera as the main form of input (thus the word "kiosk" in the name).

# Requirements
1. Windows 10 version 1809 or later (the sample is a UWP application).
2. A webcam, ideally top-mounted so you have a similar experience as looking at a mirror when interacting with the demos 
3. [Visual Studio 2019](https://www.visualstudio.com) with the Universal Windows Platform workload enabled
4. API Keys
  * Between the various demos, you might need API keys for Face, Computer Vision, Custom Vision, Text Analytics, Bing Search and Bing AutoSuggestion. Visit  [Microsoft.com/cognitive](https://www.microsoft.com/cognitive-services) if you need keys. Please notice that some of the demos perform tasks in realtime, so if you are going to use some of those demos you will need to upgrade to a paid key so it has enough throughput and won't be throttled. 

# Running the sample
1. Open the solution in Visual Studio 2019
2. Right click on the IntelligentKioskSample project and set it as the StartUp Project 
3. Run the solution (F5)
4. Enter your API Keys in the Settings page (they will be saved in the user profile). See [Settings](Documentation/AppSettings.md) for  more details on the available settings.
  * If you prefer using command line to obtain the keys, you can follow the steps in the in the [Key Acquisition Steps](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/KeyAcquisitionScript.md) file on how to do that through azure cli commands from bash / git bash 
5. Explore one of the scenarios

If you have build issues on first launch, try to reload your project. Also make sure all NuGet packages are installed correctly.

# Scenarios

| Scenario                     | Overview | Features Covered  |
| ---------------------------- | -------- | ----------------  |
| [Anomaly Detector](Documentation/AnomalyDetectorDemo.md)      | Showcases anomaly detection with three pre-selected data streams, as well as a Live Sound scenario that samples the audio level of your microphone as the input data stream. | Anomaly Detector API |
| [Bing News Analytics](Documentation/BingNewsAnalytics.md)      | Connecting the Bing News APIs with the Text Analytics APIs to create a visualization of the news based on their sentiment and most common topics | Bing News API, Bing AutoSuggestion API, Text Sentiment and Text KeyPhrase Extraction |
| [Bing Visual Search](Documentation/BingVisualSearch.md)      | Shows how to use the power of Bing Image Insights to perform visual search and find visually similar images, products or celebrities. | Bing Search API;  Bing AutoSuggestion API |
| [Caption Bot](Documentation/CaptionBot.md)      | Get a description of the content of a webcam image. | Computer Vision API; Windows 10 Face Tracking; |
| [Custom Vision Explorer](Documentation/CustomVisionExplorer.md)      | Shows how to use the Custom Vision Service to create a custom image classifier or object detector and score images against it. | Custom Vision API, Bing Image Search API;  Bing AutoSuggestion API |
| [Custom Vision Setup](Documentation/CustomVisionSetup.md)      | Shows how to train a machine learning model using the Custom Vision Service. | Custom Vision API, Bing Image Search API;  Bing AutoSuggestion API |
| [Digital Asset Management](Documentation/DigitalAssetManagement.md)      | Showcases how Computer Vision can add a layer of insights to a collection of images | Face API, Emotion API, Computer Vision API, Custom Vision API  |
| [Face API Explorer](Documentation/FaceAPIExplorer.md)          | A playground for the Face APIs used for extracting face-related attributes, such as head pose, gender, age, emotion, facial hair, and glasses, as well as face identification. | Windows 10 Face Tracking; Face API; Bing Image Search API; Bing AutoSuggestion API |
| [Face Identification Setup](Documentation/FaceIdentificationSetup.md)    | Shows how to train the machine learning model behind the Face APIs to recognize specific people. | Face identification; Bing Search API; Bing AutoSuggestion API |
| [Form Recognizer](Documentation/FormRecognizer.md)    | Explore using machine learning models to extract key/value pairs and tables from scanned forms. This can be used to accelerate business processes by automatically converting scanned forms to usable data. | Form Recognizer API |
| [Greeting Kiosk](Documentation/GreetingKiosk.md)          | A simple hands-free and real-time workflow for recognizing when a known person approaches the camera | Windows 10 Face Tracking; Realtime sampling; Face identification |
| [How Old](Documentation/HowOld.md)          | An autonomous workflow for capturing photos when people approach a web camera and pose for a photo. | Windows 10 Face Tracking; Age and gender prediction; Face identification |
| [Insurance Claim Automation](Documentation/InsuranceClaimAutomation.md)      | An example of Robotic Process Automation (RPA), leveraging Custom Vision and Form Recognizer to illustrate automating the validation of insurance claims. | Custom Vision API, Form Recognizer API, Bing Image Search API, Bing AutoSuggestion API |
| [Mall Kiosk](Documentation/MallKiosk.md)      | An example of a Mall kiosk that makes product recommendations based on the people in front of the camera and analyzes their reaction to it  | Windows 10 Face Tracking; Age and gender prediction; Realtime sampling of Emotion; Face identification; Windows 10 Speech-To-Text; Text Sentiment Analysis |
| [Neural Text to Speech](Documentation/NeuralTextToSpeech.md)      | Shows how to use the Text to Speech API to convert text to lifelike speech. | Text to Speech API |
| [Realtime Crowd Insights](Documentation/RealtimeCrowdInsights.md)      | A realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting | Windows 10 Face Tracking; Realtime sampling; Age, gender and emotion prediction; Face identification; Unique face counting |
| [Realtime Driver Monitoring](Documentation/RealtimeDriverMonitoring.md)      | A futuristic scenario where a dashboard camera in a car could be used to monitor the driver and determine when the driver is looking away from the road ahead, sleeping, yawning or doing certain activities, such as using the phone or eating a banana. | Windows 10 Face Tracking; Realtime sampling; Facial Landmarks; Head Pose; Face identification; Unique face tracking; Image Captioning |
| [Realtime Image Classification](Documentation/RealtimeImageClassification.md)      | A realtime workflow for processing frames from a web camera to show classification models exported by Microsoft Custom Vision Service. | Custom Vision API; Windows Machine Learning |
| [Realtime Object Detection](Documentation/RealtimeObjectDetection.md)      | A realtime workflow for processing frames from a web camera to show object detection models exported by Microsoft Custom Vision Service. | Custom Vision API; Windows Machine Learning |
| [Realtime Video Insights](Documentation/RealtimeVideoInsights.md)      | A realtime workflow for processing frames from a video file  to derive realtime insights such as demographics, emotion, unique face counting and face identification | Realtime sampling; Age, gender and emotion prediction; Face identification; Unique face counting; Celebrity detection |
| [Speaker Recognition Explorer](Documentation/SpeakerRecognitionExplorer.md)      | Shows how to identify speakers by their unique voice characteristics using voice biometry. | Speech to Text API |
| [Speech to Text Explorer](Documentation/SpeechToTextExplorer.md)      | Shows how to use the Speech to Text API to convert spoken audio to text. | Speech to Text API |
| [Text Analytics Explorer](Documentation/TextAnalyticsExplorer.md)      | Shows how the Text Analytics API extracts insights from text. | Text Analytics API |
| [Translator API Explorer](Documentation/TranslatorExplorer.md)      | Shows how to use the Translator Text API for text translation. | Translator Text API |
| [Vision API Explorer](Documentation/VisionAPIExplorer.md)          | A playground for some of the offerings in the Computer Vision APIs. It shows how to extract tags, faces, description of the photo, celebrities, OCR results and color analysis. | Windows 10 Face Tracking; Computer Vision API; Bing Image Search API; Bing AutoSuggestion API |
| [Visual Alert Builder](Documentation/VisualAlertBuilder.md)          | Shows how to use Custom Vision Service to quickly create an image classifier to detect visual states, and how to process frames from a web camera to alert when those states are detected. | Custom Vision API; Windows Machine Learning |

# Source Code Structure

1 Kiosk/ServiceHelpers: A portable library that serves as a wrapper around the several services. 
  * One of the key things that the wrappers provide is the ability to detect when the API calls return a call-rate-exceeded error and automatically retry the call (after some delay). 
  * The ImageAnalyzer class is another key class here, responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services. 
  * Another example of functionality is the FaceListManager class, which is a wrapper on top of the FaceList APIs to make it easier to track unique faces. 

2 Kiosk: The main version of the kiosk
  * Kiosk/Views: The root pages that represent the various views in the kiosk.
  * Kiosk/Controls: The controls used by the various pages in the kiosk.

# Contributing
We welcome contributions. Feel free to file issues & pull requests on the repo and we'll address them as we can.

For questions, feedback, or suggestions about Microsoft Cognitive Services, reach out to us directly on our [Cognitive Services UserVoice Forum](<https://cognitive.uservoice.com>).

# License
All Microsoft Cognitive Services SDKs and samples are licensed with the MIT License. For more details, see
[LICENSE](</LICENSE.md>).

# Developer Code of Conduct
The image, voice, video or text understanding capabilities of the Intelligent Kiosk Sample uses Microsoft Cognitive Services. Microsoft will receive the images, audio, video, and other data that you upload (via this app) for service improvement purposes. To report abuse of the Microsoft Cognitive Services to Microsoft, please visit the Microsoft Cognitive Services website at https://www.microsoft.com/cognitive-services, and use the “Report Abuse” link at the bottom of the page to contact Microsoft. For more information about Microsoft privacy policies please see their privacy statement here: https://go.microsoft.com/fwlink/?LinkId=521839.

Developers using Cognitive Services, including this sample, are expected to follow the “Developer Code of Conduct for Microsoft Cognitive Services”, found at [http://go.microsoft.com/fwlink/?LinkId=698895](http://go.microsoft.com/fwlink/?LinkId=698895).
