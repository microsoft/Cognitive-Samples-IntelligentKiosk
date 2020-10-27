# Caption Bot

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/CaptionBot.jpg "Caption Bot")

A realtime workflow for processing frames from a web camera to automatically generate a caption that describes what the algorithm sees. It uses the Computer Vision API to identify the components of the captures.

# Key Source Code

* [CaptionKioskPage](../Kiosk/Views/CaptionKioskPage.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed, configures it to run in autonomous mode and reacts to the state machine changes to capture photos and analyze them.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces, draw face rectangles, execute the state machine that drives the autonomous photo capture workflow, etc)
