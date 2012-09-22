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
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace KiritoriMage
{
    public class ImageAdorner : Adorner
    {
        public ImageViewModel ImageViewModel { get; set; }

        public ImagePositionConverter HorizontalPositionConverter { get; set; }
        public ImagePositionConverter VerticalPositionConverter { get; set; }

        public ImageAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            // to pass events to underlying elements
            IsHitTestVisible = false;
        }

        private Rect ImagePositionRectToViewPointRect(Int32Rect r)
        {
            return new Rect(
                new Point(
                    this.HorizontalPositionConverter.ConvertImagePositionToViewPosition(r.X),
                    this.VerticalPositionConverter.ConvertImagePositionToViewPosition(r.Y)),
                new Point(
                    this.HorizontalPositionConverter.ConvertImagePositionToViewPosition(r.X + r.Width),
                    this.VerticalPositionConverter.ConvertImagePositionToViewPosition(r.Y + r.Height)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            int maxX = this.HorizontalPositionConverter.GetMaxImagePosition();
            int maxY = this.VerticalPositionConverter.GetMaxImagePosition();

            double maxXView = this.HorizontalPositionConverter.GetMaxViewPosition();
            double maxYView = this.VerticalPositionConverter.GetMaxViewPosition();

            // FIXME: sorting everytime is slow.
            int[] xSplitPosArray = ImageViewModel.HorizontalSplitPositionBarViewModel.GetPositionArraySorted();
            int[] ySplitPosArray = ImageViewModel.VerticalSplitPositionBarViewModel.GetPositionArraySorted();

            // render lines
            Pen pen = new Pen();
            pen.Brush = Brushes.Red;
            pen.Thickness = 1;
            foreach (int x in xSplitPosArray)
            {
                double xView = this.HorizontalPositionConverter.ConvertImagePositionToViewPosition(x);
                drawingContext.DrawLine(pen, new Point(xView, 0), new Point(xView, maxYView));
            }

            foreach (int y in ySplitPosArray)
            {
                double yView = this.HorizontalPositionConverter.ConvertImagePositionToViewPosition(y);
                drawingContext.DrawLine(pen, new Point(0, yView), new Point(maxXView, yView));
            }


            int i = 1;
            foreach (PointInt point in ImageViewModel.MarkedPointList)
            {
                int x = point.X;
                int y = point.Y;

                Int32Rect? markedRegion = Util.SearchRegionFromArray(x, y, xSplitPosArray, ySplitPosArray, maxX, maxY);

                if (markedRegion.HasValue)
                {
                    double xView = this.HorizontalPositionConverter.ConvertImagePositionToViewPosition(x);
                    double yView = this.VerticalPositionConverter.ConvertImagePositionToViewPosition(y);

                    // marked point cursor
                    drawingContext.DrawEllipse(Brushes.Red, null, new Point(xView, yView), 3.0, 3.0);

                    // number
                    FormattedText ft = new FormattedText(
                        String.Format("({0})", i),
                        System.Globalization.CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        16, // font size
                        Brushes.Red);

                    Rect rect = ImagePositionRectToViewPointRect(markedRegion.Value);

                    drawingContext.DrawText(ft,
                        new Point(rect.X + rect.Width / 2 - ft.Width / 2,
                            rect.Y + rect.Height / 2 - ft.Height / 2));

                    // region color
                    drawingContext.PushOpacity(0.5);
                    drawingContext.DrawRectangle(Brushes.LightSalmon, null, rect);
                    drawingContext.Pop();

                    i++;
                }
            }


            int curPosX = ImageViewModel.CurrentPositon.X;
            int curPosY = ImageViewModel.CurrentPositon.Y;

            // if y < 0 or x < 0, the cursor is on the vertical/horizontal bar
            if ((curPosY < 0 || curPosX < 0) && !(curPosX < 0 && curPosY < 0))
            {
                if (this.ImageViewModel.ShowCursorLine)
                {
                    Pen cursorLinePen = new Pen();
                    cursorLinePen.Brush = Brushes.Green;
                    cursorLinePen.Thickness = 1;

                    if (curPosX < maxX && curPosY < 0)
                    {
                        double xView = this.HorizontalPositionConverter.ConvertImagePositionToViewPosition(curPosX);
                        drawingContext.DrawLine(cursorLinePen, new Point(xView, 0), new Point(xView, maxYView));
                    }
                    else if (curPosX < 0 && curPosY < maxY)
                    {
                        double yView = this.VerticalPositionConverter.ConvertImagePositionToViewPosition(curPosY);
                        drawingContext.DrawLine(cursorLinePen, new Point(0, yView), new Point(maxXView, yView));
                    }
                }
            }
            else
            {
                Int32Rect? cursorRegion = Util.SearchRegionFromArray(
                    curPosX, curPosY,
                    xSplitPosArray, ySplitPosArray,
                    maxX, maxY);

                if (cursorRegion.HasValue)
                {
                    drawingContext.PushOpacity(0.5);
                    drawingContext.DrawRectangle(Brushes.LightGreen, null,
                        ImagePositionRectToViewPointRect(cursorRegion.Value));
                    drawingContext.Pop();
                }
            }
        }
    }
}
