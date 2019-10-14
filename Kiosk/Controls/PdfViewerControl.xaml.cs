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
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace IntelligentKioskSample.Controls
{
    public sealed partial class PdfViewerControl : UserControl
    {
        PageCacheManager _pageCache;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                "Source",
                typeof(Uri),
                typeof(PdfViewerControl),
                new PropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty EnableScrollControlsProperty =
            DependencyProperty.Register(
                "EnableScrollControls",
                typeof(bool),
                typeof(PdfViewerControl),
                new PropertyMetadata(true));

        public int BufferPageCount { get; set; } = 2;

        public ObservableCollection<PdfViewerPage> PdfPages { get; set; }

        public PdfViewerControl()
        {
            this.InitializeComponent();

            //set fields
            PdfPages = new ObservableCollection<PdfViewerPage>();

            //set event handlers
            ((INotifyCollectionChanged)PdfPages).CollectionChanged += CollectionChanged;
            Carousel.SelectionChanged += Carousel_SelectionChanged;
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        private async void Carousel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
            await _pageCache?.LoadSelectedPage(Carousel.SelectedIndex);
        }

        void UpdateButtons()
        {
            //enable movement buttons
            MoveForward.IsEnabled = Carousel.SelectedIndex < Carousel.Items.Count - 1;
            MoveBack.IsEnabled = Carousel.SelectedIndex > 0;
        }

        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public bool EnableScrollControls
        {
            get { return (bool)GetValue(EnableScrollControlsProperty); }
            set { SetValue(EnableScrollControlsProperty, value); }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PdfViewerControl)d).OnSourceChanged();
        }

        async void OnSourceChanged()
        {
            await LoadAsync();
        }

        bool IsWebUri(Uri uri)
        {
            if (uri != null)
            {
                var str = uri.ToString().ToLower();
                return str.StartsWith("http://") || str.StartsWith("https://");
            }
            return false;
        }

        public async Task LoadAsync()
        {
            if (Source == null)
            {
                PdfPages.Clear();
            }
            else
            {
                if (Source.IsFile || !IsWebUri(Source))
                {
                    await LoadFromLocalAsync();
                }
                else if (IsWebUri(Source))
                {
                    await LoadFromRemoteAsync();
                }
                else
                {
                    throw new ArgumentException($"Source '{Source.ToString()}' could not be recognized!");
                }
            }
        }

        private async Task LoadFromRemoteAsync()
        {
            HttpClient client = new HttpClient();
            var stream = await client.GetStreamAsync(Source);
            var memStream = new MemoryStream();
            await stream.CopyToAsync(memStream);
            memStream.Position = 0;
            PdfDocument imageDoc = await PdfDocument.LoadFromStreamAsync(memStream.AsRandomAccessStream());
            CreatePages(imageDoc);
        }

        private async Task LoadFromLocalAsync()
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(Source.LocalPath);
            PdfDocument imageDoc = await PdfDocument.LoadFromStreamAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());
            CreatePages(imageDoc);
        }

        private async void CreatePages(PdfDocument imageDoc)
        {
            PdfPages.Clear();
            for (int i = 0; i < imageDoc.PageCount; i++)
            {
                PdfPages.Add(new PdfViewerPage());
            }
            _pageCache = new PageCacheManager(PdfPages, imageDoc) { Prefetch = BufferPageCount };
            await _pageCache.LoadSelectedPage(Carousel.SelectedIndex);
        }

        private void Forward(object sender, RoutedEventArgs e)
        {
            Carousel.SelectedIndex++;
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            Carousel.SelectedIndex--;
        }

        class PageCacheManager
        {
            IList<PdfViewerPage> _pages;
            PdfDocument _imageDoc;
            int _lastSelectedIndex;

            public int Prefetch { get; set; } = 2;

            public PageCacheManager(IList<PdfViewerPage> pages, PdfDocument imageDoc)
            {
                //set fields
                _pages = pages;
                _imageDoc = imageDoc;
            }

            public async Task LoadSelectedPage(int selectedIndex)
            {
                //calculate ranges
                var cacheRange = GetRange(selectedIndex);
                var expireRange = GetRange(_lastSelectedIndex);
                expireRange = GetRangeDiff(cacheRange, expireRange);
                _lastSelectedIndex = selectedIndex;

                //expire cached items out of range
                for (int pageIndex = expireRange.Start; pageIndex <= expireRange.End; pageIndex++)
                {
                    var page = _pages[pageIndex];
                    if (page.Image != null)
                    {
                        page.Image = null;
                    }
                }

                //cache new items
                for (int pageIndex = cacheRange.Start; pageIndex <= cacheRange.End; pageIndex++)
                {
                    var page = _pages[pageIndex];
                    if (page.Image == null)
                    {
                        //render page as image
                        var imagePage = _imageDoc.GetPage((uint)pageIndex);
                        using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                        {
                            await imagePage.RenderToStreamAsync(stream);
                            var image = new BitmapImage();
                            await image.SetSourceAsync(stream);
                            page.Image = image;
                        }
                    }
                }
            }

            (int Start, int End) GetRange(int value)
            {
                var start = value - Prefetch;
                if (start < 0)
                {
                    start = 0;
                }

                var end = value + Prefetch;
                if (end >= _pages.Count)
                {
                    end = _pages.Count - 1;
                }

                return (start, end);
            }

            (int Start, int End) GetRangeDiff((int Start, int End) current, (int Start, int End) prev)
            {
                //same
                if (current.Start == prev.Start || current.End == prev.End)
                {
                    return (0, -1); //no range
                }

                //greater than overlap
                if (current.Start > prev.Start && current.Start <= prev.End)
                {
                    return (prev.Start, current.Start - 1);
                }

                //less than overlap
                if (current.End >= prev.Start && current.End < prev.End)
                {
                    return (current.End + 1, prev.End);
                }

                //no overlap
                return prev;
            }
        }
    }

    public class PdfViewerPage : BaseViewModel
    {
        BitmapImage _image;

        public BitmapImage Image { get => _image; set => Set(ref _image, value); }
    }

    public class SimpleCarousel : Carousel
    {
        public SimpleCarousel()
        {
            //remove mouse wheel scrolling
            var wheelMethod = typeof(Carousel).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).First(i => i.Name == "OnPointerWheelChanged");
            var wheelDelegate = wheelMethod.CreateDelegate(typeof(PointerEventHandler), this) as PointerEventHandler;
            PointerWheelChanged -= wheelDelegate;
        }
    }
}
