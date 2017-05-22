 Bing News Analytics

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/BingNewsAnalytics.jpg "Bing News Analytics")

This example connects the Bing News APIs with the Text Analytics APIs to create a visualization of the news based on their sentiment and most common topics.

*	The results contain the top-50 results from Bing News for the given query, sorted by sentiment of the headlines from most positive to most negative. A background color reflects the sentiment (using a linear gradient from green to red) to make it easy to digest, but the sentiment value is also displayed for reference and for the color blind.
*	The Sentiment Distribution on the top-left provides the big picture of the news sentiment. The colors in the chart also match the colors used in the result tiles. 
*	The Top Keywords on the bottom-left is a word cloud of the top 10 keywords in the news headlines. The keywords are computed via the Key Phrase API in Text Analytics.
*	Up on the top-right corner you can turn off the sentiment sorting and see what the Bing-ranked results look like.
*	The result tiles are hyperlinked to the news.
*	The Bing Autosuggestion API is also used in this demo to offer search suggestions while you type.
*	The search results can be changed to all the four different languages that are supported by the Text Sentiment API (English, Spanish, French and Portuguese). 

# Key Source Code

* [BingNewsAnalytics](../Kiosk/Views/BingNewsAnalytics.xaml.cs): Main page that drives the demo

* [BingSearchHelper](../Kiosk/ServiceHelpers/BingSearchHelper.cs): The code that calls the Bing News API

* [TextAnalyticsHelper](../Kiosk/ServiceHelpers/TextAnalyticsHelper.cs): The code that calls the Text Analytics APIs
