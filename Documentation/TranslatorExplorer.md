# Translator API Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/TextTranslatorExplorer.png "Translator API Explorer")

This demo shows text translation using the Translator Text API. Using this demo, users can translate text to and from 60+ supported languages, and easily detect the language of any text. You will also be able to find alternative translations for single words, along with examples for different contexts.

# Text Translation using OCR

Also we added the ability to provide images from local files, Bing Images or web camera as an input method. We will OCR it, detect the language and make the translation.

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/ImageTranslatorExplorer.png "Translator API Explorer")

# Key Source Code

* [TranslatorExplorerPage](../Kiosk/Views/TranslatorExplorer/TranslatorExplorerPage.xaml.cs): Main page that drives the demo.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks and that provides the control button to capture a photo.

* [ImageWithFaceBorderUserControl](../Kiosk/Controls/ImageWithFaceBorderUserControl.xaml.cs): A control used to display the result of each photo capture. It detects when the photo is loaded, automatically calls the Vision APIs and displays the result over the photo.

* [ImageAnalyzer](../Kiosk/ServiceHelpers/ImageAnalyzer.cs): Responsible for wrapping an image and exposing several methods and properties that act as a bridge to the Cognitive Services.

* [ImageSearchUserControl](../Kiosk/Controls/ImageSearchUserControl.xaml.cs): A control used for searching Bing Images and local images and using them as input in the page.
