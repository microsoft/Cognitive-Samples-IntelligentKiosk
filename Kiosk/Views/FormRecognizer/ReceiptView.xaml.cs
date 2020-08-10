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

using IntelligentKioskSample.Controls.Overlays;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceHelpers;
using ServiceHelpers.Models.FormRecognizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace IntelligentKioskSample.Views.FormRecognizer
{
    public sealed partial class ReceiptView : UserControl, INotifyPropertyChanged
    {
        private Dictionary<Uri, ImageAnalyzer> receiptImageCache = new Dictionary<Uri, ImageAnalyzer>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ReceiptResultUI receipt;
        public ReceiptResultUI Receipt
        {
            get { return receipt; }
            set
            {
                receipt = value;
                NotifyPropertyChanged();
            }
        }

        private string json;
        public string Json
        {
            get { return json; }
            set
            {
                json = value;
                NotifyPropertyChanged();
            }
        }

        public ReceiptView()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        public async Task AnalyzeReceiptAsync(Uri uri, StorageFile file)
        {
            //get image analyzer
            ImageAnalyzer image = null;
            if (receiptImageCache.ContainsKey(uri))
            {
                image = receiptImageCache[uri];
            }
            else
            {
                if (uri.IsFile)
                {
                    file = file ?? await StorageFile.GetFileFromPathAsync(uri.LocalPath);
                    image = new ImageAnalyzer(file.OpenStreamForReadAsync);
                }
                else
                {
                    image = new ImageAnalyzer(uri.AbsoluteUri);
                }
                receiptImageCache.Add(uri, image);
            }

            await AnalyzeReceiptAsync(image);
        }

        public async Task AnalyzeReceiptAsync(ImageAnalyzer image)
        {
            //update UI
            Receipt = null;
            image.ShowDialogOnFaceApiErrors = true;
            progressControl.IsActive = true;
            OverlayPresenter.ItemsSource = null;
            OverlayPresenter.Source = await image.GetImageSource();

            //analyze receipt
            var receipt = image.ReceiptOcrResult?.AnalyzeResult?.DocumentResults?.FirstOrDefault();
            if (receipt == null)
            {
                await image.AnalyzeReceiptOCRAsync();
            }
            receipt = image.ReceiptOcrResult?.AnalyzeResult?.DocumentResults?.FirstOrDefault();
            Receipt = new ReceiptResultUI()
            {
                MerchantAddress = GetReceiptItemUI(receipt.Fields.MerchantAddress),
                MerchantPhoneNumber = GetReceiptItemUI(receipt.Fields.MerchantPhoneNumber),
                MerchantName = GetReceiptItemUI(receipt.Fields.MerchantName),
                Subtotal = GetReceiptItemUI(receipt.Fields.Subtotal),
                Tax = GetReceiptItemUI(receipt.Fields.Tax),
                Tip = GetReceiptItemUI(receipt.Fields.Tip),
                Total = GetReceiptItemUI(receipt.Fields.Total),
                TransactionDate = GetReceiptItemUI(receipt.Fields.TransactionDate),
                TransactionTime = GetReceiptItemUI(receipt.Fields.TransactionTime),
                Items = GetReceiptFieldsUI(receipt.Fields.Items)
            };
            var itemsQuantityOverlayInfo = Receipt.Items.Where(i => i.Quantity != null).SelectMany(i => i.Quantity.OverlayInfo);
            var itemsNameOverlayInfo = Receipt.Items.Where(i => i.Name != null).SelectMany(i => i.Name.OverlayInfo);
            var itemsTotalPriceOverlayInfo = Receipt.Items.Where(i => i.TotalPrice != null).SelectMany(i => i.TotalPrice.OverlayInfo);

            OverlayPresenter.ItemsSource = Receipt.MerchantAddress.OverlayInfo.Concat
                (Receipt.MerchantPhoneNumber.OverlayInfo).Concat
                (Receipt.MerchantName.OverlayInfo).Concat
                (Receipt.Subtotal.OverlayInfo).Concat
                (Receipt.Tax.OverlayInfo).Concat
                (Receipt.Tip.OverlayInfo).Concat
                (Receipt.Total.OverlayInfo).Concat
                (Receipt.TransactionDate.OverlayInfo).Concat
                (Receipt.TransactionTime.OverlayInfo).Concat
                (itemsQuantityOverlayInfo).Concat
                (itemsNameOverlayInfo).Concat
                (itemsTotalPriceOverlayInfo).ToArray();

            //set formated Json
            Json = JValue.Parse(image.ReceiptOcrResult.RawResponse).ToString(Formatting.Indented);

            //update UI
            ShowNotFoundReceiptFields();
            progressControl.IsActive = false;
        }

        ReceiptItemUI GetReceiptItemUI(ReceiptFieldData field)
        {
            return new ReceiptItemUI()
            {
                ReceiptItem = field,
                OverlayInfo = field != null ? new TextOverlayInfo[] { new TextOverlayInfo(field.Text, field.BoundingBox) { IsMuted = true } } : new TextOverlayInfo[] { }
            };
        }

        IList<ReceiptFieldUI> GetReceiptFieldsUI(ReceiptItems items)
        {
            var fields = new List<ReceiptFieldUI>();
            if (items?.ValueArray != null)
            {
                foreach (ReceiptItemValue item in items.ValueArray.Where(i => i.ValueObject != null))
                {
                    fields.Add(new ReceiptFieldUI()
                    {
                        Quantity = GetReceiptItemUI(item.ValueObject.Quantity),
                        Name = GetReceiptItemUI(item.ValueObject.Name),
                        TotalPrice = GetReceiptItemUI(item.ValueObject.TotalPrice),
                    });
                }
            }
            return fields;
        }

        private void ShowNotFoundReceiptFields()
        {
            var notFoundFields = new List<string>()
            {
                string.IsNullOrEmpty(this.nameFieldControl.FieldValue) ?     "Merchant" : string.Empty,
                string.IsNullOrEmpty(this.addressFieldControl.FieldValue) ?  "Address" : string.Empty,
                string.IsNullOrEmpty(this.phoneFieldControl.FieldValue) ?    "Phone Number" : string.Empty,
                string.IsNullOrEmpty(this.dateFieldControl.FieldValue) ?     "Date" : string.Empty,
                string.IsNullOrEmpty(this.timeFieldControl.FieldValue) ?     "Time" : string.Empty,
                string.IsNullOrEmpty(this.totalFieldControl.FieldValue) ?    "Total" : string.Empty,
                string.IsNullOrEmpty(this.subtotalFieldControl.FieldValue) ? "Subtotal" : string.Empty,
                string.IsNullOrEmpty(this.taxFieldControl.FieldValue) ?      "Tax" : string.Empty,
                string.IsNullOrEmpty(this.tipFieldControl.FieldValue) ?      "Tip" : string.Empty,
                this.receiptItems.Items.Count == 0 ?                         "Receipt Items" : string.Empty
            };

            if (notFoundFields.Any(x => x.Length > 0))
            {
                this.notFoundFieldsTextBlock.Text = string.Join(", ", notFoundFields.Where(x => x.Length > 0).ToArray());
                this.notFoundPanel.Visibility = Visibility.Visible;
            }
            else
            {
                this.notFoundPanel.Visibility = Visibility.Collapsed;
            }

            this.fieldsPanel.Visibility = notFoundFields.All(x => x.Length > 0) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public class ReceiptResultUI
    {
        public ReceiptItemUI TransactionDate { get; set; }
        public ReceiptItemUI TransactionTime { get; set; }
        public ReceiptItemUI MerchantName { get; set; }
        public ReceiptItemUI MerchantAddress { get; set; }
        public ReceiptItemUI MerchantPhoneNumber { get; set; }
        public ReceiptItemUI Tax { get; set; }
        public ReceiptItemUI Tip { get; set; }
        public ReceiptItemUI Total { get; set; }
        public ReceiptItemUI Subtotal { get; set; }
        public IList<ReceiptFieldUI> Items { get; set; }
    }

    public class ReceiptFieldUI
    {
        public ReceiptItemUI Quantity { get; set; }
        public ReceiptItemUI Name { get; set; }
        public ReceiptItemUI TotalPrice { get; set; }
    }

    public class ReceiptItemUI
    {
        public ReceiptFieldData ReceiptItem { get; set; }
        public IEnumerable<TextOverlayInfo> OverlayInfo { get; set; }
    }
}
