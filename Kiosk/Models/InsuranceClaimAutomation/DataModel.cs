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

using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media.Imaging;

namespace IntelligentKioskSample.Models.InsuranceClaimAutomation
{
    public enum InputViewState
    {
        NotSelected,
        Selection,
        Selected
    }

    public enum InputValidationStatus
    {
        Unknown,
        Valid,
        Invalid
    }

    public enum CustomFormFieldType
    {
        CustomName,
        Date,
        WarrantyId,
        WarrantyAmount,
        Total
    }

    public class DataGridViewModel : BaseViewModel
    {
        private TokenOverlayInfo customName;
        private TokenOverlayInfo date;
        private TokenOverlayInfo warrantyId;
        private TokenOverlayInfo warranty;
        private TokenOverlayInfo invoiceTotal;

        private InputValidationStatus formValidStatus = InputValidationStatus.Unknown;
        private InputValidationStatus productImageValidStatus = InputValidationStatus.Unknown;

        public Guid Id { get; private set; }

        public int ClaimId { get; set; }

        [JsonIgnore]
        public BitmapImage ProductImage { get; set; }

        public string ProductImageUri { get; set; }

        [JsonIgnore]
        public BitmapImage FormImage { get; set; }

        public Uri FormFile { get; set; }

        public string FormImageUri { get; set; }

        public bool IsFormImage { get; set; }

        public List<PredictionModel> ObjectDetectionMatches { get; set; }

        public List<PredictionModel> ObjectClassificationMatches { get; set; }

        public DataGridViewModel(Guid id)
        {
            Id = id;
        }

        public TokenOverlayInfo CustomName { get => customName; set => Set(ref customName, value); }
        public TokenOverlayInfo Date { get => date; set => Set(ref date, value); }
        public TokenOverlayInfo WarrantyId { get => warrantyId; set => Set(ref warrantyId, value); }
        public TokenOverlayInfo Warranty { get => warranty; set => Set(ref warranty, value); }
        public TokenOverlayInfo InvoiceTotal { get => invoiceTotal; set => Set(ref invoiceTotal, value); }
        public InputValidationStatus ProductImageValidStatus { get => productImageValidStatus; set => Set(ref productImageValidStatus, value); }
        public InputValidationStatus FormValidStatus { get => formValidStatus; set => Set(ref formValidStatus, value); }
    }
}
