# Neural Text to Speech

![alt text](https://github.com/Microsoft/Cognitive-Samples-IntelligentKiosk/blob/master/Documentation/NeuralTextToSpeech.jpg "Neural Text to Speech")

Text-to-speech converts input text into human-like synthesized speech using Speech Synthesis Markup Language (SSML). Use neural voices, which are human-like voices powered by deep neural networks.

# Neural voices

We specified some neural voices in the NeuralVoices.json file. You can add more neural voices. For a full list of neural voices, see [supported languages](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support#neural-voices).


# Key Source Code

* [NeuralTTS](../Kiosk/Views/NeuralTTS/NeuralTTS.xaml.cs): Main page that drives the demo.

* [NeuralTTSDataLoader](../Kiosk/Views/NeuralTTS/NeuralTTSDataLoader.cs): A class used to save and load cached audio results.

* [NeuralVoices.json](../Kiosk/Views/NeuralTTS/NeuralVoices.json): A JSON file that contains a list of the available neural voices for this demo.

* [Authentication](../Kiosk/Views/NeuralTTS/Authentication.cs): The text-to-speech REST API requires an Authorization header. This class has logic to get a service access token.
