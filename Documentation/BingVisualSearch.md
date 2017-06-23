 Bing Visual Search

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/BingVisualSearch.jpg "Bing Visual Search")

This example shows how to use the power of Bing Image Insights to perform visual search and find visually similar images, products or celebrities on the internet. 

*	The results contain the top results for the given input image, sorted by visual similarity.
*	Through the buttoms on the top-right corner you can select input images from Bing Image searches, local files, web cam capture or from a selection of favorite images. 
*	The Search Type drop down menu allows you to change the type of search results.
*	The Bing Autosuggestion API is also used in this demo to offer search suggestions while you type.

# Key Source Code

* [BingVisualSearch](../Kiosk/Views/BingVisualSearch.xaml.cs): Main page that drives the demo

* [BingSearchHelper](../Kiosk/ServiceHelpers/BingSearchHelper.cs): The code that calls the Bing Search APIs
