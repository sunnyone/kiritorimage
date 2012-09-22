/*
 * Copyright (c) 2012 Yoichi Imai, All rights reserved.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace KiritoriMage
{
    public class ImageViewModel : ViewModelBase
    {
        BitmapSource loadedBitmap;
        public BitmapSource LoadedBitmap
        { 
            get {
                return loadedBitmap;
            }
            set {
                loadedBitmap = value;

                if (loadedBitmap == null) {
                    HorizontalSplitPositionBarViewModel.MaxPosition = 0;
                    VerticalSplitPositionBarViewModel.MaxPosition = 0;
                } else {
                    HorizontalSplitPositionBarViewModel.MaxPosition = loadedBitmap.PixelWidth;
                    VerticalSplitPositionBarViewModel.MaxPosition = loadedBitmap.PixelHeight;
                }

                OnPropertyChanged("LoadedBitmap");
            }
        }
        public string LoadedBitmapFileName { get; set; }

        public SplitPositionBarViewModel HorizontalSplitPositionBarViewModel { get; set; }
        public SplitPositionBarViewModel VerticalSplitPositionBarViewModel { get; set; }
        public ObservableCollection<PointInt> MarkedPointList { get; set; }

        public PointInt CurrentPositon { get; set; }

        public int LoadedBitmapWidth
        {
            get
            {
                return (this.LoadedBitmap == null) ? 0 : this.LoadedBitmap.PixelWidth;
            }
        }

        public int LoadedBitmapHeight
        {
            get
            {
                return (this.LoadedBitmap == null) ? 0 : this.LoadedBitmap.PixelHeight;
            }
        }

        public ImageViewModel()
        {
            HorizontalSplitPositionBarViewModel = new SplitPositionBarViewModel();
            VerticalSplitPositionBarViewModel = new SplitPositionBarViewModel();

            HorizontalSplitPositionBarViewModel.AnyPositionChanged += new EventHandler((sender, e) =>
                { OnRangeDataUpdated(); });
            VerticalSplitPositionBarViewModel.AnyPositionChanged += new EventHandler((sender, e) =>
                { OnRangeDataUpdated(); });

            CurrentPositon = new PointInt(0, 0);

            MarkedPointList = new ObservableCollection<PointInt>();
            ShowCursorLine = true;
        }

        public event EventHandler RangeDataUpdated;

        void OnRangeDataUpdated()
        {
            if (RangeDataUpdated != null)
            {
                RangeDataUpdated(this, new EventArgs());
            }
        }

        // FIXME: this is slow.
        public List<Int32Rect?> GetSelectedRegionList()
        {
            int maxX = this.LoadedBitmapWidth;
            int maxY = this.LoadedBitmapHeight;

            int[] xSplitPosArray = this.HorizontalSplitPositionBarViewModel.GetPositionArraySorted();
            int[] ySplitPosArray = this.VerticalSplitPositionBarViewModel.GetPositionArraySorted();

            return (from p
                    in this.MarkedPointList
                    select Util.SearchRegionFromArray(p.X, p.Y, xSplitPosArray, ySplitPosArray, maxX, maxY)).ToList();
        }

        public bool ShowCursorLine { get; set; }

        public void ClearPositions()
        {
            this.VerticalSplitPositionBarViewModel.PositionList.Clear();
            this.HorizontalSplitPositionBarViewModel.PositionList.Clear();
            this.MarkedPointList.Clear();
        }
    }
}
