# Speaker Recognition Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/SpeakerRecognitionExplorer.jpg "Speaker Recognition Explorer")

The Speaker Recognition service provides algorithms that identify speakers by their unique voice characteristics using voice biometry. You can also provide audio training data for a single speaker, which creates an enrollment profile based on the unique characteristics of the speaker's voice. You can then cross-check audio voice samples against a group of enrolled speaker profiles, to see if it matches any profile in the group (speaker identification). 
Speaker Recognition is currently only supported in Azure Speech resources created in the westus region.

# Key Source Code

* [SpeakerRecognitionExplorer](../Kiosk/Views/SpeakerRecognition/SpeakerRecognitionExplorer.xaml.cs): Main page that drives the demo. It hosts the control used for providing audio input from a microphone or local file and determine an unknown speaker’s identity within a group of enrolled speakers.

* [SpeakerRecognitionModelSetup](../Kiosk/Views/SpeakerRecognition/SpeakerRecognitionModelSetup.xaml.cs): A custom wizard control used for creating custom voice models.
