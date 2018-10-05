# Intelligent Kiosk Sample
The Intelligent Kiosk Sample is a collection of demos showcasing workflows and experiences built on top of the Microsoft Cognitive Services. Most of the experiences are hands-free and autonomous, using the human faces in front of a web camera as the main form of input (thus the word "kiosk" in the name).

# Requirements
1. Windows 10 version 1803 or later (the sample is a UWP application). For a Windows 10 IoT deployment, see details later in this page. 
2. A webcam, ideally top-mounted so you have a similar experience as looking at a mirror when interacting with the demos 
3. [Visual Studio 2017](https://www.visualstudio.com) with the Universal Windows Platform workload enabled
4. API Keys
  * Between the various demos, you might need API keys for Face, Computer Vision, Custom Vision, Text Analytics, Bing Search and Bing AutoSuggestion. Visit  [Microsoft.com/cognitive](https://www.microsoft.com/cognitive-services) if you need keys. Please notice that some of the demos perform tasks in realtime, so if you are going to use some of those demos you will need to upgrade to a paid key so it has enough throughput and won't be throttled. 

# Running the sample
1. Open the solution in Visual Studio 2017
2. Right click on the IntelligentKioskSample project and set it as the StartUp Project 
3. Run the solution (F5)
4. Enter your API Keys in the Settings page (they will be saved in the user profile). See [Settings](Documentation/AppSettings.md) for  more details on the available settings.
  * If you prefer using command line to obtain the keys, you can follow the steps in the in the [Key Acquisition Steps](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/KeyAcquisitionScript.md) file on how to do that through azure cli commands from bash / git bash 
5. Explore one of the scenarios

# Scenarios

| Scenario                     | Overview | Features Covered  |
| ---------------------------- | -------- | ----------------  |
| [Automatic Photo Capture](Documentation/AutomaticPhotoCapture.md)      | An autonomous workflow for capturing photos when people approach a web camera and pose for a photo | Windows 10 Face Tracking; Age and gender prediction; Face identification |
| [Bing News Analytics](Documentation/BingNewsAnalytics.md)      | Connecting the Bing News APIs with the Text Analytics APIs to create a visualization of the news based on their sentiment and most common topics | Bing News API, Bing AutoSuggestion API, Text Sentiment and Text KeyPhrase Extraction |
| [Bing Visual Search](Documentation/BingVisualSearch.md)      | Shows how to use the power of Bing Image Insights to perform visual search and find visually similar images, products or celebrities. | Bing Search API;  Bing AutoSuggestion API |
| [Custom Vision Explorer](Documentation/CustomVisionExplorer.md)      | Shows how to use the Custom Vision Service to create a classifier and score images against it. | Custom Vision API, Bing Image Search API;  Bing AutoSuggestion API |
| [Emotion API Explorer](Documentation/EmotionAPIPlayground.md)       | A playground for the Emotion APIs | Windows 10 Face Tracking; Emotion prediction; Bing Search API; Bing AutoSuggestion API |
| [Face API Explorer](Documentation/FaceAPIPlayground.md)          | A playground for the Face APIs used for age and gender prediction, as well as face identification | Windows 10 Face Tracking; Age and gender prediction; Face identification; Facial landmarks; Bing Image Search API; Bing AutoSuggestion API |
| [Face Identification Setup](Documentation/FaceIdentificationSetup.md)    | Shows how to train the machine learning model behind the Face APIs to recognize specific people. | Face identification; Bing Search API; Bing AutoSuggestion API |
| [Greeting Kiosk](Documentation/GreetingKiosk.md)          | A simple hands-free and real-time workflow for recognizing when a known person approaches the camera | Windows 10 Face Tracking; Realtime sampling; Face identification |
| [Image Collection Insights](Documentation/ImageCollectionInsights.md)          | An example of using Cognitive Services to add a layer of intelligence on top of a collection of photos | Face API, Emotion API, Computer Vision API  |
| [Mall Kiosk](Documentation/MallKiosk.md)      | An example of a Mall kiosk that makes product recommendations based on the people in front of the camera and analyzes their reaction to it  | Windows 10 Face Tracking; Age and gender prediction; Realtime sampling of Emotion; Face identification; Windows 10 Speech-To-Text; Text Sentiment Analysis |
| [Realtime Crowd Insights](Documentation/RealtimeCrowdInsights.md)      | A realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting | Windows 10 Face Tracking; Realtime sampling; Age, gender and emotion prediction; Face identification; Unique face counting |
| [Realtime Driver Monitoring](Documentation/RealtimeDriverMonitoring.md)      | A futuristic scenario where a dashboard camera in a car could be used to monitor the driver and determine when the driver is looking away from the road ahead, sleeping, yawning or doing certain activities, such as using the phone or eating a banana. | Windows 10 Face Tracking; Realtime sampling; Facial Landmarks; Head Pose; Face identification; Unique face tracking; Image Captioning |
| [Realtime Video Insights](Documentation/RealtimeVideoInsights.md)      | A realtime workflow for processing frames from a video file  to derive realtime insights such as demographics, emotion, unique face counting and face identification | Realtime sampling; Age, gender and emotion prediction; Face identification; Unique face counting; Celebrity detection |
| [Vision API Explorer](Documentation/VisionAPIExplorer.md)          | A playground for some of the offerings in the Computer Vision APIs. It shows how to extract tags, faces, description of the photo, celebrities, OCR results and color analysis. | Windows 10 Face Tracking; Computer Vision API; Bing Image Search API; Bing AutoSuggestion API |

# Raspberry Pi + Sense HAT Kiosk
A trimmed down version of the kiosk is also available in the RaspberryPiKiosk folder. This version is similar to the main version and references the same ServicesHelper library. It has less demos and does a couple of tweaks to improve performance (it disables the Windows based face tracking code and makes the camera preview size very small). 

This version also has a nice integration with the Sense HAT add-on for the Pi, making use of it to display output and switch between demos through the built-in joystick. The Sense HAT integration uses the great library and sample code provided by [Mattias Larsson](https://www.hackster.io/laserbrain/windows-iot-sense-hat-10cac2).

Navigating the kiosk via the Sense HAT joystick (assuming the joystick is on the lower/left corner as you look at the LEDs):

* Left: Go to the demo launcher (the screen will keep scrolling the word "Launcher" while in the Launcher screen)
* Up/Down while in the Launcher screen: Move different demos into focus (the name of the current demo in focus will keep scrolling)
* Enter while the Launcher is scrolling the name of any particular demo: Launches that demo

Demos in the Pi version:

| Scenario                     | Overview |
| ---------------------------- | -------- |
| Greetings Kiosk          | Samples the camera at 1fps and uses the Sense HAT to scroll the name and age of the main face in front of the camera (or gender and age if the person is not recognized). |
| Realtime Crowd Insights | Similar to the same demo in the main Kiosk, except that it also uses the Sense HAT to communicate the number of man and women (each unique face is a mapped to a LED, with the color indicating the gender). |
| Realtime Driver Monitoring | Similar to the same demo in the main Kiosk, except that it also uses the Sense HAT to indicate the detection of any of the dangerous situations by turning all the LEDs red. |
| Emotion Meter  | A simple emotion meter, with each column of the Sense HAT 8x8 LED matrix indicating the level of each of the 8 emotions in the primary face in front of the camera. |

# Source Code Structure

1 Kiosk/ServiceHelpers: A portable library that serves as a wrapper around the several services. 
  * One of the key things that the wrappers provide is the ability to detect when the API calls return a call-rate-exceeded error and automatically retry the call (after some delay). 
  * The ImageAnalyzer class is another key class here, responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services. 
  * Another example of functionality is the FaceListManager class, which is a wrapper on top of the FaceList APIs to make it easier to track unique faces. 

2 Kiosk: The main version of the kiosk
  * Kiosk/Views: The root pages that represent the various views in the kiosk.
  * Kiosk/Controls: The controls used by the various pages in the kiosk.

3 RaspberryPiKiosk: The trimmed down version fo the main kiosk

# Contributing
We welcome contributions. Feel free to file issues & pull requests on the repo and we'll address them as we can.

For questions, feedback, or suggestions about Microsoft Cognitive Services, reach out to us directly on our [Cognitive Services UserVoice Forum](<https://cognitive.uservoice.com>).

# License
All Microsoft Cognitive Services SDKs and samples are licensed with the MIT License. For more details, see
[LICENSE](</LICENSE.md>).

# Developer Code of Conduct
The image, voice, video or text understanding capabilities of the Intelligent Kiosk Sample uses Microsoft Cognitive Services. Microsoft will receive the images, audio, video, and other data that you upload (via this app) for service improvement purposes. To report abuse of the Microsoft Cognitive Services to Microsoft, please visit the Microsoft Cognitive Services website at https://www.microsoft.com/cognitive-services, and use the “Report Abuse” link at the bottom of the page to contact Microsoft. For more information about Microsoft privacy policies please see their privacy statement here: https://go.microsoft.com/fwlink/?LinkId=521839.

Developers using Cognitive Services, including this sample, are expected to follow the “Developer Code of Conduct for Microsoft Cognitive Services”, found at [http://go.microsoft.com/fwlink/?LinkId=698895](http://go.microsoft.com/fwlink/?LinkId=698895).
