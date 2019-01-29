// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

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

        public Recommendation GetGenericRecommendationForPerson(int age, Gender? gender)
        {
            List<Tuple<int, Recommendation>> recommendationPool;
            if (gender == Gender.Male)
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
