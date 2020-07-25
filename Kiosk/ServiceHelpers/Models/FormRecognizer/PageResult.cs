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

using System.Collections.Generic;

namespace ServiceHelpers.Models.FormRecognizer
{
    public class PageResult
    {
        public int Page { get; set; }
        public int? ClusterId { get; set; }
        public IList<KeyValuePair> KeyValuePairs { get; set; }
        public IList<TableResult> Tables { get; set; }
    }

    public class KeyValuePair
    {
        public WordResult Key { get; set; }
        public WordResult Value { get; set; }
        public double Confidence { get; set; }
    }

    public class TableResult
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public IList<TableCellResult> Cells { get; set; }
    }

    public class TableCellResult
    {
        public string Text { get; set; }
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public IList<double?> BoundingBox { get; set; }
        public double Confidence { get; set; }
        public int RowSpan { get; set; }
        public int ColumnSpan { get; set; }
        public bool IsHeader { get; set; }
        public bool IsFooter { get; set; }
    }
}
