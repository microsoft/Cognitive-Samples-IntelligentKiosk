# Custom Vision Setup

This is where you can create and manage your own models. In case you have used the Face Identification Setup in the kiosk this 
will look very familiar to you. It integrates with Bing Images very nicely, which literally makes the task of creating a classifier a 
matter of seconds. 

You can get to the Custom Vision Setup page by clicking on the ‘+’ button near the Project selector in the Custom Vision Explorer page. 
The Setup page will prompt you for API keys. In case you don’t have keys for the service you can find the key acquisition steps by 
clicking on the Settings button on the top-right of the Setup page.

Image Classification model:
![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/CustomVisionSetup.JPG "Custom Vision Setup")

Object Detection model:
![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/SetupObjectDetection.png "Custom Vision Setup")

# Camera integration during the Custom Vision Setup workflow

When you are creating your image classifiers the web cam capture can come in very handy. You can just enable auto-capture in the UI 
and spend a few seconds moving the object in front of the camera while it captures photos from several angles.

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/CustomVisionCameraCapture.JPG "Custom Vision Camera Capture")

# Setup Object Detection model

This is how you can create and manage your own object detection model. When you tag images in object detection project, you need to specify the region of each tagged object.

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/SetupObjectRegions.png "Setup Object Detection model")

# Export your Custom Vision models

Also you can export your Custom Vision models and run them offline in the Realtime Image Classification and Realtime Object Detection demo.

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/ExportCustomVisionModel.png "Export your Custom Vision models")

# Key Source Code

* [CustomVisionSetup](../Kiosk/Views/CustomVision/CustomVisionSetup.xaml.cs): Main page that drives the demo. 

* [ImageSearchUserControl](../Kiosk/Controls/ImageSearchUserControl.xaml.cs): A control used for providing input images. It allows you to provide images from Bing Images, local images or the web camera.

* [ImageWithRegionEditorsControl](../Kiosk/Views/CustomVision/ImageWithRegionEditorsControl.xaml.cs): A control used for managing model input images. It allows you to specify the region of each tagged object in the image.

* [RegionEditorControl](../Kiosk/Views/CustomVision/RegionEditorControl.xaml.cs): A control used for drawing regions in the image.