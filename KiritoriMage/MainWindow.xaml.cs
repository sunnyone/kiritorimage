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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.IO;

namespace KiritoriMage
{

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        ImageViewModel imageViewModel = new ImageViewModel();

        AdornerLayer imageAdornerLayer;

        // just references
        ImagePositionConverter horizontalPositionConverter;
        ImagePositionConverter verticalPositionConverter;

        private const string MainWindowTitleFormat = "{0} - KiritoriMage";

        public MainWindow()
        {
            InitializeComponent();

            //AddHandler(FrameworkElement.MouseDownEvent, new MouseButtonEventHandler(imageMain_MouseDown), true);
        }

        private void LoadImageFromFile(string path) {
            BitmapImage bitmapImage = null;
            try
            {
                // to prevent file locking, use BeginInit-EndInit
                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bitmapImage.EndInit();
            }
            catch (Exception ex)
            {
                statusText.Text = "Failed to load " + path + ": " + ex.Message;
                return;
            }

            this.imageViewModel.LoadedBitmapFileName = path;
            this.imageViewModel.LoadedBitmap = bitmapImage;
            this.imageViewModel.ClearPositions();

            this.Title = String.Format(MainWindowTitleFormat, System.IO.Path.GetFileName(path));
            statusText.Text = String.Format("Loaded {0} ({1},{2})",
                path, bitmapImage.PixelWidth, bitmapImage.PixelHeight);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.dockPanelRoot.DataContext = imageViewModel;

            horizontalPositionConverter = (ImagePositionConverter)this.Resources["HorizontalPositionConverter"];
            horizontalPositionConverter.GetMaxImagePosition = new Func<int>(
                () => { return this.imageViewModel.LoadedBitmapWidth; }
            );
            horizontalPositionConverter.GetMaxViewPosition = new Func<double>(
                () => { return this.imageMain.ActualWidth; }
            );


            verticalPositionConverter = (ImagePositionConverter)this.Resources["VerticalPositionConverter"];
            verticalPositionConverter.GetMaxImagePosition = new Func<int>(
                () => { return this.imageViewModel.LoadedBitmapHeight; }
            );
            verticalPositionConverter.GetMaxViewPosition = new Func<double>(
                () => { return this.imageMain.ActualHeight; }
            );


            this.imageViewModel.LoadedBitmap = (BitmapImage)this.Resources["KiritoriMageHelp"];

            ImageAdorner imageAdorner = new ImageAdorner(this.imagePanel)
            {
                ImageViewModel = this.imageViewModel,
                HorizontalPositionConverter = horizontalPositionConverter,
                VerticalPositionConverter = verticalPositionConverter
            };

            imageAdornerLayer = AdornerLayer.GetAdornerLayer(this.imagePanel);
            imageAdornerLayer.Add(imageAdorner);

            // mainly for generating a new point
            imageViewModel.RangeDataUpdated += new EventHandler((sender1, e1) => { 
                imageAdornerLayer.Update(); 
            });

            this.Width = Properties.Settings.Default.WindowWidth;
            this.Height = Properties.Settings.Default.WindowHeight;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            string filename = ((App)Application.Current).ArgsSpecifiedFilename;
            if (filename != null)
            {
                LoadImageFromFile(filename);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.WindowWidth = this.ActualWidth;
            Properties.Settings.Default.WindowHeight = this.ActualHeight;
            Properties.Settings.Default.Save();
        }

        private void thumbDragStarted()
        {
            this.Cursor = Cursors.Hand;
            this.imageViewModel.ShowCursorLine = false;
        }
        private void HorizontalThumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            thumbDragStarted();
        }
        private void VerticalThumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            thumbDragStarted();
        }


        private void thumbDragDelta(SplitPositionItem item, ImagePositionConverter converter, double change)
        {
            item.Position += converter.ConvertViewPositionToImagePosition(change);
        }

        private void HorizontalThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            thumbDragDelta((SplitPositionItem)((Thumb)sender).DataContext, this.horizontalPositionConverter, e.HorizontalChange);
        }

        private void VerticalThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            thumbDragDelta((SplitPositionItem)((Thumb)sender).DataContext, this.verticalPositionConverter, e.VerticalChange);
        }


        private void thumbDragCompleted(SplitPositionBarViewModel barViewModel, SplitPositionItem item)
        {
            barViewModel.CheckAndRemoveBorderItem(item);

            this.Cursor = Cursors.Arrow;
            this.imageViewModel.ShowCursorLine = true;
        }
        private void HorizontalThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            thumbDragCompleted(imageViewModel.HorizontalSplitPositionBarViewModel, (SplitPositionItem)((Thumb)sender).DataContext);
        }
        private void VerticalThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            thumbDragCompleted(imageViewModel.VerticalSplitPositionBarViewModel, (SplitPositionItem)((Thumb)sender).DataContext);
        }

        private void horizontalBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int x = this.horizontalPositionConverter.ConvertViewPositionToImagePosition(e.GetPosition(horizontalBar).X);

            if (0 < x && x < this.horizontalPositionConverter.GetMaxImagePosition())
            {
                SplitPositionItem item = new SplitPositionItem() { Position = x };
                imageViewModel.HorizontalSplitPositionBarViewModel.PositionList.Add(item);
            }
        }

        private void verticalBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int y = this.verticalPositionConverter.ConvertViewPositionToImagePosition(e.GetPosition(verticalBar).Y);

            if (0 < y && y < this.verticalPositionConverter.GetMaxImagePosition())
            {
                SplitPositionItem item = new SplitPositionItem() { Position = y };
                imageViewModel.VerticalSplitPositionBarViewModel.PositionList.Add(item);
            }
        }

        private void dockPanelRoot_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(this.imagePanel);

            imageViewModel.CurrentPositon.X = this.horizontalPositionConverter.ConvertViewPositionToImagePosition(p.X);
            imageViewModel.CurrentPositon.Y = this.verticalPositionConverter.ConvertViewPositionToImagePosition(p.Y);

            imageAdornerLayer.Update();
        }

        private void imagePanel_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] filenames = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (filenames.Length < 1)
            {
                return;
            }

            LoadImageFromFile(filenames[0]);
        }

        private void imageMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // FIXME: dirty hack: refresh all properties...
            foreach (SplitPositionItem item in this.imageViewModel.VerticalSplitPositionBarViewModel.PositionList)
            {
                item.Position = item.Position;
            }
            foreach (SplitPositionItem item in this.imageViewModel.HorizontalSplitPositionBarViewModel.PositionList)
            {
                item.Position = item.Position;
            }

            imageAdornerLayer.Update();
        }

        private void imageMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point posAtImageMain = e.GetPosition(imageMain);

            int x = this.horizontalPositionConverter.ConvertViewPositionToImagePosition(posAtImageMain.X);
            int y = this.verticalPositionConverter.ConvertViewPositionToImagePosition(posAtImageMain.Y);

            this.imageViewModel.MarkedPointList.Add(new PointInt(x, y));

            this.imageAdornerLayer.Update();
        }

        private void imageMain_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // removes last element
            int count = this.imageViewModel.MarkedPointList.Count;
            if (count > 0)
            {
                this.imageViewModel.MarkedPointList.RemoveAt(count - 1);
            }

            this.imageAdornerLayer.Update();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (this.imageViewModel.LoadedBitmapFileName == null)
                return;

            string imageDir = System.IO.Path.GetDirectoryName(this.imageViewModel.LoadedBitmapFileName);
            string imageBaseName = System.IO.Path.GetFileNameWithoutExtension(this.imageViewModel.LoadedBitmapFileName);

            List<string> savedFileList = new List<string>();

            int i = 0;
            foreach (Int32Rect? rect in imageViewModel.GetSelectedRegionList())
            {
                i++;

                if (!rect.HasValue)
                    continue;

#if true
                CroppedBitmap croppedBitmap = new CroppedBitmap(this.imageViewModel.LoadedBitmap, rect.Value);
                string filename = String.Format("{0}_{1:000}.jpg", imageBaseName, i);

                JpegBitmapEncoder enc = new JpegBitmapEncoder();
                enc.QualityLevel = 90;
                enc.Frames.Add(BitmapFrame.Create(croppedBitmap));

                string path = System.IO.Path.Combine(imageDir, Properties.Settings.Default.SaveRelativeDir, filename);
                try
                {
                    using (FileStream fs = new System.IO.FileStream(path, FileMode.Create))
                    {
                        enc.Save(fs);
                    }
                }
                catch (Exception ex)
                {
                    this.statusText.Text = String.Format("Failed to write {0}: {1}", path, ex.Message);
                    return;
                }

                savedFileList.Add(System.IO.Path.GetFileName(path));
#else
                // dirty / debugging purpose: save range to a file.
                string filename = String.Format("{0}.txt", imageBaseName);
                string textPath = System.IO.Path.Combine(imageDir, filename);
                System.IO.File.AppendAllText(textPath, string.Format("{0},{1} ", rect.Value.X, rect.Value.Width), Encoding.UTF8);
#endif
            }

            this.statusText.Text = String.Format("Saved {0}.", String.Join(", ", savedFileList));
        }

        private void buttonRotate_Click(object sender, RoutedEventArgs e)
        {
            TransformedBitmap tb = new TransformedBitmap();
            tb.BeginInit();
            tb.Transform = new RotateTransform(90);
            tb.Source = this.imageViewModel.LoadedBitmap;
            tb.EndInit();
            this.imageViewModel.LoadedBitmap = tb;

            this.imageViewModel.ClearPositions();
        }
    }
}
