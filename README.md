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

# Demos
**Automatic Photo Capture**: An example of an autonomous workflow for capturing photos when people approach a web camera and pose for a photo.

It uses the built-in face tracking functionality in Windows 10 to detect when people are nearby and applies a simple movement detection heuristic to determine when they are posing for a photo. In the example it also shows the age, gender and identification of all the people in each capture.

**Realtime Crowd Insights**: An example of a realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting. 

The sample uses a hybrid approach in order to obtain a fluid user interface. It calls the APIs at a rate of 1fps to compute age, gender, identification and emotion of all the faces in the frame, but it uses the built-in face tracking in Windows 10, running at 15fps, to associate and display the metadata about each face as the faces move around (using a simple heuristic based on the geometry of the faces).

This sample also shows how to use FaceLists to track unique faces.

**Face API Playground**: Here you can explore age and gender prediction as well as face identification by using web cam photos, local files on your hard drive or images from Bing Images. For people that have been added to the Face Identification Setup model it will show their names and age, and for unknown people it will show their gender and age. When using the web cam, hit the pause button to have it take a photo and show the results. Then hit the play button to resume the live feed. 

**Emotion API Playground**: This page is a simple example of emotion recognition. It allows you to load images from Bing or Local Files, or from the web camera, and displays the top-3 emotions on each face. When using the web cam, hit the pause button to have it take a photo and show the results. Then hit the play button to resume the live feed. 

**Face Identification Setup**

The Face Identification Setup portion of the application allows you to train the machine learning model behind the Face APIs to recognize specific people. See the [Face Identification Setup documentation](Documentation/FaceIdentificationSetup.md) for the details.
