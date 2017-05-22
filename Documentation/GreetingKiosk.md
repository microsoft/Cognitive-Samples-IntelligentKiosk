# Greeting Kiosk

This demo is an example of a hands-free and real-time workflow for identifying when a person in front of the camera matches a known face.

This could be used, for example, as a simple log-in mechanism to greet people by name and present them with some personalized information, like photos, weather, news, etc.

You can use the Face Identification Setup in the Intelligent Kiosk to add or remove faces to the trained model.

The sample calls the APIs at a rate of 1fps to compute the face identification when faces are present in front of the camera (detected via the CameraControl), and while no faces 
are present it keeps checking for changes at 10fps.

# Key Source Code

* [GreetingKiosk](../Kiosk/Views/GreetingKiosk.xaml.cs): Main page that drives the demo. It hosts the CameraControl (see below) to display the live camera feed, creates a background loop to capture and process frames from the camera. It then uses the result to greet the person with a message.

* [CameraControl](../Kiosk/Controls/CameraControl.xaml.cs): The code that contains the camera feed and runs a background loop to perform several tasks (track faces and draw face rectangles).

