# Insurance Claim Automation

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/InsuranceClaimAutomation.jpg "Insurance Claim Automation")

An example of Robotic Process Automation (RPA), leveraging Custom Vision and Form Recognizer to illustrate automating the validation of insurance claims. 

**Important notes**

To enable this demo you should provide the following keys:

Settings page:
- Custom Vision API Training/Prediction Keys.

Code behind - [InsuranceClaimAutomation](../Kiosk/Views/InsuranceClaimAutomation/InsuranceClaimAutomation.xaml.cs):
- Custom Vision Object Detection and Classification Model ID for your particular business scenario.
- Form Recognizer API key and endpoint.
- Form Recognizer Model ID. See how to create your own Form Recognizer Model [here](https://docs.microsoft.com/en-us/azure/cognitive-services/form-recognizer/quickstarts/dotnet-sdk).


# Key Source Code

* [InsuranceClaimAutomation](../Kiosk/Views/InsuranceClaimAutomation/InsuranceClaimAutomation.xaml.cs): Main page that drives the demo. It hosts the InputPickerControl to select the product and form image, analyzes those images and shows the result in the result table.

* [InputPickerControl](../Kiosk/Views/InsuranceClaimAutomation/InputPickerControl.xaml.cs): A control used for collecting input images – from Bing Images, local files or camera captures.

* [VisualAlertBuilderWizardControl](../Kiosk/Views/InsuranceClaimAutomation/DetailsViewControl.xaml.cs): A control used to display details of the particular claim: the detected object in the product image and the extracted form fields.
