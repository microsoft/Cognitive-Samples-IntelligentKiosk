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

using ServiceHelpers;
using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace IntelligentKioskSample.Controls
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }
    }

    public class ReverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Collapsed ? true : false;
        }
    }

    public class ReverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (Visibility)value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (Visibility)value;
        }
    }

    public class ReverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class StringMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null &&
                string.Equals(value.ToString(), (string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseStringMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null &&
                string.Equals(value.ToString(), (string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class StringMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null && string.Equals(value.ToString(), (string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseStringMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null && string.Equals(value.ToString(), (string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class StringContainsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null &&
                ((string)value).Contains((string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class StringFormatConverter : IValueConverter
    {
        public string StringFormat { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var format = parameter as string;
            if (format == null)
            {
                format = StringFormat;
            }
            if (!string.IsNullOrWhiteSpace(format) && value != null)
            {
                return String.Format(format, value);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class StringLengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && !string.IsNullOrEmpty((string)value))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseStringLengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || (value is string && string.IsNullOrEmpty((string)value)))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class CollectionCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            //get min value
            int.TryParse(parameter != null ? (string)parameter : string.Empty, out int minValue);

            //get count
            var count = 0;
            if (value is int)
            {
                count = (int)value;
            }
            else
            {
                var collection = value as ICollection;
                if (collection != null)
                {
                    count = collection.Count;
                }
            }

            return count > minValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseCollectionCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            //get min value
            int.TryParse(parameter != null ? (string)parameter : string.Empty, out int minValue);

            //get count
            var count = 0;
            if (value is int)
            {
                count = (int)value;
            }
            else
            {
                var collection = value as ICollection;
                if (collection != null)
                {
                    count = collection.Count;
                }
            }

            return count > minValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class EnumMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null || !(value is Enum))
            {
                return Visibility.Collapsed;
            }

            var currentState = value.ToString();
            var stateStrings = parameter.ToString();

            if (string.Equals(currentState, stateStrings, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseEnumMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null || !(value is Enum))
            {
                return Visibility.Collapsed;
            }

            var currentState = value.ToString();
            var stateStrings = parameter.ToString();

            if (!string.Equals(currentState, stateStrings, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return 0;
            }
            if (int.TryParse(value as string, out var number))
            {
                return number;
            }
            return 0;
        }
    }

    public class ImageAnalyzerToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var imageAnalyzer = value as ImageAnalyzer;
            if (imageAnalyzer != null)
            {
                return imageAnalyzer.GetImageSource().Result;
            }
            return value; //no convertion
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MathConverter : IValueConverter
    {
        public double Add { get; set; }
        public double Multiply { get; set; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                var number = System.Convert.ToDouble(value);
                return (number + Add) * Multiply;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                var number = System.Convert.ToDouble(value);
                return (number / Multiply) - Add;
            }
            return value;
        }
    }

    public class BooleanToIntConverter : IValueConverter
    {
        public int IfTrue { get; set; } = 1;
        public int IfFalse { get; set; } = 0;
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            return (bool)value ? IfTrue : IfFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((int)value) == IfTrue ? true : false;
        }
    }
}