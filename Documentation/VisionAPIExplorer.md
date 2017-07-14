# Vision API Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/VisionApiExplorer.PNG "Vision API Explorer")

Similar to the Face API and Emotion API Explorer demos, this demo shows how to process photos through the Computer Vision APIs. It will show tags, faces, description of the photo, celebrities, OCR results and color analysis. Photos can be provided from a list of suggested photos, from a web cam, from local photos or from Bing Image results.  
 
The integration with Bing makes it super easy to locate good photos for demos (e.g. try “surfer” for a photo to show off photo captioning, or “receipt” or “Japanese receipt” to show off OCR, or a celebrity name of your choice to show off celebrity recognition). 

When using the web cam, hit the camera button to have it take a photo and show the results. Then hit the refresh button to resume the live feed.

# Key Source Code

* [VisionApiExplorer](../Kiosk/Views/VisionApiExplorer.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed and allow photo captures, as well as the ImageSearchUserControl as another input model, and displays the the photo and its results via the ImageWithFaceBorderUserControl control (also below).

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces and draw face rectangles) and that provides the control button to capture a photo.

* [ImageWithFaceBorderUserControl](../Kiosk/Controls/ImageWithFaceBorderUserControl.xaml.cs): A control used to display the result of each photo capture. It detects when the photo is loaded, automatically calls the Vision APIs and displays the result over the photo.

* [ImageAnalyzer](../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.

* [ImageSearchUserControl](../Kiosk/Controls/ImageSearchUserControl.xaml.cs): A control used for searching Bing Images and local images and using them as input in the page.
