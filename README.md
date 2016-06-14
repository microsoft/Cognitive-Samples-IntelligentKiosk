# Intelligent Kiosk Sample
The Intelligent Kiosk Sample is a collection of demos showcasing workflows and experiences built on top of the Microsoft Cognitive Services. Most of the experiences are hands-free and autonomous, using the human faces in front of a web camera as the main form of input (thus the word "kiosk" in the name).

# Requirements
1. A Windows 10 computer (the sample is a UWP application)
2. A webcam, ideally top-mounted so you have a similar experience as looking at a mirror when interacting with the demos 
3. Visual Studio 2015
4. API Keys
  * Face and Emotion Keys from [Microsoft.com/cognitive](https://www.microsoft.com/cognitive-services)
  * Bing Search API Key from [Azure Market Place](https://azure.microsoft.com/en-us/marketplace/partners/bing/search/) (only needed if you will be using the search functionality)

# Running the sample
1. Open the solution in Visual Studio 2015 and hit F5
2. Enter your API Keys in the Settings page (they will be saved in the user profile). See [Settings](Documentation/AppSettings.md) for  more details on the available settings.
3. Explore one of the scenarios

# Scenarios

| Scenario                     | Overview | Features Covered  |
| ---------------------------- | -------- | ----------------  |
| [Automatic Photo Capture](Documentation/AutomaticPhotoCapture.md)      | An autonomous workflow for capturing photos when people approach a web camera and pose for a photo | Windows 10 Face Tracking; Age and gender prediction; Face identification |
| [Realtime Crowd Insights](Documentation/RealtimeCrowdInsights.md)      | A realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting | Windows 10 Face Tracking; Realtime sampling; Age, gender and emotion prediction; Face identification; Unique face counting |
| [Face API Playground](Documentation/FaceAPIPlayground.md)          | A playground for the Face APIs used for age and gender prediction, as well as face identification. | Windows 10 Face Tracking; Age and gender prediction; Face identification; Bing Image Search API; Bing AutoSuggestion API |
| [Emotion API Playground](Documentation/EmotionAPIPlayground.md)       | A playground for the Emotion APIs | Windows 10 Face Tracking; Emotion prediction; Bing Image Search API; Bing AutoSuggestion API |
| [Face Identification Setup](Documentation/FaceIdentificationSetup.md)    | Shows how to train the machine learning model behind the Face APIs to recognize specific people. | Face identification; Bing Image Search API; Bing AutoSuggestion API |

# Source Code Structure

* Kiosk/ServiceHelpers: A portable library that serves as a wrapper around the several services. 
 * One of the key things that the wrappers provide is the ability to detect when the API calls return a call-rate-exceeded error and automatically retry the call (after some delay). 
 * The ImageAnalyzer class is another key class here, responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services. 
 * Another example of functionality is the FaceListManager class, which is a wrapper on top of the FaceList APIs to make it easier to track unique faces. 

* Kiosk/Views: The root pages that represent the various views in the kiosk.

* Kiosk/Controls: The controls used by the various pages in the kiosk.

# Contributing
We welcome contributions and are always looking for input and suggestions. Feel free to file issues on the repo and we'll address them as we can or take matters into your hand and submit a pull request with your changes.

And if you have questions, feedback, or suggestions about Microsoft Cognitive Services, reach out to us directly on our [Cognitive Services UserVoice Forum](<https://cognitive.uservoice.com>).
