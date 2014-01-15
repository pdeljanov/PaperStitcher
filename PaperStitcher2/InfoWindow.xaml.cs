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
using System.Windows.Shapes;

namespace PaperStitcher2
{
    public delegate void PositionChangedHandler(double x, double y);
    public delegate void SizeChangedHandler(double width, double height);
    public delegate void FlipChangedHandler(bool flipV, bool flipH);

    /// <summary>
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window
    {
        private double x;
        private double y;
        private double width;
        private double height;
        private bool flipV;
        private bool flipH;

        public event PositionChangedHandler ElementPositionChanged;
        public event SizeChangedHandler ElementSizeChanged;

        public InfoWindow()
        {
            InitializeComponent();

            x = 0;
            y = 0;
            width = 1;
            height = 1;
            flipV = false;
            flipH = false;
        }

        #region Member Event Handlers

        void txtHeight_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    double temp = double.Parse(txtHeight.Text);
                    InspectedHeight = temp;
                    FireSizeChanged();
                }
                catch (Exception)
                {
                    txtHeight.Text = height.ToString();
                }
            }
        }

        void txtWidth_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    double temp = double.Parse(txtWidth.Text);
                    InspectedWidth = temp;
                    FireSizeChanged();
                }
                catch (Exception)
                {
                    txtWidth.Text = width.ToString();
                }
            }
        }

        void txtYPosition_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    double temp = double.Parse(txtYPosition.Text);
                    InspectedY = temp;
                    FirePositionChanged();
                }
                catch (Exception)
                {
                    txtYPosition.Text = y.ToString();
                }
            }
        }

        void txtXPosition_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    double temp = double.Parse(txtXPosition.Text);
                    InspectedX = temp;
                    FirePositionChanged();
                }
                catch (Exception)
                {
                    txtXPosition.Text = x.ToString();
                }
            }
        }

        #endregion

        #region Inspected Element Properties

        public double InspectedX
        {
            get { return x; }
            set
            {
                x = value;
                txtXPosition.Text = x.ToString();
            }
        }

        public double InspectedY
        {
            get { return y; }
            set
            {
                y = value;
                txtYPosition.Text = y.ToString();
            }
        }

        public double InspectedWidth
        {
            get { return width; }
            set
            {
                width = value;
                txtWidth.Text = width.ToString();
            }
        }

        public double InspectedHeight
        {
            get { return height; }
            set
            {
                height = value;
                txtHeight.Text = height.ToString();
            }
        }

        public bool InspectedFlipVertical
        {
            get { return flipV; }
            set
            {
                flipV = value;
                chkFlipVertically.IsChecked = flipV;
            }
        }

        public bool InspectedFlipHorizontal
        {
            get { return flipH; }
            set
            {
                flipH = value;
                chkFlipHorizontally.IsChecked = flipH;
            }
        }

        public bool IsInspecting
        {
            set
            {
                txtXPosition.IsEnabled = value;
                txtYPosition.IsEnabled = value;
                txtWidth.IsEnabled = value;
                txtHeight.IsEnabled = value;
            }
        }

        #endregion

        private void FireSizeChanged()
        {
            if (ElementSizeChanged != null)
            {
                ElementSizeChanged(width, height);
            }
        }

        private void FirePositionChanged()
        {
            if (ElementPositionChanged != null)
            {
                ElementPositionChanged(x, y);
            }
        }


    
    }
}
