# Realtime Video Insights

A realtime workflow for processing frames from a video file to derive realtime insights such as demographics, emotion, unique face
counting and face identification against faces in your own trained model or in the built-in celebrity model in the Vision APIs. 

This sample offers a visualization of the unique faces via tracks on a timeline. The emotion of each person during the video is 
then shown via slices on that timeline. 
 
You can go to any frame in the video by clicking on the Seek bar in the video control (you can also do this by clicking at any point in 
time in the Emotion Timeline, in case you want to see the frame that caused a big spike in surprise, for example). You can also pause the 
video, change the playback speed, etc. 

The demo will attempt to identify all the people in the video by checking against existing trained models in the kiosk, as well as the
Celebrity model in the Vision API.

Blank spots in the timeline mean that either that person was not seen at that particular point in the video, or that due to latency
issues that particular frame was not processed (the demo will always be processing the most recent frame in the video, at a
maximun rate of 1fps if the API latency is low). 

# Key Source Code

* [VideoInsightsPage] (../Kiosk/Views/VideoInsightsPage.xaml.cs): Main page that drives the demo. It hosts the MediaElement to display the video, creates a background loop to process frames from the video at 1fps and maintains state about the demographics, overall stats and tracks for people in the video.

* [FaceListManager] (../Kiosk/ServiceHelpers/FaceListManager.cs): A management layer on top of the FaceList functionality.

* [VideoTrack] (../Kiosk/Controls/VideoTrack.xaml.cs): The control that displays the Emotion Timeline for each person in the video.

* [AgeGenderDistributionControl] (../Kiosk/Controls/AgeGenderDistributionControl.xaml.cs): The control that displays the Demographis data.
 
* [OverallStatsControl] (../Kiosk/Controls/OverallStatsControl.xaml.cs): The control that displays the Overall Stats data.

* [FrameRelayVideoEffect] (../Kiosk/KioskRuntimeComponent/FrameRelayVideoEffect.cs): A video effect that is added to the 
video playback pipeline to provide access to video frames while the video is playing. It does not really add any effects, just 
provides access to each frame.

