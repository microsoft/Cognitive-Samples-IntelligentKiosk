# Anomaly Detector

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/AnomalyDetectorDemo.png "Anomaly Detector")

This demo shows anomaly detection using the Anomaly Detector API. There are many types of time-series data—and no one algorithm fits them all. Anomaly Detector looks at your time-series data set and automatically selects the best algorithm from the model gallery to ensure high accuracy for your specific scenario: business incidents, monitoring IoT device traffic, managing fraud, responding to changing markets and more.

# Key Source Code

* [AnomalyDetectorDemo](../Kiosk/Views/AnomalyDetector/AnomalyDetectorDemo.xaml.cs): Main page that drives the demo. It hosts the AnomalyChartControl (see below) to display the chart, setup a microphone to capture and process audio data. It then uses the result to detect anomaly data.

* [AnomalyChartControl](../Kiosk/Views/AnomalyDetector/AnomalyChartControl.xaml.cs): The code that contains the chart and enables different type of scenarios.

* [ScenarioInfoControl](../Kiosk/Views/AnomalyDetector/ScenarioInfoControl.xaml.cs): The code that contains the scenario display information.
