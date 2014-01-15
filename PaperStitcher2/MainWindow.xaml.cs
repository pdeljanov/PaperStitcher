//
// PaperStitcher -- A multi-monitor wallpaper utility
// Copyright (c) 2010 Philip Deljanov <philip.deljanov@gmail.com>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//

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
using System.Threading;
using System.Windows.Media.Animation;

using Layout = System.Drawing;

using PaperStitcher.Common;

namespace PaperStitcher2
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void ManagerModeChangedHandler(ManagerMode newMode);

        private struct ImageBrowserPreview
        {
            public String FilePath { get; set; }
            public BitmapImage Thumbnail { get; set; }
        }

        //  Shared library controller reference
        private ImageLibraryController m_libraryManager;

        //  Shared layout manager reference, and our editing screen key
        private LayoutCanvas m_layoutCanvas;
        private Screen m_screen;

        //  Set to true if updating values which may trigger event handlers and that
        //  is not desired
        private bool m_isManuallyEditing;

        //  The height in pixels that the expander should expand to on expand
        private double m_newExpanderHeight = 240;

        private bool m_isInfoWinOpen;
        private InfoWindow m_winInfo;

        public TopLevelManager.ResetDisplaySettingsDelegate resetDisplayDelegate;

        public event ManagerModeChangedHandler ManagerModeChanged;

        public event EventHandler RenderWallpaper;

        private LibraryCategory m_libraryCategoryLocal;

        public MainWindow(bool isPrimary)
        {
            InitializeComponent();

            this.Title = App.ApplicationName;

            SetPrimaryMode(isPrimary);

            m_libraryCategoryLocal = new LibraryCategory("Libraries", "/PaperStitcher;component/Images/computer.png");
            
            uiLibraryTree.Items.Add(m_libraryCategoryLocal);
        }

        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            //  Execute the display change on the UI thread
            this.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)(() => { resetDisplayDelegate(); }
                        ));
        }

        #region Miscellaneous Controls

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (RenderWallpaper != null)
                RenderWallpaper(this, null);
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            var win = new WinAbout();
            win.Owner = this;
            win.ShowDialog();
        }

        private void chkShuffle_Checked(object sender, RoutedEventArgs e)
        {
            cmdChangeInterval.Visibility = System.Windows.Visibility.Visible;
            m_layoutCanvas.IsShuffleEnabled = true;
        }

        private void chkShuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            cmdChangeInterval.Visibility = System.Windows.Visibility.Collapsed;
            m_layoutCanvas.IsShuffleEnabled = false;
        }

        private int GetIntervalIndex(ShuffleMode mode)
        {
            if (mode.Type == ShuffleType.Daily)
            {
                return 9;
            }
            else if (mode.Type == ShuffleType.Weekly)
            {
                return 10;
            }
            else if (mode.Type == ShuffleType.Monthly)
            {
                return 11;
            }

            switch ( (int)mode.Interval.TotalMilliseconds)
            {
                case (5 * 60 * 1000): return 0;
                case (10 * 60 * 1000): return 1;
                case (15 * 60 * 1000): return 2;
                case (30 * 60 * 1000): return 3;
                case (1 * 60 * 60 * 1000): return 4;
                case (2 * 60 * 60 * 1000): return 5;
                case (4 * 60 * 60 * 1000): return 6;
                case (6 * 60 * 60 * 1000): return 7;
                case (12 * 60 * 60 * 1000): return 8;
            }

            return 0;
        }

        private void cmdChangeInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_layoutCanvas == null || m_isManuallyEditing)
                return;

            switch (cmdChangeInterval.SelectedIndex)
            {
                case 0:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromMinutes(5); break;
                case 1:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromMinutes(10); break;
                case 2:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromMinutes(15); break;
                case 3:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromMinutes(30); break;
                case 4:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromHours(1); break;
                case 5:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromHours(2); break;
                case 6:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromHours(4); break;
                case 7:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromHours(6); break;
                case 8:
                    m_layoutCanvas.ShuffleMode.Interval = TimeSpan.FromHours(12); break;
                case 9:
                    m_layoutCanvas.ShuffleMode.Type = ShuffleType.Daily; break;
                case 10:
                    m_layoutCanvas.ShuffleMode.Type = ShuffleType.Weekly; break;
                case 11:
                    m_layoutCanvas.ShuffleMode.Type = ShuffleType.Monthly; break;
            }

            m_layoutCanvas.ShuffleEpoch = DateTime.Now;
        }

        private void cmbFillMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_layoutCanvas == null || m_layoutCanvas.Count != 1 || m_isManuallyEditing)
                return;

            m_layoutCanvas.SingleObjectStyle = (PlacementStyle)cmbFillMode.SelectedIndex;

            switch (cmbFillMode.SelectedIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    {
                        ApplyQuickLayout(m_layoutCanvas[0], m_layoutCanvas.SingleObjectStyle);
                        RefreshCanvasChild((LayoutImage)canvas.First());

                        break;
                    }
                case 4:
                    {
                        break;
                    }
            }

            RefreshInformationWindowState();
            RefreshAdvancedButtonState();
            RefreshEditorState();
        }

        private void cmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ManagerMode newMode = ManagerMode.Desktop;

            switch (cmbMode.SelectedIndex)
            {
                case 0:
                    {
                        newMode = ManagerMode.Desktop;
                        uacImage.Visibility = System.Windows.Visibility.Collapsed;
                        break;
                    }
                case 1:
                    {
                        newMode = ManagerMode.Logon;

                        if (!SecurityHelper.IsAdmin)
                        {
                            uacImage.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            uacImage.Visibility = System.Windows.Visibility.Collapsed;
                        }

                        break;
                    }
            }

            if (ManagerModeChanged != null)
                ManagerModeChanged(newMode);
        }

        #endregion

        #region Window Management

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlassController.ExtendGlassFrame(this, new Thickness(-1));

            System.Windows.Interop.HwndSource src = System.Windows.Interop.HwndSource.FromHwnd(
                new System.Windows.Interop.WindowInteropHelper(this).Handle);
            src.AddHook(new System.Windows.Interop.HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == GlassController.WM_DWMCOMPOSITIONCHANGED)
            {
                GlassController.ExtendGlassFrame(this, new Thickness(-1));
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void SetPrimaryMode(bool isPrimary)
        {
            if (!isPrimary)
            {
                cmbMode.Visibility = Visibility.Collapsed;
                btnApply.Visibility = Visibility.Collapsed;
                this.ShowInTaskbar = false;
            }
            else
            {
                cmbMode.Visibility = Visibility.Visible;
                btnApply.Visibility = Visibility.Visible;
                this.ShowInTaskbar = true;
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);

                if (!FeatureHelper.CanEditLogonScreen)
                {
                    cmbMode.Visibility = System.Windows.Visibility.Collapsed;
                }

            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            stopImageBrowserPopulation = true;

            m_libraryManager.AddedItem -= new LibraryAddItemHandler(libraryController_AddedItem);
            m_libraryManager.RemovedItem -= new LibraryRemovedItemHandler(libraryController_RemovedItem);
        }

        #endregion

        #region Library Controller Event Handlers

        public void SetImageLibraryController(ImageLibraryController c)
        {
            m_libraryManager = c;
            m_libraryManager.AddedItem += new LibraryAddItemHandler(libraryController_AddedItem);
            m_libraryManager.RemovedItem += new LibraryRemovedItemHandler(libraryController_RemovedItem);

            foreach (LibraryItem item in m_libraryManager)
            {
                libraryController_AddedItem(item);
            }
        }

        void libraryController_RemovedItem(LibraryItem item)
        {
            m_libraryCategoryLocal.Children.Remove(item);
        }

        void libraryController_AddedItem(LibraryItem item)
        {
            m_libraryCategoryLocal.Children.Add(item);
        }

        #endregion

        #region Library Controls

        private void btnAddDirectory_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Multiselect = false;
            openDlg.Filter = "All Supported Types (*.bmp;*.jpg;*.jpeg;*.png)|*.bmp;*.jpg;*.jpeg;*.png|Bitmap Files (*.bmp)|*.bmp|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png";

            Nullable<bool> result = openDlg.ShowDialog(this);
            if (result == true)
            {
                string path = openDlg.FileName;
                System.IO.FileInfo fInfo = new System.IO.FileInfo(path);

                string dirPath = fInfo.DirectoryName;

                m_libraryManager.AddSignalled(dirPath);
            }

            openDlg = null;
        }

        private void btnRemoveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (uiLibraryTree.SelectedItem is LibraryItem)
            {
                LibraryItem item = (LibraryItem)uiLibraryTree.SelectedItem;
                m_libraryManager.RemoveSignalled(item.Location);
            }
        }

        private void uiLibraryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (uiLibraryTree.SelectedItem is LibraryItem)
            {
                stopImageBrowserPopulation = true;
                Monitor.Enter(imageBrowserLock);

                listImages.Items.Clear();

                LibraryItem item = (LibraryItem)uiLibraryTree.SelectedItem;

                string location = item.Location;

                if (!System.IO.Directory.Exists(location))
                {
                    MessageBox.Show(this, "The selected image directory no longer exists.", App.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    m_libraryManager.Remove(item);
                    return;
                }

                stopImageBrowserPopulation = false;

                Monitor.Exit(imageBrowserLock);

                Thread t = new Thread(new ParameterizedThreadStart(ThreadPopulateImageBrowser));
                t.Start(location);
            }
        }

        #endregion

        #region Image Browser Controls

        private volatile bool stopImageBrowserPopulation;
        private Object imageBrowserLock = new Object();

        private delegate void AddImageToBrowserHandler(ImageBrowserPreview io);

        private void AddImageToBrowser(ImageBrowserPreview io)
        {
            this.listImages.Items.Add(io);
        }

        private void ThreadPopulateImageBrowser(Object pathObject)
        {
            Monitor.Enter(imageBrowserLock);

            string path = pathObject.ToString();

            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(path);

            string[] filters = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };

            for (int i = 0; i < filters.Length; i++)
            {
                System.IO.FileInfo[] files = dir.GetFiles(filters[i]);

                foreach (System.IO.FileInfo file in files)
                {
                    ImageBrowserPreview io = new ImageBrowserPreview();
                    io.FilePath = file.FullName;

                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.DecodePixelWidth = 80;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.UriSource = new Uri(file.FullName);
                    bi.EndInit();
                    bi.Freeze();

                    io.Thumbnail = bi;

                    if (stopImageBrowserPopulation == true)
                    {
                        Monitor.Exit(imageBrowserLock);
                        return;
                    }

                    AddImageToBrowserHandler addImageHandler = new AddImageToBrowserHandler(AddImageToBrowser);
                    this.Dispatcher.BeginInvoke(addImageHandler, io);
                }
            }

            Monitor.Exit(imageBrowserLock);
        }

        private void listImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_layoutCanvas == null || listImages.SelectedIndex < 0 || m_isManuallyEditing)
                return;

            string source = ((ImageBrowserPreview)listImages.SelectedItem).FilePath;

            UIElement elem = canvas.SelectedElement;

            bool canAdd = (elem == null) && m_layoutCanvas.SingleObjectStyle == PlacementStyle.Custom;

            if (m_layoutCanvas.Count == 0 || canAdd)
            {
                //  If we have ABSOLUTELY NOTHING, OR WE WANT TO ADD, DO THIS

                SimpleImageObject sio = new SimpleImageObject(source);
                LayoutImage lImage = new LayoutImage(sio);

                m_layoutCanvas.Add(sio);

                lImage.Width = sio.SourceWidth;
                lImage.Height = sio.SourceHeight;

                canvas.AddChild(lImage);

                SetPreviewImage(lImage, sio.Source);

                if (cmbFillMode.SelectedIndex != 4)
                {
                    ApplyQuickLayout(sio, (PlacementStyle)cmbFillMode.SelectedIndex);
                }
                else
                {
                    sio.ActualWidth = sio.SourceWidth;
                    sio.ActualHeight = sio.SourceHeight;
                    sio.X = 0;
                    sio.Y = 0;
                }

                RefreshCanvasChild(lImage);

                RefreshFillModeState();
                RefreshAdvancedButtonState();
            }
            else
            {
                //  EDIT THE SELECTED ELEMENT, OR FIRST ELEMENT
                if (elem == null)
                    elem = canvas.First();

                SimpleImageObject sio = (SimpleImageObject)((LayoutImage)elem).LayoutObject;

                sio.Source = source;

                //  We have a selected element, modify it
                SetPreviewImage((LayoutImage)elem, sio.Source);

                if (cmbFillMode.SelectedIndex != 4)
                {
                    ApplyQuickLayout(sio, (PlacementStyle)cmbFillMode.SelectedIndex);
                    RefreshCanvasChild((LayoutImage)elem);
                }
            }
        }

        #endregion

        #region Layout Logic

        public void SetupLayout(LayoutCanvas layoutCanvas, Screen screen, bool isDesktopMode)
        {
            //  Cache the references to the canvas and screen
            m_layoutCanvas = layoutCanvas;
            m_screen = screen;

            //  Set initial canvas attributes
            canvas.Width = screen.Width;
            canvas.Height = screen.Height;

            //  Set the canvas background
            canvas.Background = new SolidColorBrush(
                WpfToGdi.GdiColorToWpfColour(layoutCanvas.BackgroundColour));

            canvas.ClearChildren();

            foreach (LayoutObject lo in layoutCanvas)
            {
                LayoutImage lImage = new LayoutImage(lo);

                //  Add to canvas (but don't add through the helper, because we want to manually manipulate
                //  this object)
                canvas.Children.Add(lImage);

                //  Set the preview image
                SetPreviewImage(lImage, lo.Source);

                //  Set attributes onto the child
                RefreshCanvasChild(lImage);
            }

            // Set the manual edit flag so that we don't trigger events when we change
            //  these objects
            m_isManuallyEditing = true;

            chkShuffle.Visibility = isDesktopMode ? Visibility.Visible : Visibility.Hidden;
            chkShuffle.IsChecked = layoutCanvas.IsShuffleEnabled;
            cmdChangeInterval.SelectedIndex = GetIntervalIndex(m_layoutCanvas.ShuffleMode);
            cmbFillMode.SelectedIndex = (int)m_layoutCanvas.SingleObjectStyle;

            m_isManuallyEditing = false;

            RefreshFillModeState();
            RefreshAdvancedButtonState();
            RefreshEditorState();
        }

        private void CentreObject(LayoutObject obj, Layout.Rectangle bounds)
        {
            obj.ActualWidth = obj.SourceWidth;
            obj.ActualHeight = obj.SourceHeight;
            obj.X = (bounds.Width / 2) - (obj.SourceWidth / 2);
            obj.Y = (bounds.Height / 2) - (obj.SourceHeight / 2);
        }

        private void StretchObject(LayoutObject obj, Layout.Rectangle bounds)
        {
            obj.X = 0;
            obj.Y = 0;
            obj.ActualWidth = bounds.Width;
            obj.ActualHeight = bounds.Height;
        }

        private void FitObjectWidth(LayoutObject obj, Layout.Rectangle bounds)
        {
            int newWidth = bounds.Width;
            int newHeight = (int)((double)newWidth * obj.Aspect);

            obj.ActualWidth = newWidth;
            obj.ActualHeight = newHeight;

            int y = (int)(((double)bounds.Height / 2) - ((double)newHeight / 2));

            obj.X = 0;
            obj.Y = y;
        }

        private void FitObjectHeight(LayoutObject obj, Layout.Rectangle bounds)
        {
            int newHeight = bounds.Height;
            int newWidth = (int)((double)newHeight / obj.Aspect);

            obj.ActualWidth = newWidth;
            obj.ActualHeight = newHeight;

            int x = (int)(((double)bounds.Width / 2) - ((double)newWidth / 2));

            obj.X = x;
            obj.Y = 0;
        }

        private void ApplyQuickLayout(LayoutObject obj, PlacementStyle style)
        {
            switch (style)
            {
                case PlacementStyle.Centered:
                    {
                        //  Centered
                        CentreObject(obj, m_screen.Bounds);
                        break;
                    }
                case PlacementStyle.Stretched:
                    {
                        //  Stretch
                        StretchObject(obj, m_screen.Bounds);
                        break;
                    }
                case PlacementStyle.FitWidth:
                    {
                        //  Fit Width
                        FitObjectWidth(obj, m_screen.Bounds);
                        break;
                    }
                case PlacementStyle.FitHeight:
                    {
                        // Fit Height
                        FitObjectHeight(obj, m_screen.Bounds);
                        break;
                    }
            }
        }

        #endregion

        #region Layout Preview Logic

        private void SetPreviewImage(LayoutImage image, string source)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(source);
            bi.EndInit();

            image.Source = bi;
        }

        private void RefreshCanvasChild(LayoutImage elem)
        {
            LayoutObject lo = ((LayoutImage)elem).LayoutObject;

            canvas.MoveUIElement(elem, new Point(lo.X, lo.Y), DraggableCanvas.MeasureType.Absolute);

            canvas.ResizeUIElement(elem,
                new Size(lo.ActualWidth, lo.ActualHeight),
                DraggableCanvas.MeasureType.Absolute);

            canvas.FlipUIElement(elem, lo.IsFlippedX, lo.IsFlippedY);

            canvas.SetUIElementZIndex(elem, lo.Z);
        }

        #endregion

        #region Expander Logic

        private void expander_Expanded(object sender, RoutedEventArgs e)
        {
            double to = m_newExpanderHeight;

            m_newExpanderHeight = gridMain.RowDefinitions[3].ActualHeight;

            GridLengthAnimation grdAnimation = new GridLengthAnimation();
            grdAnimation.To = new GridLength(to);
            grdAnimation.From = new GridLength(m_newExpanderHeight);
            grdAnimation.Duration = new Duration(TimeSpan.FromSeconds(.25));
            gridMain.RowDefinitions[3].BeginAnimation(RowDefinition.HeightProperty, grdAnimation);

            uiVerticalSplitter.IsEnabled = true;
        }

        private void expander_Collapsed(object sender, RoutedEventArgs e)
        {
            double to = m_newExpanderHeight;

            uiVerticalSplitter.IsEnabled = false;

            m_newExpanderHeight = gridMain.RowDefinitions[3].ActualHeight;

            GridLengthAnimation grdAnimation = new GridLengthAnimation();
            grdAnimation.To = new GridLength(to);
            grdAnimation.From = new GridLength(m_newExpanderHeight);
            grdAnimation.Duration = new Duration(TimeSpan.FromSeconds(.25));
            gridMain.RowDefinitions[3].BeginAnimation(RowDefinition.HeightProperty, grdAnimation);
        }

        #endregion

        #region Editor Controls

        private void RefreshEditorState()
        {
            canvas.IsEnabled = (m_layoutCanvas.SingleObjectStyle == PlacementStyle.Custom);
        }

        private void RefreshAdvancedButtonState()
        {
            bool flipEnabled = false;
            bool moveEnabled = false;

            bool customObjectSelected = (m_layoutCanvas.SingleObjectStyle == PlacementStyle.Custom &&
                            canvas.SelectedElement is LayoutImage);

            flipEnabled = (m_layoutCanvas.UsingSingleObjectMode) || (customObjectSelected);

            moveEnabled = customObjectSelected;

            btnVAlignTop.IsEnabled = moveEnabled;
            btnVAlignMiddle.IsEnabled = moveEnabled;
            btnVAlignBottom.IsEnabled = moveEnabled;
            btnHAlignLeft.IsEnabled = moveEnabled;
            btnHAlignRight.IsEnabled = moveEnabled;
            btnHAlignCentre.IsEnabled = moveEnabled;
            btnNudgeDown.IsEnabled = moveEnabled;
            btnNudgeLeft.IsEnabled = moveEnabled;
            btnNudgeRight.IsEnabled = moveEnabled;
            btnNudgeUp.IsEnabled = moveEnabled;
            btnSendBackwards.IsEnabled = moveEnabled;
            btnSendForwards.IsEnabled = moveEnabled;
            btnSendToBack.IsEnabled = moveEnabled;
            btnSendToFront.IsEnabled = moveEnabled;

            btnFlipHorizontal.IsEnabled = flipEnabled;
            btnFlipVertical.IsEnabled = flipEnabled;
            btnDelete.IsEnabled = flipEnabled;
        }

        private void RefreshFillModeState()
        {
            if (m_layoutCanvas.Count == 1)
            {
                cmbFillMode.IsEnabled = true;
            }
            else
            {
                cmbFillMode.IsEnabled = false;
            }
        }

        private void btnNudgeLeft_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.SelectedElement != null)
            {
                canvas.MoveUIElement(canvas.SelectedElement, new Point(-4, 0), DraggableCanvas.MeasureType.Relative);
            }
        }

        private void btnNudgeUp_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.SelectedElement != null)
            {
                canvas.MoveUIElement(canvas.SelectedElement, new Point(0, -4), DraggableCanvas.MeasureType.Relative);
            }
        }

        private void btnNudgeRight_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.SelectedElement != null)
            {
                canvas.MoveUIElement(canvas.SelectedElement, new Point(4, 0), DraggableCanvas.MeasureType.Relative);
            }
        }

        private void btnNudgeDown_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.SelectedElement != null)
            {
                canvas.MoveUIElement(canvas.SelectedElement, new Point(0, 4), DraggableCanvas.MeasureType.Relative);
            }
        }

        private void btnVAlignTop_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                double x = ((LayoutImage)elem).LayoutObject.X;
                canvas.MoveUIElement(canvas.SelectedElement, new Point(x, 0), DraggableCanvas.MeasureType.Absolute);
            }
        }

        private void btnVAlignMiddle_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

                double y = (m_screen.Height / 2) - ((double)lo.ActualHeight / 2);
                canvas.MoveUIElement(canvas.SelectedElement, new Point(lo.X, y), DraggableCanvas.MeasureType.Absolute);
            }
        }

        private void btnVAlignBottom_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

                double y = m_screen.Height - lo.ActualHeight;
                canvas.MoveUIElement(canvas.SelectedElement, new Point(lo.X, y), DraggableCanvas.MeasureType.Absolute);
            }
        }

        private void btnHAlignLeft_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                double y = ((LayoutImage)elem).LayoutObject.Y;
                canvas.MoveUIElement(canvas.SelectedElement, new Point(0, y), DraggableCanvas.MeasureType.Absolute);
            }
        }

        private void btnHAlignCentre_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

                double x = (m_screen.Width / 2) - ((double)lo.ActualWidth / 2);
                canvas.MoveUIElement(canvas.SelectedElement, new Point(x, lo.Y), DraggableCanvas.MeasureType.Absolute);
            }
        }

        private void btnHAlignRight_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

                double x = m_screen.Width - lo.ActualWidth;
                canvas.MoveUIElement(canvas.SelectedElement, new Point(x, lo.Y), DraggableCanvas.MeasureType.Absolute);
            }
        }

        private void btnFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = null;

            if (m_layoutCanvas.UsingSingleObjectMode)
            {
                elem = canvas.First();
            }
            else
            {
                elem = canvas.SelectedElement;
            }

            if (elem is LayoutImage)
            {
                LayoutObject lo = ((LayoutImage)elem).LayoutObject;
                canvas.FlipUIElement(elem, !lo.IsFlippedX, lo.IsFlippedY);
            }
        }

        private void btnFlipVertical_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = null;

            if (m_layoutCanvas.UsingSingleObjectMode)
            {
                elem = canvas.First();
            }
            else
            {
                elem = canvas.SelectedElement;
            }

            if (elem is LayoutImage)
            {
                LayoutObject lo = ((LayoutImage)elem).LayoutObject;
                canvas.FlipUIElement(elem, lo.IsFlippedX, !lo.IsFlippedY);
            }
        }

        private void btnSendBackwards_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                canvas.SendUIElementBackwards(elem);

                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

            }
        }

        private void btnSendForwards_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                canvas.SendUIElementForwards(elem);

                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

            }
        }

        private void btnSendToBack_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                canvas.SendUIElementToBack(elem);

                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

            }
        }

        private void btnSendToFront_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (elem is LayoutImage)
            {
                canvas.SendUIElementToFront(elem);

                LayoutObject lo = ((LayoutImage)elem).LayoutObject;

            }
        }

        private void btnSetBackground_Click(object sender, RoutedEventArgs e)
        {
            ColourPicker frm = new ColourPicker();
            frm.Owner = this;
            frm.ShowInTaskbar = false;
            frm.SelectedColor = WpfToGdi.GdiColorToWpfColour(m_layoutCanvas.BackgroundColour);
            if (frm.ShowDialog() == true)
            {
                m_layoutCanvas.BackgroundColour = WpfToGdi.WpfColorToGdiColour(frm.SelectedColor);
                canvas.Background = new SolidColorBrush(frm.SelectedColor);
            }
        }

        private void btnEditWindow_Click(object sender, RoutedEventArgs e)
        {
            if (m_isInfoWinOpen)
            {
                m_winInfo.Close();

                m_winInfo.ElementPositionChanged -= new PositionChangedHandler(winInfo_PositionChanged);
                m_winInfo.ElementSizeChanged -= new SizeChangedHandler(winInfo_SizeChanged);
                m_winInfo.Closing -= new System.ComponentModel.CancelEventHandler(winInfo_Closing);
                m_winInfo = null;
                m_isInfoWinOpen = false;
            }
            else
            {
                m_winInfo = new InfoWindow();

                m_winInfo.ElementPositionChanged += new PositionChangedHandler(winInfo_PositionChanged);
                m_winInfo.ElementSizeChanged += new SizeChangedHandler(winInfo_SizeChanged);
                m_winInfo.Closing += new System.ComponentModel.CancelEventHandler(winInfo_Closing);

                m_winInfo.Show();
                m_isInfoWinOpen = true;

                RefreshInformationWindowState();
                RefreshInformationWindow(true, true, true);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            UIElement elem = canvas.SelectedElement;

            if (m_layoutCanvas.UsingSingleObjectMode)
            {
                elem = canvas.First();
            }

            if (elem is LayoutImage)
            {
                canvas.DeleteChild(elem);
                m_layoutCanvas.Remove(((LayoutImage)elem).LayoutObject);

                RefreshFillModeState();
                RefreshInformationWindowState();
                RefreshInformationWindow(true, true, true);
                RefreshAdvancedButtonState();
            }
        }

        private void canvas_OnSelectedElementChanged(object sender, EventArgs e)
        {
            RefreshInformationWindowState();
            RefreshInformationWindow(true, true, true);
            RefreshAdvancedButtonState();
        }

        private void canvas_OnChildElementMoved(UIElement elem, Point location)
        {
            if (elem is LayoutImage)
            {
                LayoutObject obj = ((LayoutImage)elem).LayoutObject;

                obj.X = (int)location.X;
                obj.Y = (int)location.Y;

                RefreshInformationWindow(true, false, false);
            }
        }

        private void canvas_OnChildElementResized(UIElement elem, Rect bounds)
        {
            if (elem is LayoutImage)
            {
                LayoutObject obj = ((LayoutImage)elem).LayoutObject;

                obj.ActualWidth = (int)bounds.Width;
                obj.ActualHeight = (int)bounds.Height;
                obj.X = (int)bounds.X;
                obj.Y = (int)bounds.Y;

                RefreshInformationWindow(true, true, false);
            }
        }

        private void canvas_OnChildElementFlipped(UIElement elem, bool isFlippedX, bool isFlippedY)
        {
            if (elem is LayoutImage)
            {
                LayoutObject obj = ((LayoutImage)elem).LayoutObject;

                obj.IsFlippedY = isFlippedY;
                obj.IsFlippedX = isFlippedX;

                RefreshInformationWindow(false, false, true);
            }
        }

        private void canvas_OnChildElementZIndexChanged(UIElement elem, int index)
        {
            if (elem is LayoutImage)
            {
                LayoutObject obj = ((LayoutImage)elem).LayoutObject;
                obj.Z = index;
            }
        }

        #endregion

        #region Information Window

        private void RefreshInformationWindowState()
        {
            if (m_isInfoWinOpen)
            {
                if (m_layoutCanvas.UsingSingleObjectMode)
                {
                    m_winInfo.IsInspecting = false;
                }
                else if (canvas.SelectedElement == null)
                {
                    m_winInfo.IsInspecting = false;
                }
                else
                {
                    m_winInfo.IsInspecting = true;
                }
            }
        }

        private void RefreshInformationWindow(bool doPosition, bool doSize, bool doFlip)
        {
            if (m_isInfoWinOpen)
            {
                UIElement elem = canvas.SelectedElement;

                if (m_layoutCanvas.UsingSingleObjectMode)
                {
                    elem = canvas.First();
                }

                if (elem is LayoutImage)
                {
                    LayoutObject obj = ((LayoutImage)elem).LayoutObject;

                    if (doPosition)
                    {
                        m_winInfo.InspectedX = obj.X;
                        m_winInfo.InspectedY = obj.Y;
                    }
                    if (doSize)
                    {
                        m_winInfo.InspectedWidth = obj.ActualWidth;
                        m_winInfo.InspectedHeight = obj.ActualHeight;
                    }
                    if (doFlip)
                    {
                        m_winInfo.InspectedFlipHorizontal = obj.IsFlippedX;
                        m_winInfo.InspectedFlipVertical = obj.IsFlippedY;
                    }
                }

            }
        }

        void winInfo_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_isInfoWinOpen = false;
        }

        void winInfo_SizeChanged(double width, double height)
        {
            if (canvas.SelectedElement != null)
            {
                canvas.ResizeUIElement(canvas.SelectedElement, new Size(width, height), DraggableCanvas.MeasureType.Absolute);
            }
        }

        void winInfo_PositionChanged(double x, double y)
        {
            if (canvas.SelectedElement != null)
            {
                canvas.MoveUIElement(canvas.SelectedElement, new Point(x, y), DraggableCanvas.MeasureType.Absolute);
            }
        }

        #endregion
   
    }
}
