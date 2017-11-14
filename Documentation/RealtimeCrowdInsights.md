# Realtime Crowd Insights

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/RealtimeCrowdInsights.png "Realtime Crowd Insights")

A realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting. 

The sample uses a hybrid approach in order to obtain a fluid user interface. It calls the APIs at a rate of 1fps to compute age, gender, identification and emotion of all the faces in the frame, but it uses the built-in face tracking in Windows 10, running at 15fps, to associate and display the metadata about each face as the faces move around (using a simple heuristic based on the geometry of the faces).

This sample also shows how to use FaceLists to track unique faces.

# Key Source Code

* [RealTimeDemo](../Kiosk/Views/RealTimeDemo.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed, creates a background loop to capture and process frames from the camera at 1fps, implements the IRealTimeDataProvider interface used by the camera control to visualize data in realtime and maintains state about the demographics and overall stats.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces, draw face rectangles and display the realtime data about each face).

* [FaceListManager](../Kiosk/ServiceHelpers/FaceListManager.cs): A management layer on top of the FaceList functionality.

* [EmotionResponseTimelineControl](../Kiosk/Controls/EmotionResponseTimelineControl.xaml.cs): The control that displays the Crowd Average Emotion Timeline.

* [AgeGenderDistributionControl](../Kiosk/Controls/AgeGenderDistributionControl.xaml.cs): The control that displays the Demographics data.
 
* [OverallStatsControl](../Kiosk/Controls/OverallStatsControl.xaml.cs): The control that displays the Overall Stats data.

* [RealTimeFaceIdentificationBorder](../Kiosk/Controls/RealTimeFaceIdentificationBorder.xaml.cs): The control that displays the realtime feedback over the faces in the camera feed. It consists entirely of text elements - even the emotion feedback is just using an emoji font (see [EmotionEmojiControl](../Kiosk/Controls/EmotionEmojiControl.xaml.cs)).



