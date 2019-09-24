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

using IntelligentKioskSample.Models;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Controls
{
    public enum LifecycleStepState
    {
        Mute,
        Active,
        Completed
    }

    public sealed partial class LifecycleControl : UserControl
    {
        public static readonly DependencyProperty StepCollectionProperty =
            DependencyProperty.Register(
            "StepCollection",
            typeof(ObservableCollection<LifecycleStepViewModel>),
            typeof(LifecycleControl),
            new PropertyMetadata(null));

        public ObservableCollection<LifecycleStepViewModel> StepCollection
        {
            get { return (ObservableCollection<LifecycleStepViewModel>)GetValue(StepCollectionProperty); }
            set { SetValue(StepCollectionProperty, value); }
        }

        public LifecycleControl()
        {
            this.InitializeComponent();
        }

        public void ResetState(bool clearAll = false)
        {
            foreach (var step in StepCollection)
            {
                if (clearAll)
                {
                    step.Subtitle = string.Empty;
                }

                step.State = LifecycleStepState.Mute;
            }
        }
    }

    public class LifecycleStepViewModel : BaseViewModel
    {
        private string title = string.Empty;
        private string subtitle = string.Empty;
        private LifecycleStepState state = LifecycleStepState.Mute;

        public string Id { get; set; }
        public string Title { get => title; set => Set(ref title, value); }
        public string Subtitle { get => subtitle; set => Set(ref subtitle, value); }
        public LifecycleStepState State { get => state; set => Set(ref state, value); }
        public bool IsLast { get; set; } = false;
    }
}
