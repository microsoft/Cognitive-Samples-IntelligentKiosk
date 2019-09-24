# Ink Recognizer Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/InkRecognizerExplorerInkMirror.png "Ink Recognizer Explorer Ink Mirror")

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/InkRecognizerExplorerFormFiller.png "Ink Recognizer Explorer Form Filler")

The first page included in the Ink Recognizer Explorer demo showcases the ability to recognize digital handwriting and common shapes. Once the left-hand canvas is inked on, it can be recognized and "mirrored" on the right-hand canvas by rendering the results given back from the Ink Recognizer API. The user can also view the tree structure of the data that is given back, as well as the request and response JSON.

The second page in the Ink Recognizer Explorer is a simple form-filling scenario to highlight possibilities of using digital ink instead of more traditional pen and paper methods.

# Key Source Code

* [InkMirror](../Kiosk/Views/InkRecognizerExplorer/InkMirror.xaml.cs): This page shows an example of how the response data from the Ink Recognizer API can be used to render digital ink that was recognized.

* [FormFiller](../Kiosk/Views/InkRecognizerExplorer/FormFiller.xaml.cs): This page shows an example of how digital ink can be used in a form-filling scenario. It also highlights an example of a timer based approach to recognizing ink.

* [InkRecognizer](../Kiosk/ServiceHelpers/InkRecognizer.cs): This class serves as an example of how to convert ink stroke data into correctly formatted JSON that can be sent to the Ink Recognizer API.