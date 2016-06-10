# Intelligent Kiosk Sample
The Intelligent Kiosk Sample is a collection of demos showcasing workflows and experiences built on top of the Microsoft Cognitive Services. Most of the experiences are hands-free and autonomous, using the human faces in front of a web camera as the main form of input (thus the word "kiosk" in the name).

# Requirements
1. A Windows 10 computer (the sample is a UWP application)
2. A webcam, ideally top-mounted so you have a similar experience as looking at a mirror when interacting with the demos 
3. Visual Studio 2015
4. API Keys
  * Face and Emotion Keys from [Microsoft.com/cognitive](https://www.microsoft.com/cognitive-services)
  * Bing Search API Key from [Azure Market Place](https://azure.microsoft.com/en-us/marketplace/partners/bing/search/) (only needed if you will be using the search functionality)

# Running the sample
1. Open the solution in Visual Studio 2015 and hit F5
2. Enter your API Keys in the Settings page (they will be saved in the user profile)
  * The "Workspace Key" is a key concept internal to the sample, and is used as a way to separate Face Indentification data (person groups, people in those groups, their face samples, etc) between different workspaces/people that share the same Face API Key. Enter something meaningful for the key (e.g. JoeKiosk), or generate an unique key through the button and keep track of that value for your records. If you already have a key from a previous setup just re-use it, that way you will start with existing training data (assuming you are using the same Face API Key).

# Demos
**Automatic Photo Capture**: An example of an autonomous workflow for capturing photos when people approach a web camera and pose for a photo.

It uses the built-in face tracking functionality in Windows 10 to detect when people are nearby and applies a simple movement detection heuristic to determine when they are posing for a photo. In the example it also shows the age, gender and identification of all the people in each capture.

**Realtime Crowd Insights**: An example of a realtime workflow for processing frames from a web camera to derive realtime crowd insights such as demographics, emotion and unique face counting. 

The sample uses a hybrid approach in order to obtain a fluid user interface. It calls the APIs at a rate of 1fps to compute age, gender, identification and emotion of all the faces in the frame, but it uses the built-in face tracking in Windows 10, running at 15fps, to associate and display the metadata about each face as the faces move around (using a simple heuristic based on the geometry of the faces).

This sample also shows how to use FaceLists to track unique faces.

**Face API Playground**: Here you can explore age and gender prediction as well as face identification by using web cam photos, local files on your hard drive or images from Bing Images. For people that have been added to the Face Identification Setup model it will show their names and age, and for unknown people it will show their gender and age. When using the web cam, hit the pause button to have it take a photo and show the results. Then hit the play button to resume the live feed. 

**Emotion API Playground**: This page is a simple example of emotion recognition. It allows you to load images from Bing or Local Files, or from the web camera, and displays the top-3 emotions on each face. When using the web cam, hit the pause button to have it take a photo and show the results. Then hit the play button to resume the live feed. 

# Face Identification Setup

The Face Identification Setup portion of the application allows you to train the machine learning model behind the Face APIs to recognize specific people. It integrates with Bing Images to provide easy access to training images, which works great for famous people. You can literally have a model trained to recognize a celebrity in a matter of seconds! You can also provide images from local files, for not so famous people.  

The people in the trained set are organized in groups of people. The idea is that related people are grouped together. For example, one group could be for Microsoft Executives, while another for Celebrities, or audience guests during a demo, etc. This is mostly for making it easier to manage the people in the app, but it won’t affect the recognition (all people in all groups are tested against when a recognition is performed). 

When loading the Face Identification Setup page the first view shows the Person Groups. You can create a new group here or navigate to an existing one by selecting it. 

When navigating to a Person Group, the page will show the list of people in the group. From here you can create a new person or navigate to an existing one. 

The final page in this process, the Person page, is where you can train the system to recognize specific people by providing examples of photos of that person. If the person is a famous person (celebrity, business leader, etc) you might be able to find training images via Bing Images, which is integrated into the app. Otherwise you can provide images from local files. 

When browsing through image results (either from Bing Images or local files), select the desired photos in the list and click on the Add Selected Photos button. Once that is done you will see the selected photos being listed under the Person page. 

**Important**: Whenever a person is added to a group (or deleted), or when new training photos are added to a person (or deleted), the model needs to be re-trained. Currently that is triggered manually by executing the Train button in the Face Identification Setup page, so it is important that whenever you are done modifying the data that you remember to immediately follow it by re-training the modified group, otherwise the recognition experiments that you do after that won’t work. 

Once that is done, those people in the training group will have their names displayed in the Kiosks when they use it (or a photo of them is held up towards the camera).

**Adding people in batches** 

You can add people in batches to a Person Group by going to the “…” in the command bar and selecting the options to train from photo albums or Bing Image Searches.  

  * Auto train from Bing: It will automate the process for you by adding people from a list and assigning the top-5 results from the Bing Images search as their sample faces. You could add as many people as you would like in this way, then just go back one by one to see their sample pictures and possibility remove bad/duplicate ones (select all the bad photos + right click to bring a “delete selected photo(s)” menu) and add more photos. Just enter the people’s names (one per line), add any desired prefix or suffix terms for the Bing search (e.g. NBC, or News, etc) and click the Ok button. It will then trigger a background process of adding all the people (it will be done when the progress animation stops). Your mileage may vary depending on the repository of Bing Images for each person, but you can always delete  or add more photos after the batch process is done, so worst case you still save the time of having to type the names of all the people one at a time, and in the best case you will just need to review the sample photos and realize it worked perfectly. 

  * Auto train from photo albums: It will ask you to provide a root folder, and from there it will traverse all the sub-folders (not recursively) and create a person with the name of each sub-folder and use the photos in that sub-folder as training photos for that person. Just make sure the photos are all less than 5MB and have only one subject per photo. 

# Other App Settings
1. The “Minimum detectable face size” value lets you control the “engagement” zone when detecting faces near the camera. It is a simple way of ignoring people in the background by ignoring smaller faces. The value indicates the minimum percentage value of the face height in relation to the camera image height in order for it to be recognized. If you set it to 0 then no faces will be ignored.  
2. “Show debug info” is currently used only for one thing, to show the current coverage value of each face (see "“Minimum detectable face size” point above) so it is easier to adjust it for a given location. 

3. The "Camera Sourc"e setting that lets you pick which camera to be used, otherwise it will use the default camera in Windows. 

4. A few settings will let you control the face identification experience (minimun confidence level, what to show in the UI, etc).  

5. One of the settings enables you to save a copy of the cropped version of all the unique faces detected by the Realtime Crowd Insights Sample so you can use it for debugging purposes.
