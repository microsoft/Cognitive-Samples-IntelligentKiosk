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

using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Collections.ObjectModel;
using System.Data;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace IntelligentKioskSample.Controls
{
    public sealed partial class DataGridControl : UserControl
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                "Source",
                typeof(DataTable),
                typeof(DataGridControl),
                new PropertyMetadata(new DataTable(), OnSourceChanged));

        public DataTable Source
        {
            get { return (DataTable)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public DataGridControl()
        {
            this.InitializeComponent();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridControl)d).OnSourceChanged();
        }

        private void OnSourceChanged()
        {
            this.dataGrid.Columns.Clear();
            for (int i = 0; i < Source.Columns.Count; i++)
            {
                this.dataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = Source.Columns[i].ColumnName,
                    Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") },
                    FontSize = 12
                });
            }

            var collection = new ObservableCollection<object>();
            foreach (DataRow row in Source.Rows)
            {
                collection.Add(row.ItemArray);
            }

            this.dataGrid.ItemsSource = collection;
        }
    }
}
