# Emotion API Playground

![alt text] (https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/EmotionAPIRecognitionPlayground.png "Emotion API Playground")

This page is a simple example of emotion recognition. It allows you to load images from Bing or Local Files, or from the web camera, and displays the top-3 emotions on each face.

When using the web cam, hit the camera button to have it take a photo and show the results. Then hit the refresh button to resume the live feed.

# Key Source Code

* [EmotionRecognitionPage] (../Kiosk/Views/EmotionRecognitionPage.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed and allow photo captures, as well as the ImageSearchUserControl as another input model, and displays the the photo and its results via the ImageWithFaceBorderUserControl control (also below).

* [CameraControl] (../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces and draw face rectangles) and that provides the control button to capture a photo.

* [ImageWithFaceBorderUserControl] (../Kiosk/Controls/ImageWithFaceBorderUserControl.xaml.cs): A control used to display the result of each photo capture. It detects when the photo is loaded, automatically calls the Emotion APIs to perform the emotion analysis and displays the result over the faces in the photo.

* [ImageAnalyzer] (../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.

* [ImageSearchUserControl] (../Kiosk/Controls/ImageSearchUserControl.xaml.cs): A control used for searching Bing Images and local images and using them as input in the page.
