# Intelligent Kiosk Sample
The Intelligent Kiosk Sample is a collection of demos showcasing workflows and experiences built on top of the Microsoft Cognitive Services. Most of the experiences are hands-free and autonomous, using the human faces in front of a web camera as the main form of input (thus the word "kiosk" in the name).

# Requirements
1. A Windows 10 computer (the sample is a UWP application)
2. A webcam, ideally top-mounted so you have a similar experience as looking at a mirror when interacting with the demos 
3. Visual Studio 2015
4. API Keys
  * You will need Face, Emotion, Text Analytics, Bing Search and Bing AutoSuggestion API Keys. Visit  [Microsoft.com/cognitive](https://www.microsoft.com/cognitive-services) if you need keys.

# Running the sample
1. Open the solution in Visual Studio 2015 and hit F5
2. Enter your API Keys in the Settings page (they will be saved in the user profile). See [Settings](Documentation/AppSettings.md) for  more details on the available settings.
3. Explore one of the scenarios

# Scenarios

| Scenario                     | Overview | Features Covered  |
| ---------------------------- | -------- | ----------------  |
| [Automatic Photo Capture](Documentation/AutomaticPhotoCapture.md)      | An autonomous workflow for capturing photos when people approach a web camera and pose for a photo | Windows 10 Face Tracking; Age and gender prediction; Face identification |
| [Bing News Analytics](Documentation/BingNewsAnalytics.md)      | Connecting the Bing News APIs with the Text Analytics APIs to create a visualization of the news based on their sentiment and most common topics | Bing News API, Bing AutoSuggestion API, Text Sentiment and Text KeyPhrase Extraction |
| [Emotion API Explorer](Documentation/EmotionAPIPlayground.md)       | A playground for the Emotion APIs | Windows 10 Face Tracking; Emotion prediction; Bing Image Search API; Bing AutoSuggestion API |
| [Face API Explorer](Documentation/FaceAPIPlayground.md)          | A playground for the Face APIs used for age and gender prediction, as well as face identification | Windows 10 Face Tracking; Age and gender prediction; Face identification; Bing Image Search API; Bing AutoSuggestion API |
| [Face Identification Setup](Documentation/FaceIdentificationSetup.md)    | Shows how to train the machine learning model behind the Face APIs to recognize specific people. | Face identification; Bing Image Search API; Bing AutoSuggestion API |
| [Mall Kiosk](Documentation/MallKiosk.md)      | An example of a Mall kiosk that makes product recommendations based on the people in front of the camera and analyzes their reaction to it  | Windows 10 Face Tracking; Age and gender prediction; Realtime sampling of Emotion; Face identification; Windows 10 Speech-To-Text; Text Sentiment Analysis |
| [Realtime Crowd Insights](Documentation/RealtimeCrowdInsights.md)      | A realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting | Windows 10 Face Tracking; Realtime sampling; Age, gender and emotion prediction; Face identification; Unique face counting |

# Source Code Structure

* Kiosk/ServiceHelpers: A portable library that serves as a wrapper around the several services. 
 * One of the key things that the wrappers provide is the ability to detect when the API calls return a call-rate-exceeded error and automatically retry the call (after some delay). 
 * The ImageAnalyzer class is another key class here, responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services. 
 * Another example of functionality is the FaceListManager class, which is a wrapper on top of the FaceList APIs to make it easier to track unique faces. 

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
