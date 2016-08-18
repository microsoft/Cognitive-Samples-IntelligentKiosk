using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.Storage;

namespace IntelligentKioskSample.MallKioskPageConfig
{
    [XmlType]
    public class BehaviorAction
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Url { get; set; }
    }

    [XmlType]
    public class Recommendation
    {
        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Url { get; set; }

        [XmlArrayItem]
        public List<BehaviorAction> SpeechKeywordBehavior { get; set; }

        [XmlArrayItem]
        public List<BehaviorAction> SpeechSentimentBehavior { get; set; }
    }


    [XmlType]
    [XmlRoot]
    public class MallKioskDemoSettings
    {
        private List<Tuple<int, Recommendation>> maleRecommendations;
        private List<Tuple<int, Recommendation>> MaleRecommendations
        {
            get
            {
                if (maleRecommendations == null)
                {
                    maleRecommendations = BuildGenderBasedRecommendations("Male");
                }

                return maleRecommendations;
            }
        }

        private List<Tuple<int, Recommendation>> femaleRecommendations;
        private List<Tuple<int, Recommendation>> FemaleRecommendations
        {
            get
            {
                if (femaleRecommendations == null)
                {
                    femaleRecommendations = BuildGenderBasedRecommendations("Female");
                }

                return femaleRecommendations;
            }
        }

        private List<Tuple<int, Recommendation>> BuildGenderBasedRecommendations(string gender)
        {
            List<Tuple<int, Recommendation>> result = new List<Tuple<int, Recommendation>>();
            string groupPrefix = gender + "YoungerThan";
            foreach (Recommendation r in this.GenericRecommendations.Where(r => r.Id.StartsWith(groupPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                int age;
                if (int.TryParse(r.Id.Substring(groupPrefix.Length, r.Id.Length - groupPrefix.Length), out age))
                {
                    result.Add(new Tuple<int, Recommendation>(age, r));
                }
            }
            Recommendation olderReco = this.GenericRecommendations.FirstOrDefault(r => r.Id.Equals(gender + "Older", StringComparison.OrdinalIgnoreCase));
            if (olderReco != null)
            {
                result.Add(new Tuple<int, Recommendation>(150, olderReco));
            }

            return result;
        }

        public Recommendation GetGenericRecommendationForPerson(int age, string gender)
        {
            List<Tuple<int, Recommendation>> recommendationPool;
            if (gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
            {
                recommendationPool = this.MaleRecommendations;
            }
            else
            {
                recommendationPool = this.FemaleRecommendations;
            }

            Tuple<int, Recommendation> reco = recommendationPool.FirstOrDefault(t => t.Item1 >= age);
            if (reco != null)
            {
                return reco.Item2;
            }

            return null;
        }

        [XmlArrayItem]
        public List<Recommendation> GenericRecommendations { get; set; }

        [XmlArrayItem]
        public List<Recommendation> PersonalizedRecommendations { get; set; }

        public static Task<MallKioskDemoSettings> FromFileAsync(string filePath)
        {
            return Task.Run<MallKioskDemoSettings>(() =>
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    var xs = new XmlSerializer(typeof(MallKioskDemoSettings));
                    MallKioskDemoSettings result = (MallKioskDemoSettings)xs.Deserialize(fileStream);
                    return result;
                }
            });
        }

        public static Task<MallKioskDemoSettings> FromContentAsync(string content)
        {
            return Task.Run<MallKioskDemoSettings>(() =>
            {
                using (TextReader reader = new StringReader(content))
                {
                    var xs = new XmlSerializer(typeof(MallKioskDemoSettings));
                    MallKioskDemoSettings result = (MallKioskDemoSettings)xs.Deserialize(reader);
                    return result;
                }
            });
        }
    }
}
