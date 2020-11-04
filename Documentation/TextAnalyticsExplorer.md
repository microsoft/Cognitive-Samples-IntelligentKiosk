# Text Analytics Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/TextAnalyticsExplorer.jpg "Text Analytics Explorer")

Here you can explore advanced natural language processing over raw text, and includes four main functions: sentiment analysis, opinion mining, key phrase extraction, named / linked entity recognition, and language detection. You can explore the built-in text samples or type your own text for analysis.

# Key Source Code

* [TextAnalyticsExplorer](../Kiosk/Views/TextAnalyticsExplorer/TextAnalyticsExplorer.xaml.cs): Main page that drives the demo. It hosts the HorizontalStackedBarChartControl (see below) to display the document sentiment results, and displays the other result of text analysis.

* [HorizontalStackedBarChartControl](../Kiosk/Controls/HorizontalStackedBarChartControl.xaml.cs): The control that displays the document sentiment in a chart.
