# Realtime Driver Monitoring

An example that puts together the Face API and Vision API to envision a futuristic scenario where a dashboard camera in a car could be used to monitor the driver and determine when the driver is looking away from the road ahead, sleeping, yawning or doing certain activities, such as using the phone or eating a banana. 

The sample calls the APIs at a rate of 2fps to compute face identification and facial landmarks, as well as to determine activities via the Captioning functionality in the Vision APIs.

This sample also shows how to use FaceLists to track unique faces.

The thresholds in the simple heuristic used to determine when the driver is yawning or sleeping can be configured via the settings button to the right of the Driver Id label in the UI.

# Key Source Code

* [RealtimeDriverMonitoring](../Kiosk/Views/RealtimeDriverMonitoring.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed, creates a background loop to capture and process frames from the camera at 2fps, and maintains a simple state that is used to derive insights about the driver.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces, draw face rectangles and display the realtime data about each face).

* [FaceListManager](../Kiosk/ServiceHelpers/FaceListManager.cs): A management layer on top of the FaceList functionality.


