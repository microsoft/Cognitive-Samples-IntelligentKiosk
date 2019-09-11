# Anomaly Detector

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/AnomalyDetectorDemo.jpg "Anomaly Detector")

This demo shows anomaly detection using the Anomaly Detector API. There are a three built-in scenarios that you can use out of the box using pre-selected data streams and story lines, as well as a Live Sound scenario that samples the audio level of your microphone as the input data stream.

# Key Source Code

* [AnomalyDetectorDemo](../Kiosk/Views/AnomalyDetector/AnomalyDetectorDemo.xaml.cs): Main page that drives the demo. It hosts the AnomalyChartControl (see below) to display the API results, sets up all the scenarios, including the initialization/handling of the audio processing pipeline for the Live Sound scenario.

* [AnomalyChartControl](../Kiosk/Views/AnomalyDetector/AnomalyChartControl.xaml.cs): The code that contains the chart and enables different types of scenarios.

* [ScenarioInfoControl](../Kiosk/Views/AnomalyDetector/ScenarioInfoControl.xaml.cs): Displays the story line behind each scenario.
