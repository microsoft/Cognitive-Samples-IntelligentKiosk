# Visual Alert Builder

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/VisualAlertBuilder.jpg "Visual Alert Builder")

This sample illustrates how to leverage Microsoft Custom Vision Service to train a device with a camera to detect specific visual states, and how to run this detection pipeline offline directly on the device through an ONNX model exported from Custom Vision. A visual state could be something like an empty room or a room with people, an empty driveway or a driveway with a truck, etc. 

Once you have it configured and at least one alert created, just select a target alert and point your camera to particular item for scoring. Once you do it, the results are shown below the camera, along with the associated confidence level.

# Key Source Code

* [VisualAlertBuilderPage](../Kiosk/Views/VisualAlert/VisualAlertBuilderPage.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed and capture frames for processing, as well as the VisualAlertBuilderWizardControl and LifecycleControl (see below) to create custom visual alerts.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and enables camera captures.

* [VisualAlertBuilderWizardControl](../Kiosk/Views/VisualAlert/VisualAlertBuilderWizardControl.xaml.cs): A custom wizard control used for creating visual alerts.

* [LifecycleControl](../Kiosk/Controls/LifecycleControl.xaml.cs): A control used to display the current step in the alert creation.
