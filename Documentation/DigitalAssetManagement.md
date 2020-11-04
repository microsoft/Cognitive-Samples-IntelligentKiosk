# Digital Asset Management
This sample illustrates how Computer Vision can add a layer of insights to a collection of images. 

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/DigitalAssetManagement.jpg "Digital Asset Management")

# Key Source Code

* [DigitalAssetManagementPage](../Kiosk/Views/DigitalAssetManagement/DigitalAssetManagementPage.xaml.cs): Main page that drives the demo. It displays the images along with its associated filters.  It also contains the UI to manage your cached insights extracted from your images.

* [ImageInsights](../Kiosk/Views/DigitalAssetManagement/ImageInsights.cs): POCO object representing insights extracted from each of your images.

* [ImageInsightsViewModel](../Kiosk/Views/DigitalAssetManagement/ImageInsightsViewModel.cs): A wrapper around the ImageInsights object to support data binding to the UI and filtering the image collection.

* [ImageProcessor](../Kiosk/Views/DigitalAssetManagement/ImageProcessor.cs): Uses your Azure Cognitive Service to extract ImageInsights from your images.

* [FilesViewModel](../Kiosk/Views/DigitalAssetManagement/FilesViewModel.cs): CRUD operations for ImageInsights.  Stores them in JSON format within the applications local storage.

## Running the demo

The first time the solution is ran you will be prompted to enter your Azure Cognitive Service key.  If you don't have one, you can create one [here](https://ms.portal.azure.com/#create/Microsoft.CognitiveServicesAllInOne).  Your key is stored in the applications local settings.  Your key can be changed in the future using the settings menu.

Next, select either a local folder containing images, or an Azure blob collection containing images.  If you are using a blob collection you will need to supply a [shared access signature URI](https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview).  This URI will allow temporary access to even private containers.

Once a folder or storage collection is selected the images will be processed for insights.  The image insights are cached in the applications local storage.  Only the insights about the images are cached while the images themselves are not.  The insights can be re opened, downloaded or deleted using the History menu.

## How it works

Each image from a local folder or a blob collection is processed through the Computer Vision API and/or the Face API, depending on which services you elect to use.  The results are cached in the applications local storage using JSON file format.  The images themselves are never cached.  The JSON file contains the output from the API for each image, minus some extracted insights not used by this demo.

Each time the JSON file is loaded it is used to create a list of filters over the images.  These filters, along with the associated images are displayed in the UI.  When a filter is selected, images matching that filter are added to the image display.  If no filters are selected, all images are displayed.
