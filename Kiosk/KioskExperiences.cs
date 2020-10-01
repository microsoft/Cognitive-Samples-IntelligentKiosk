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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Windows.Storage;

namespace IntelligentKioskSample
{
    public static class KioskExperiences
    {
        private static readonly Type[] expTypes;

        static KioskExperiences()
        {
            expTypes = typeof(KioskExperiences).GetTypeInfo().Assembly.GetTypes().Where(t =>
                t.Namespace != null && t.Namespace.StartsWith("IntelligentKioskSample.Views")
                && t.GetTypeInfo().GetCustomAttribute<KioskExperienceAttribute>() != null)
                .ToArray();
        }

        public static IEnumerable<KioskExperience> Experiences
        {
            get
            {
                var experiences = expTypes.Select(t => new KioskExperience()
                {
                    PageType = t,
                    Attributes = t.GetTypeInfo().GetCustomAttribute<KioskExperienceAttribute>()
                })
                .Where(e => !string.IsNullOrEmpty(e.Attributes.RequiredFile)
                                ? Util.FileExists(ApplicationData.Current.LocalFolder, e.Attributes.RequiredFile) // Hide specific experiences
                                : true)
                .Where(e => e.Attributes.IsPublic)
                .ToList();

                return experiences.OrderBy(e => e.Attributes.DisplayName);
            }
        }
    }

    public class KioskExperience
    {
        public Type PageType { get; set; }
        public KioskExperienceAttribute Attributes { get; set; }
    }

    [Flags]
    public enum ExperienceType
    {
        Automated = 1,
        Guided = 2,
        Business = 4,
        IntelligentEdge = 8,
        Fun = 16,
        Experimental = 32,
        Preview = 64
    }

    [Flags]
    public enum TechnologyAreaType
    {
        Vision = 1,
        Speech = 2,
        Search = 8,
        Language = 16,
        Decision = 32
    }

    [Flags]
    public enum TechnologyType
    {
        Face = 1,
        Emotion = 2,
        Vision = 4,
        TextAnalytics = 8,
        BingNews = 16,
        BingImages = 32,
        BingAutoSuggest = 64,
        Bots = 128,
        SpeechToText = 256,
        CustomVision = 512,
        QnA = 1024,
        WinML = 2048,
        TextToSpeech = 4096,
        CognitiveSearch = 8192,
        TranslatorText = 16384,
        AnomalyDetector = 32768,
        FormRecognizer = 131072
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class KioskExperienceAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
        public string ImagePath { get; set; }
        public ExperienceType ExperienceType { get; set; }
        public TechnologyAreaType TechnologyArea { get; set; }
        public TechnologyType TechnologiesUsed { get; set; }
        public bool IsPublic { get; set; } = true;
        public string DateAdded { get; set; }
        public string DateUpdated { get; set; }
        public string UpdatedDescription { get; set; }
        public string RequiredFile { get; set; }
    }
}
