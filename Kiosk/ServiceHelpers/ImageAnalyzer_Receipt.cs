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

using ServiceHelpers.Models.FormRecognizer;
using System;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public partial class ImageAnalyzer
    {
        public AnalyzeResultResponse ReceiptOcrResult { get; set; }

        public async Task AnalyzeReceiptOCRAsync()
        {
            try
            {
                if (this.ImageUrl != null)
                {
                    this.ReceiptOcrResult = await ReceiptOCRHelper.GetReceiptOCRResult(this.ImageUrl);
                }
                else if (this.GetImageStreamCallback != null)
                {
                    this.ReceiptOcrResult = await ReceiptOCRHelper.GetReceiptOCRResult(await this.GetImageStreamCallback());
                }

                ResolveWordsInReceiptFields();
            }
            catch (Exception)
            {
                this.ReceiptOcrResult = new AnalyzeResultResponse();
            }
        }

        private void ResolveWordsInReceiptFields()
        {
            if (this.ReceiptOcrResult?.AnalyzeResult?.DocumentResults != null)
            {
                foreach (ReceiptResult result in this.ReceiptOcrResult.AnalyzeResult.DocumentResults)
                {
                    // Merhant name
                    result.Fields.MerchantName?.UpdateValue();

                    // Merchant address
                    result.Fields.MerchantAddress?.UpdateValue();

                    // Merchant phone
                    result.Fields.MerchantPhoneNumber?.UpdateValue();

                    // Transaction date
                    result.Fields.TransactionDate?.UpdateValue();

                    // Transaction time
                    result.Fields.TransactionTime?.UpdateValue();

                    // Total
                    result.Fields.Total?.UpdateValue();

                    // Subtotal
                    result.Fields.Subtotal?.UpdateValue();

                    // Tax
                    result.Fields.Tax?.UpdateValue();

                    // Tip
                    result.Fields.Tip?.UpdateValue();
                }
            }
        }
    }
}
