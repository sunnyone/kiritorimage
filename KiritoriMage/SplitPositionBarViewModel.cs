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
using System.Collections.ObjectModel;

namespace KiritoriMage
{
    public class SplitPositionBarViewModel : ViewModelBase
    {
        ObservableCollection<SplitPositionItem> positionList = new ObservableCollection<SplitPositionItem>();
        public ObservableCollection<SplitPositionItem> PositionList
        {
            get { return positionList; }
            set { positionList = value; OnPropertyChanged("PositionList"); }
        }

        public int[] GetPositionArraySorted()
        {
            return (from item
                    in this.PositionList
                    orderby item.Position
                    select item.Position).ToArray();
        }

        public int MaxPosition { get; set; }
        public int MinPosition { get { return 0; } }

        public event EventHandler AnyPositionChanged;

        void OnAnyPositionChanged() {
            if (AnyPositionChanged != null) {
                AnyPositionChanged(this, new EventArgs());
            }
        }

        public SplitPositionBarViewModel()
        {
            positionList.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(positionList_CollectionChanged);
        }

        public void CheckAndRemoveBorderItem(SplitPositionItem item)
        {
            int pos = item.Position;
            if (pos <= MinPosition)
            {
                PositionList.Remove(item);
            }
            if (pos >= MaxPosition)
            {
                PositionList.Remove(item);
            }

            bool samePositionExists = (PositionList.Count((other) => { return (item != other && other.Position == pos); }) > 0);
            if (samePositionExists) {
                PositionList.Remove(item);
            }
        }


        void positionList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (SplitPositionItem item in e.NewItems)
                {
                    item.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(positionItem_PropertyChanged);
                }
            }

            OnAnyPositionChanged();
        }

        void positionItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Position")
                return;

            SplitPositionItem item = (SplitPositionItem)sender;

            if (item.Position < MinPosition)
            {
                item.Position = MinPosition;
            }
            else if (item.Position > MaxPosition)
            {
                item.Position = MaxPosition;
            }

            OnAnyPositionChanged();
        }
    }
}
