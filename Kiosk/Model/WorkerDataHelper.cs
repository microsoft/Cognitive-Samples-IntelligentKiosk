using System.Collections.Generic;

namespace IntelligentKioskSample.Model
{
    public class WorkerDataHelper
    {
        public WorkerDataHelper() { }

        public List<PactWorker> Initialise()
        {
            //if the name created on the ADmin portal matches the dummmy data it will link the face with this data (simulation of hooking it up to an existing AD
            List<PactWorker> list = new List<PactWorker>();
            list.Add(new PactWorker("Bec Lyons", "TSP", "123456789", 3, 0, "Jackhammer, Saw"));
            list.Add(new PactWorker("Worker 1", "Board 1", "123456789", 1, 0, "Unauthorised to use objects"));
            list.Add(new PactWorker("Jane Doe", "Board 2", "123456789", 2, 0, "Unauthorised to use objects."));
            list.Add(new PactWorker("John Smith", "Board 3", "123456789", 2, 0, "Unauthorised to use objects"));
            list.Add(new PactWorker("Joe Bloggs", "Board 4", "123456789", 2, 0, "Unauthorised to use objects"));
            list.Add(new PactWorker("Adrian Plunket", "Board 5", "123456789", 1, 0, "Unauthorised to use objects"));
            list.Add(new PactWorker("Olivia Potato", "Board 6", "123456789", 2, 0, "Unauthorised to use objects"));
            list.Add(new PactWorker("Rick Roll", "Board 7", "123456789", 2, 0, "Unauthorised to use objects"));
            list.Add(new PactWorker("Bruce Wayne", "Batman", "123456789", 3, 23, "Batmobile, Baterang, Smoke bomb"));
            list.Add(new PactWorker("Ryan Preece", "Consultant", "0403 123 456", 3, 0, "Visual Studio, UWP, HoloLens"));
            list.Add(new PactWorker("Safe Worker", "Model", "0403 123 456", 3, 0, "Jackhammer, Saw"));

            return list;
        }
    }
}
