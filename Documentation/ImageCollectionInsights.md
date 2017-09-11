# Image Collection Insights

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/ImageCollectionInsights.JPG "Image Collection Insights")

This demo showcases an example of using Cognitive Services to add a layer of intelligence on top of a collection of 
photos (e.g. filter by face, by emotion, by tag, etc). 

Couple notes:
* Once it processes a folder it saves the results in a json file in that folder so it doesn’t repeat the work again when looking at
that folder. If you want to force it to bypass the json file and re-compute, expand the toolbar menu and you will find a way to do it

*	Processing is limited to the first 50 files. If you want to bypass that and compute all files in a folder, expand the toolbar menu
and you will find a toggle to enable that

# Key Source Code

* [ImageCollectionInsights](../Kiosk/Views/ImageCollectionInsights/ImageCollectionInsights.xaml.cs): Main page that drives the demo. It 
displays the images on a grid on the right hand side, and adds a set of filters on the left hand side that shows the insights from
the photos and lets you filter the content by faces, emotion or visual features. 

* [ImageProcessor](../Kiosk/Views/ImageCollectionInsights/ImageProcessor.cs): Class that processes each photo and creates the metadata
that is displayed by the UI. 
