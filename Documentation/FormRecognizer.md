# Form Recognizer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/FormRecognizerCustom.jpg "Form Recognizer Custom models")

To train a model using your own forms, you’ll first need to create a Form Recognizer service in Azure. Add your service’s key to the settings page under Cognitive Services Keys -> Form Recognizer API. Also you need to provide your Azure Storage Account in the code behind FormRecognizerScenarioSetup.xaml.cs. Once you’ve collected five example forms for training, use New Scenario to create your model.

# Prebuilt receipt model

Form Recognizer also includes a model for reading English sales receipts from the United States—the type used by restaurants, gas stations, retail, and so on. This model extracts key information such as the time and date of the transaction, merchant information, amounts of taxes and totals and more. In addition, the prebuilt receipt model is trained to recognize and return all of the text on a receipt.

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/FormRecognizerReceipt.jpg "Form Recognizer Receipt model")

# Key Source Code

* [FormRecognizerExplorer](../Kiosk/Views/FormRecognizer/FormRecognizerExplorer.xaml.cs): Main page that drives the demo. It hosts the control used for providing input images and display the API results.

* [FormRecognizerScenarioSetup](../Kiosk/Views/FormRecognizer/FormRecognizerScenarioSetup.xaml.cs): A custom wizard control used for creating custom models.
