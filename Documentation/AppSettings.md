# App Settings

* **API Keys**: Lets you enter the API Keys for the various services. The "Workspace Key" is a concept internal to the kiosk, and is used as a way to separate Face Indentification data (person groups, people in those groups, their face samples, etc) between different workspaces/people that share the same Face API Key. Enter something meaningful for the key (e.g. JoeKiosk), or generate an unique key through the button and keep track of that value for your records. If you already have a key from a previous setup just re-use it, that way you will start with existing training data (assuming you are using the same Face API Key).

* **Camera Source**: Lets you pick which camera to be used, otherwise it will use the default camera in Windows. 

* **Show debug info**: Enables some debugging UI. It is currently used only for showing the current coverage value of each face (see "“Minimum detectable face size” setting) so it is easier to adjust it. 
 
* **Minimum detectable face size**: Lets you control the “engagement” zone when detecting faces near the camera. It is a simple way of ignoring people in the background by ignoring smaller faces. The value indicates the minimum percentage value of the face height in relation to the camera image height in order for it to be recognized. If you set it to 0 then no faces will be ignored.  
