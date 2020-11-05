# Speech to Text Explorer

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/SpeachToTextExplorer.jpg "Speech to Text Explorer")

Speech to Text Explorer uses Speech to Text API from Azure Speech Services to convert spoken audio to text. Speech to text uses the Universal language model, which was trained on Microsoft-owned data for conversational and dictation scenarios.

# Speech translation

To translate your speech in real time, go to Translation and choose a target language. Use the microphone to start transcribing live audio, or go to Sample and choose a pre-recorded audio sample to capture from.

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/SpeachToTextWithTranslationExplorer.jpg "Speech translation")

# Key Source Code

* [SpeechToTextExplorer](../Kiosk/Views/SpeechToText/SpeechToTextExplorer.xaml.cs): Main page that drives the demo. It hosts the SpeechToTextView and SpeechToTextWithTranslation (see below) to display the result of audio-to-text conversion and translation.

* [SpeechToTextView](../Kiosk/Views/SpeechToText/SpeechToTextView.xaml.cs): A custom control used to display the result of converting audio to text.

* [SpeechToTextWithTranslation](../Kiosk/Views/SpeechToText/SpeechToTextWithTranslation.xaml.cs): A custom control used to display the result of converting audio to text with translation.
