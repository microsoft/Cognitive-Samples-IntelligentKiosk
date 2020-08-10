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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceHelpers.Models.FormRecognizer
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ReceiptFieldType
    {
        [EnumMember(Value = "string")]
        String,
        [EnumMember(Value = "number")]
        Number,
        [EnumMember(Value = "phoneNumber")]
        PhoneNumber,
        [EnumMember(Value = "date")]
        Date,
        [EnumMember(Value = "time")]
        Time,
        [EnumMember(Value = "array")]
        Array
    }

    public class ReceiptResult
    {
        public string DocType { get; set; }
        public IList<int> PageRange { get; set; }
        public ReceiptFields Fields { get; set; }
    }

    public class ReceiptFields
    {
        public ReceiptFieldData ReceiptType { get; set; }
        public ReceiptFieldData MerchantName { get; set; }
        public ReceiptFieldData MerchantAddress { get; set; }
        public ReceiptFieldData MerchantPhoneNumber { get; set; }
        public ReceiptFieldData TransactionDate { get; set; }
        public ReceiptFieldData TransactionTime { get; set; }
        public ReceiptFieldData Subtotal { get; set; }
        public ReceiptFieldData Tax { get; set; }
        public ReceiptFieldData Tip { get; set; }
        public ReceiptFieldData Total { get; set; }
        public ReceiptItems Items { get; set; }
    }

    public class ReceiptFieldData
    {
        public ReceiptFieldType Type { get; set; }
        public string ValueString { get; set; }
        public double? ValueNumber { get; set; }
        public string ValuePhoneNumber { get; set; }
        public string ValueDate { get; set; }
        public string ValueTime { get; set; }
        public string Text { get; set; }
        public IList<double?> BoundingBox { get; set; }
        public int Page { get; set; }
        public double Confidence { get; set; }

        public string Value { get; private set; }
        public void UpdateValue()
        {
            switch (Type)
            {
                case ReceiptFieldType.Number:
                    Value = ValueNumber?.ToString() ?? string.Empty;
                    break;

                case ReceiptFieldType.PhoneNumber:
                    Value = ValuePhoneNumber;
                    break;

                case ReceiptFieldType.Date:
                    Value = ValueDate;
                    break;

                case ReceiptFieldType.Time:
                    Value = ValueTime;
                    break;

                case ReceiptFieldType.String:
                default:
                    Value = ValueString;
                    break;
            }
        }
    }

    public class ReceiptItems
    {
        public string Type { get; set; }
        public IList<ReceiptItemValue> ValueArray { get; set; }
    }

    public class ReceiptItemValue
    {
        public string Type { get; set; }
        public ReceiptItemObject ValueObject { get; set; }
    }

    public class ReceiptItemObject
    {
        public ReceiptFieldData Quantity { get; set; }
        public ReceiptFieldData Name { get; set; }
        public ReceiptFieldData TotalPrice { get; set; }
    }
}
