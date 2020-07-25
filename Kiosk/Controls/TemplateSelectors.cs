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

using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Controls
{
    public class StringTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Template1 { get; set; }
        public string String1 { get; set; }
        public DataTemplate Template2 { get; set; }
        public string String2 { get; set; }
        public DataTemplate Template3 { get; set; }
        public string String3 { get; set; }
        public DataTemplate Template4 { get; set; }
        public string String4 { get; set; }
        public DataTemplate Template5 { get; set; }
        public string String5 { get; set; }
        public DataTemplate Template6 { get; set; }
        public string String6 { get; set; }
        public DataTemplate Template7 { get; set; }
        public string String7 { get; set; }
        public DataTemplate Template8 { get; set; }
        public string String8 { get; set; }
        public DataTemplate Template9 { get; set; }
        public string String9 { get; set; }
        public DataTemplate Template10 { get; set; }
        public string String10 { get; set; }
        public DataTemplate DefaultTemplate { get; set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            //validate
            if (item == null)
            {
                return DefaultTemplate;
            }

            //get string
            var str = item as string;
            if (item.GetType().IsPrimitive || item.GetType().IsEnum)
            {
                str = item.ToString();
            }

            //select template
            if (str == null)
            {
                return DefaultTemplate;
            }
            if (String1 != null && String1.Split(',').Contains(str))
            {
                return Template1;
            }
            if (String2 != null && String2.Split(',').Contains(str))
            {
                return Template2;
            }
            if (String3 != null && String3.Split(',').Contains(str))
            {
                return Template3;
            }
            if (String4 != null && String4.Split(',').Contains(str))
            {
                return Template4;
            }
            if (String5 != null && String5.Split(',').Contains(str))
            {
                return Template5;
            }
            if (String6 != null && String6.Split(',').Contains(str))
            {
                return Template6;
            }
            if (String7 != null && String7.Split(',').Contains(str))
            {
                return Template7;
            }
            if (String8 != null && String8.Split(',').Contains(str))
            {
                return Template8;
            }
            if (String9 != null && String9.Split(',').Contains(str))
            {
                return Template9;
            }
            if (String10 != null && String10.Split(',').Contains(str))
            {
                return Template10;
            }
            return DefaultTemplate;
        }
    }

    public class TypeTemplateSelector : StringTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            //convert type to string
            if (item != null)
            {
                item = item.GetType().Name;
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
