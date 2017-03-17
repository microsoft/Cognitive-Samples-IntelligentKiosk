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

using IntelligentKioskSample.Views;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace IntelligentKioskSample.Controls
{
    public sealed partial class AgeGenderDistributionControl : UserControl
    {
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(
            "HeaderText",
            typeof(string),
            typeof(AgeGenderDistributionControl),
            new PropertyMetadata("Demographics")
            );

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, (string)value); }
        }

        public static readonly DependencyProperty SubHeaderTextProperty =
            DependencyProperty.Register(
            "SubHeaderText",
            typeof(string),
            typeof(AgeGenderDistributionControl),
            new PropertyMetadata("")
            );

        public string SubHeaderText
        {
            get { return (string)GetValue(SubHeaderTextProperty); }
            set { SetValue(SubHeaderTextProperty, (string)value); }
        }

        public static readonly DependencyProperty SubHeaderVisibilityProperty =
            DependencyProperty.Register(
            "SubHeaderVisibility",
            typeof(Visibility),
            typeof(AgeGenderDistributionControl),
            new PropertyMetadata(Visibility.Collapsed)
            );

        public Visibility SubHeaderVisibility
        {
            get { return (Visibility)GetValue(SubHeaderVisibilityProperty); }
            set { SetValue(SubHeaderVisibilityProperty, (Visibility)value); }
        }

        public AgeGenderDistributionControl()
        {
            this.InitializeComponent();
        }

        public void UpdateData(DemographicsData data)
        {
            int totalPeople = data.OverallFemaleCount + data.OverallMaleCount;

            this.group0to15Bar.Update(data.AgeGenderDistribution.FemaleDistribution.Age0To15,
                                      data.AgeGenderDistribution.MaleDistribution.Age0To15,
                                      (double)(data.AgeGenderDistribution.FemaleDistribution.Age0To15 + data.AgeGenderDistribution.MaleDistribution.Age0To15) / totalPeople);

            this.group16to19Bar.Update(data.AgeGenderDistribution.FemaleDistribution.Age16To19,
                                      data.AgeGenderDistribution.MaleDistribution.Age16To19,
                                      (double)(data.AgeGenderDistribution.FemaleDistribution.Age16To19 + data.AgeGenderDistribution.MaleDistribution.Age16To19) / totalPeople);

            this.group20sBar.Update(data.AgeGenderDistribution.FemaleDistribution.Age20s,
                                      data.AgeGenderDistribution.MaleDistribution.Age20s,
                                      (double)(data.AgeGenderDistribution.FemaleDistribution.Age20s + data.AgeGenderDistribution.MaleDistribution.Age20s) / totalPeople);

            this.group30sBar.Update(data.AgeGenderDistribution.FemaleDistribution.Age30s,
                                      data.AgeGenderDistribution.MaleDistribution.Age30s,
                                      (double)(data.AgeGenderDistribution.FemaleDistribution.Age30s + data.AgeGenderDistribution.MaleDistribution.Age30s) / totalPeople);

            this.group40sBar.Update(data.AgeGenderDistribution.FemaleDistribution.Age40s,
                          data.AgeGenderDistribution.MaleDistribution.Age40s,
                          (double)(data.AgeGenderDistribution.FemaleDistribution.Age40s + data.AgeGenderDistribution.MaleDistribution.Age40s) / totalPeople);

            this.group50sAndOlderBar.Update(data.AgeGenderDistribution.FemaleDistribution.Age50sAndOlder,
                          data.AgeGenderDistribution.MaleDistribution.Age50sAndOlder,
                          (double)(data.AgeGenderDistribution.FemaleDistribution.Age50sAndOlder + data.AgeGenderDistribution.MaleDistribution.Age50sAndOlder) / totalPeople);

            this.overallFemaleTextBlock.Text = data.OverallFemaleCount.ToString();
            this.overallMaleTextBlock.Text = data.OverallMaleCount.ToString();
        }
    }
}
