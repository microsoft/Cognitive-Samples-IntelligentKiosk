# Mall Kiosk

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/Mall.png "Mall Kiosk")

An  example of a Mall kiosk that makes product recommendations based on the people in front of the camera and analyzes their reaction to it. 

* The workflow starts with a snapshot from the webcam. To do that just wait until the target audience is in the frame and click on the Camera button in the camera preview. 
* Depending on the people’s ages and gender (or identity) a recommended product is displayed in the screen.
* To start over just click on the Refresh button and the webcam preview will restart and be ready for another snapshot.
* The recommendations are configurable via the Settings button. The format is a self-explanatory set of URLs. It provides a way to define a set of generic recommendations based on gender/age/# of people, personalized recommendations based on people’s names, as well as recommendation overrides based on spoken keywords or sentiment. 

# Analyzing the reaction to a recommendation

* Via voice: The microphone button in the camera preview will allow you to capture speech input, convert it to text,  pass it to the Text Sentiment API and visualize the feedback. This feature can be used in this demo to override the recommendation based on some keyword text or positive/negative sentiment values. To speak, push the microphone button, when you are done push the stop button and it will analyze the text that the microphone captured.
* Via facial expressions: On the top/right corner of the camera live feed the FourBars button will show the average emotion response of the people in front of the camera, in realtime. If you push the Photo button in that UI it will also show the snapshots of the faces and their top emotion, which is how the demo calculates the average emotion response.

# Key Source Code

* [MallKioskPage](../Kiosk/Views/MallKioskPage.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed and allow photo captures, and a WebView control to displays the product recommendation web pages. It also contains the processing loop for analyzing the audience emotion in realtime when the facial expression feedback UI is activated (top/right corner of the camera feed).

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces and draw face rectangles) and that provides the control button to capture a photo.

* [SpeechToTextControl](../Kiosk/Controls/SpeechToTextControl.xaml.cs): The control used for text-to-speech input and sentiment analysis, raising an event to the main demo page with the results so it can use it to possibly provide an alternative product recommendation.

* [ImageAnalyzer](../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.
