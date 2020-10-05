# How Old

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/HowOld.jpg "How Old")

This is an example of an autonomous workflow for capturing photos when people approach a web camera and pose for a photo.

It uses the built-in face tracking functionality in Windows 10 to detect when people are nearby and applies a simple movement detection heuristic to determine when they are posing for a photo. In the example it also shows the age, gender and identification of all the people in each capture.

# Key Source Code

* [HowOldKioskPage](../Kiosk/Views/HowOldKioskPage.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed, configures it to run in autonomous mode and reacts to the state machine changes to capture photos and analyze them.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces, draw face rectangles, execute the state machine that drives the autonomous photo capture workflow, etc)

* [ImageWithFaceBorderUserControl](../Kiosk/Controls/ImageWithFaceBorderUserControl.xaml.cs): A control used to display the result of each photo capture. It detects when the photo is loaded, automatically calls the Face APIs to perform the age, gender and face identification and displays the result over the faces in the photo.

* [ImageAnalyzer](../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.
