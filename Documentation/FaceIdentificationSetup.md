# Face Identification Setup

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/FaceIdentificationSetup.png "Face Identification")

The Face Identification Setup portion of the application allows you to train the machine learning model behind the Face APIs to recognize specific people. It integrates with Bing Images to provide easy access to training images, which works great for famous people. You can literally have a model trained to recognize a celebrity in a matter of seconds! You can also provide images from local files, for not so famous people.  

The people in the trained set are organized in groups of people. The idea is that related people are grouped together. For example, one group could be for Microsoft Executives, while another for Celebrities, or audience guests during a demo, etc. This is mostly for making it easier to manage the people in the app, but it won’t affect the recognition (all people in all groups are tested against when a recognition is performed). 

* When loading the Face Identification Setup page, start by creating a new group or selecting an existing group.  
 
* Once a group is selected it will show the list of people in the group. From here you can create a new person or select an existing person. 
 
* Once a person is selected the next step is to provide examples of photos of that person. If the person is a famous person (celebrity, business leader, etc) you might be able to find training images via Bing Images, which is integrated into the app. Otherwise you can provide images from local files or from web camera captures. 

**Important**: Whenever a person is added to a group (or deleted), or when new training photos are added to a person (or deleted), the model needs to be re-trained. Currently that is triggered manually by executing the Train button in the Face Identification Setup page, so it is important that whenever you are done modifying the data that you remember to immediately follow it by re-training the modified group, otherwise the recognition experiments that you do after that won’t work. 

Once that is done, those people in the training group will have their names displayed in the Kiosks when they use it (or a photo of them is held up towards the camera).

**Adding people in batches** 

You can add people in batches to a Person Group by going to the “…” in the command bar and selecting the options to train from photo albums or Bing Image Searches.  

  * Auto train from Bing: It will automate the process for you by adding people from a list and assigning the top-5 results from the Bing Images search as their sample faces. You could add as many people as you would like in this way, then just go back one by one to see their sample pictures and possibility remove bad/duplicate ones (select all the bad photos + right click to bring a “delete selected photo(s)” menu) and add more photos. Just enter the people’s names (one per line), add any desired prefix or suffix terms for the Bing search (e.g. NBC, or News, etc) and click the Ok button. It will then trigger a background process of adding all the people (it will be done when the progress animation stops). Your mileage may vary depending on the repository of Bing Images for each person, but you can always delete  or add more photos after the batch process is done, so worst case you still save the time of having to type the names of all the people one at a time, and in the best case you will just need to review the sample photos and realize it worked perfectly. 

  * Auto train from photo albums: It will ask you to provide a root folder, and from there it will traverse all the sub-folders (not recursively) and create a person with the name of each sub-folder and use the photos in that sub-folder as training photos for that person. Just make sure the photos are all less than 5MB and have only one subject per photo. 

# Key Source Code

* [FaceIdenficationSetup.xaml.cs](../Kiosk/Views/FaceIdentificationSetup.xaml.cs): Page that hosts the UI.

* [ImageSearchUserControl](../Kiosk/Controls/ImageSearchUserControl.xaml.cs): A control used for selecting training images. Either from Bing Images, local images or from web camera captures.
