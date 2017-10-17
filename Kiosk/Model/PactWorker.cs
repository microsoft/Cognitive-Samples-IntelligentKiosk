
namespace IntelligentKioskSample.Model
{
    public class PactWorker
    {
        public PactWorker(string v1, string v2, string v3, int auth, int inf, string obj)
        {
            Name = v1;
            Role = v2;
            MobileNo = v3;
            Authorization = auth;
            Infringements = inf;
            Objects = obj;
        }


        public int Authorization { get; set; }
        public int Infringements { get; set; }
        public string Objects { get; set; }

        public bool CorrectGear { get; set; }

        public string MobileNo { get; set; }

        public string Name { get; set; }
        public string Role { get; set; }
    }
}