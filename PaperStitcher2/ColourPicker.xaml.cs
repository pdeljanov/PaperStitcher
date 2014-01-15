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
    /// <summary>
    /// A Colour Picker based heavily off the one on Silverlight Contrib.
    /// </summary>
    public partial class ColourPicker : Window
    {
        bool m_isDraggingSample;
        bool m_isDraggingHue;

        double m_sampleX;
        double m_sampleY;
        double m_hueY;

        ColourSpace m_colourSpace;
        Color m_selectedColour;


        public ColourPicker()
        {
            InitializeComponent();

            m_colourSpace = new ColourSpace();

            sampleMap.MouseLeftButtonDown += new MouseButtonEventHandler(sampleMap_MouseLeftButtonDown);
            sampleMap.MouseLeftButtonUp += new MouseButtonEventHandler(sampleMap_MouseLeftButtonUp);
            sampleMap.MouseMove += new MouseEventHandler(sampleMap_MouseMove);

            hueMap.MouseLeftButtonDown += new MouseButtonEventHandler(hueMap_MouseLeftButtonDown);
            hueMap.MouseLeftButtonUp += new MouseButtonEventHandler(hueMap_MouseLeftButtonUp);
            hueMap.MouseMove += new MouseEventHandler(hueMap_MouseMove);

            txtRgbR.KeyDown += new KeyEventHandler(colourComponentChanged);
            txtRgbG.KeyDown += new KeyEventHandler(colourComponentChanged);
            txtRgbB.KeyDown += new KeyEventHandler(colourComponentChanged);
            txtHsbH.KeyDown += new KeyEventHandler(colourComponentChanged);
            txtHsbS.KeyDown += new KeyEventHandler(colourComponentChanged);
            txtHsbB.KeyDown += new KeyEventHandler(colourComponentChanged);

            m_selectedColour = Colors.LightBlue;

            m_sampleX = sampleMap.Width;
            m_sampleY = 0;
            m_hueY = 0;

            UpdateVisuals();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == GlassController.WM_DWMCOMPOSITIONCHANGED)
            {
                GlassController.ExtendGlassFrame(this, new Thickness(0, 0, 0, 40));
                handled = true;
            }
            return IntPtr.Zero;
        }

        void colourComponentChanged(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    Color result;

                    if (sender == txtRgbR || sender == txtRgbG || sender == txtRgbB)
                    {
                        byte r = byte.Parse(txtRgbR.Text);
                        byte g = byte.Parse(txtRgbG.Text);
                        byte b = byte.Parse(txtRgbB.Text);

                        result = Color.FromRgb(r, g, b);
                    }
                    else
                    {
                        double h = double.Parse(txtHsbH.Text);
                        double s = double.Parse(txtHsbS.Text);
                        double b = double.Parse(txtHsbB.Text);

                        if (h < 0 || s < 0 || b < 0)
                            throw new FormatException();

                        result = m_colourSpace.ConvertHsvToRgb(h, s, b);
                    }

                    this.SelectedColor = result;

                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Invalid colour value entered.", App.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public Color SelectedColor
        {
            get { return m_selectedColour; }
            set
            {
                m_selectedColour = value;
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            Color c = this.SelectedColor;
            HSV hsv = m_colourSpace.ConvertRgbToHsv(c);

            m_hueY = (hsv.Hue / 360 * hueMap.Height);
            m_sampleY = -1 * (hsv.Value - 1) * sampleMap.Height;
            m_sampleX = hsv.Saturation * sampleMap.Width;

            if (!double.IsNaN(m_hueY))
                UpdateHueSelection();

            UpdateSampleSelection();

            UpdateCurrentColour();
        }

        void hueMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (!m_isDraggingHue)
                return;

            Point pos = e.GetPosition((UIElement)sender);
            DragSlider(pos.X, pos.Y);
        }

        void hueMap_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_isDraggingHue = false;
        }

        void hueMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_isDraggingHue = true;
            Point pos = e.GetPosition((UIElement)sender);
            DragSlider(pos.X, pos.Y);
        }

        void sampleMap_MouseMove(object sender, MouseEventArgs e)
        {
            if(!m_isDraggingSample)
                return;

            Point pos = e.GetPosition((UIElement)sender);
            DragSlider(pos.X, pos.Y);
        }

        void sampleMap_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_isDraggingSample = false;
        }

        void sampleMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_isDraggingSample = true;
            Point pos = e.GetPosition((UIElement)sender);
            DragSlider(pos.X, pos.Y);
        }




        private Color GetColor()
        {
            double yComponent = 1 - (m_sampleY / sampleMap.Height);
            double xComponent = m_sampleX / sampleMap.Width;
            double hueComponent = (m_hueY / hueMap.Height) * 360;

            return m_colourSpace.ConvertHsvToRgb(hueComponent, xComponent, yComponent);
        }

        private void UpdateCurrentColour()
        {
            Color currColour = GetColor();
            samplePreview.Fill = new SolidColorBrush(currColour);
            txtSample.Text = m_colourSpace.GetHexCode(currColour);

            HSV hsv = m_colourSpace.ConvertRgbToHsv(currColour);

            txtRgbR.Text = currColour.R.ToString();
            txtRgbG.Text = currColour.G.ToString();
            txtRgbB.Text = currColour.B.ToString();

            if (double.IsNaN(hsv.Hue))
                txtHsbH.Text = "0";
            else
                txtHsbH.Text = Math.Round(hsv.Hue).ToString();

            txtHsbS.Text = Math.Round(hsv.Saturation*100).ToString();
            txtHsbB.Text = Math.Round(hsv.Value*100).ToString();

            m_selectedColour = currColour;
        }

        private void UpdateHueSelection()
        {
            double huePos = m_hueY / hueMap.Height * 255;
            Color c = m_colourSpace.GetColorFromPosition(huePos);

            hueSelector.SetValue(Canvas.TopProperty, m_hueY - (hueSelector.Height / 2));

            sampleMap.Fill = new SolidColorBrush(c);
        }

        private void UpdateSampleSelection()
        {
            sampleSelector.SetValue(Canvas.TopProperty, m_sampleY - (sampleSelector.Height / 2));
            sampleSelector.SetValue(Canvas.LeftProperty, m_sampleX - (sampleSelector.Width / 2));
        }

        private void DragSlider(double x, double y)
        {
            if (m_isDraggingSample)
            {
                if (x < 0)
                    m_sampleX = 0;
                else if (x > sampleMap.Width)
                    m_sampleX = sampleMap.Width;
                else
                    m_sampleX = x;

                if (y < 0)
                    m_sampleY = 0;
                else if (y > sampleMap.Height)
                    m_sampleY = sampleMap.Height;
                else
                    m_sampleY = y;

                UpdateSampleSelection();
                UpdateCurrentColour();
            }
            else if (m_isDraggingHue)
            {
                if (y < 0)
                    m_hueY = 0;
                else if (m_hueY > hueMap.Height)
                    m_hueY = hueMap.Height;
                else
                    m_hueY = y;

                UpdateHueSelection();
                UpdateCurrentColour();
            }
        }






        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlassController.ExtendGlassFrame(this, new Thickness(0, 0, 0, 40));

            System.Windows.Interop.HwndSource src = System.Windows.Interop.HwndSource.FromHwnd(
                new System.Windows.Interop.WindowInteropHelper(this).Handle);
            src.AddHook(new System.Windows.Interop.HwndSourceHook(WndProc));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
