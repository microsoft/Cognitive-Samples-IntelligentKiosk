# Realtime Crowd Insights

![alt text] (https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/RealtimeCrowdInsights.png "Realtime Crowd Insights")

A realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting. 

The sample uses a hybrid approach in order to obtain a fluid user interface. It calls the APIs at a rate of 1fps to compute age, gender, identification and emotion of all the faces in the frame, but it uses the built-in face tracking in Windows 10, running at 15fps, to associate and display the metadata about each face as the faces move around (using a simple heuristic based on the geometry of the faces).

This sample also shows how to use FaceLists to track unique faces.
