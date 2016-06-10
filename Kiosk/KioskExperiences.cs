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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample
{
    public static class KioskExperiences
    {
        private static readonly Type[] expTypes;

        static KioskExperiences()
        {
            expTypes = typeof(KioskExperiences).GetTypeInfo().Assembly.GetTypes().Where(t =>
                t.Namespace == "IntelligentKioskSample.Views"
                && t.GetTypeInfo().GetCustomAttribute<KioskExperienceAttribute>() != null)
                .ToArray();
        }

        public static IEnumerable<KioskExperience> Experiences
        {
            get
            {
                return expTypes.Select(t => new KioskExperience()
                {
                    PageType = t,
                    Attributes = t.GetTypeInfo().GetCustomAttribute<KioskExperienceAttribute>()
                }).OrderBy(e => e.Attributes.Title);
            }
        }
    }

    public class KioskExperience
    {
        public Type PageType { get; set; }
        public KioskExperienceAttribute Attributes { get; set; }
    }

    public enum ExperienceType
    {
        Kiosk,
        Other
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class KioskExperienceAttribute : Attribute
    {
        public string Title { get; set; }
        public string ImagePath { get; set; }
        public ExperienceType ExperienceType { get; set; }
    }
}
