# Face API Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/FaceAPIExplorer.jpg "Face API Explorer")

Here you can explore extract a series of face-related attributes, such as head pose, gender, age, emotion, facial hair, and glasses. You can do so by using web cam captures, local files on your hard drive or images from Bing Images. For people that have been added to the Face Identification Setup model it will also show their names.

# Key Source Code

* [FaceApiExplorerPage](../Kiosk/Views/FaceApiExplorer/FaceApiExplorerPage.xaml.cs): Main page that drives the demo. It hosts the ImagePickerControl (see below) to provide an image as input, and displays the result of face detection.

* [ImageAnalyzer](../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.

* [ImagePickerControl](../Kiosk/Controls/ImagePickerControl.xaml.cs): A control used for capturing a photo from camera, searching Bing Images and local images and using them as input in the page.
