# Face API Playground

![alt text] (https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/FaceAPIPlayground.png "Face API Playground")

Here you can explore age and gender prediction as well as face identification. You can do so by using web cam captures, local files on your hard drive or images from Bing Images. For people that have been added to the Face Identification Setup model it will show their names and age, and for unknown people it will show their gender and age. 

When using the web cam, hit the camera button to have it take a photo and show the results. Then hit the refresh button to resume the live feed.

# Key Source Code

* [RecognitionPage] (../Kiosk/Views/RecognitionPage.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed and allow photo captures, as well as the ImageSearchUserControl as another input model, and displays the the photo and its results via the ImageWithFaceBorderUserControl control (also below).

* [CameraControl] (../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces and draw face rectangles) and that provides the control button to capture a photo.

* [ImageWithFaceBorderUserControl] (../Kiosk/Controls/ImageWithFaceBorderUserControl.xaml.cs): A control used to display the result of each photo capture. It detects when the photo is loaded, automatically calls the Face APIs to perform the age, gender and face identification and displays the result over the faces in the photo.

* [ImageAnalyzer] (../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.

* [ImageSearchUserControl] (../Kiosk/Controls/ImageSearchUserControl.xaml.cs): A control used for searching Bing Images and local images and using them as input in the page.
