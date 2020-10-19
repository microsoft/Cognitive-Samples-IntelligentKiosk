# Vision API Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/VisionAPIExplorer.jpg "Vision API Explorer")

Similar to the Face API Explorer demos, this demo shows how to process photos through the Computer Vision APIs. It will show tags, faces, description of the photo, celebrities, OCR results and color analysis. Photos can be provided from a list of suggested photos, from a web cam, from local photos or from Bing Image results.

# Key Source Code

* [VisionApiExplorer](../Kiosk/Views/VisionApiExplorer.xaml.cs): Main page that drives the demo. It hosts the ImagePickerControl (see below) to provide an image as input, and displays the result of computer vision.

* [ImageAnalyzer](../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.

* [ImagePickerControl](../Kiosk/Controls/ImagePickerControl.xaml.cs): A control used for capturing a photo from camera, searching Bing Images and local images and using them as input in the page.
