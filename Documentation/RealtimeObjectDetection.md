# Realtime Object Detection

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/RealtimeObjectDetection.png "Realtime Object Detection")

This is an Intelligent Edge scenario built on top of Windows ML and ONNX to bring to life a completely offline object detection experience that runs directly from the camera feed. This demo uses a model built and exported by Microsoft Custom Vision.

# Key Source Code

* [RealtimeObjectDetection](../Kiosk/Views/RealtimeObjectDetection.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed, creates a background loop to capture and process frames from the camera. It then uses the result to detect objects in the image.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and enables camera captures.
