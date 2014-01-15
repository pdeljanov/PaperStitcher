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

using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.IO;
using System.Collections.Generic;

namespace PaperStitcher.Common
{
    class Renderer
    {
        private ImageFormat m_format;
        private int m_sizeLimit;
        private string m_outputPath;

        public Renderer()
        {
        }

        public int FileSizeLimit
        {
            get { return m_sizeLimit; }
            set { m_sizeLimit = value; }
        }

        public ImageFormat ImageFormat
        {
            get { return m_format; }
            set { m_format = value; }
        }

        public string OutputPath
        {
            get { return m_outputPath; }
            set { m_outputPath = value; }
        }

        public bool Render(DisplayInformation dpInfo, LayoutManager lm, bool isDesktopMode)
        {
            if( m_outputPath == string.Empty)
                return false;

            Rectangle wallRect;

            if (isDesktopMode)
            {
                wallRect = dpInfo.DesktopBounds;
            }
            else
            {
                wallRect = dpInfo.Primary.Bounds;
                wallRect.X = 0;
                wallRect.Y = 0;
            }

            //  Create the bitmap representing the wallpaper
            Bitmap bmp = new Bitmap(wallRect.Width, wallRect.Height);

            Graphics e = Graphics.FromImage(bmp);
            
            e.FillRectangle(Brushes.Black, 0, 0, wallRect.Width, wallRect.Height);
            
            foreach(KeyValuePair<int, LayoutCanvas> kvp in lm)
            {
                LayoutCanvas canvas = kvp.Value;
                Screen screen = dpInfo.Screens[kvp.Key];

                //  Get X and Y coordinates of screen in IMAGE coordinates (taking into account
                //  the shifts required to display the image properly)
                int x = (screen.X < 0) ? wallRect.Width + screen.X : screen.X;
                int y = (screen.Y < 0) ? -screen.Y : screen.Y;

                Rectangle scrBounds = new Rectangle(x, y, screen.Width, screen.Height);

                //  Fill screen background
                if (screen.Y >= 0)
                {
                    e.FillRectangle(new SolidBrush(canvas.BackgroundColour), scrBounds);
                }
                else
                {
                    Rectangle scrTop = new Rectangle(x, wallRect.Height - y, scrBounds.Width, -wallRect.Y);
                    Rectangle scrBtm = new Rectangle(x, -wallRect.Y - y, scrBounds.Width, scrBounds.Height + wallRect.Y);

                    Brush brush = new SolidBrush(canvas.BackgroundColour);

                    e.FillRectangle(brush, scrTop);
                    e.FillRectangle(brush, scrBtm);
                }

                //  Sort based on ZIndex
                LayoutObject[] clone = new LayoutObject[canvas.Count];
                canvas.CopyTo(clone);
                BubbleSort(clone);

                for( int i = 0; i < clone.Length; i++)
                {
                    LayoutObject lo = clone[i];

                    string trueSource = string.Empty;

                    if (canvas.IsShuffleEnabled)
                    {
                        trueSource = FileRandomizer.GetRandomFile(
                            Path.GetDirectoryName(lo.Source));
                    }
                    else
                    {
                        trueSource = lo.Source;
                    }

                    Rectangle loBounds = new Rectangle(lo.X + x, lo.Y + y, lo.ActualWidth, lo.ActualHeight);

                    if (scrBounds.IntersectsWith(loBounds))
                    {
                        //  Get intersecting region
                        Rectangle intRect = Rectangle.Intersect(scrBounds, loBounds);

                        //  Resized image
                        Bitmap bmpImage;
                        
                        if (lo.IsFlippedX || lo.IsFlippedY)
                        {
                            bmpImage = new Bitmap(loBounds.Width, loBounds.Height);
                            Graphics gb = Graphics.FromImage(bmpImage);

                            System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                            
                            m.Scale((lo.IsFlippedX) ? -1 : 1, (lo.IsFlippedY) ? -1 : 1);
     
                            if(lo.IsFlippedX)
                                m.Translate((float)-loBounds.Width + 1, 0);

                            if (lo.IsFlippedY)
                                m.Translate(0, (float)-loBounds.Height + 1);

                            gb.Transform = m;
                            gb.DrawImage(Image.FromFile(trueSource), new Rectangle(0, 0, loBounds.Width, loBounds.Height));
                            gb.Flush();
                            gb.Dispose();
                            gb = null;
                        }
                        else
                        {
                            bmpImage= new Bitmap(Image.FromFile(trueSource), loBounds.Size);
                        }

                        //  The destination rectangle has the same width and height as the intersection rect
                        //  but we must update the coordinates
                        Rectangle destRect = intRect;

                        //  Get the image's x and y coordinates relative to the screen position
                        int ix = loBounds.X - x;
                        int iy = loBounds.Y - y;

                        //  Offset the in image coords with the image coords
                        destRect.X = (ix < 0) ? x : (x + ix);
                        destRect.Y = (iy < 0) ? y : (y + iy);

                        //  Calculate the source rectangle
                        Rectangle srcRect = intRect;

                        srcRect.X = 0;
                        srcRect.Y = 0;

                        //  If the image has negative coordinates, ie, it starts off the screen, we must
                        //  set the x to equal the portion into the image thats on the screen
                        if (ix < 0) srcRect.X = (-1 * ix);
                        if (iy < 0) srcRect.Y = (-1 * iy);

                        if (screen.Y >= 0)
                        {
                            e.DrawImage(bmpImage, destRect, srcRect, GraphicsUnit.Pixel);
                        }
                        else
                        {
                            /*

                            +----------------+
                            |xxxxxxxxxxxxxxxx|
                            |xxxxxxxxxxxxxxxx|
                            |                +----------------+   
                            |                |                |
                            +----------------+                |
                                             |                |
                                             +----------------+
                            
                            */
                            destRect.Offset(0, screen.Y);

                            Rectangle scrTop = new Rectangle(x, y + wallRect.Y, scrBounds.Width, -wallRect.Y);
                            Rectangle scrBtm = new Rectangle(x, y, scrBounds.Width, scrBounds.Height + wallRect.Y);

                            Rectangle destRectTop = Rectangle.Intersect(scrTop, destRect);
                            Rectangle destRectBtm = Rectangle.Intersect(scrBtm, destRect);

                            //  destRectBtm -> Paints ontop
                            //  destRectTop -> Paints on bottom

                            destRectTop.Y = destRect.Y + (wallRect.Height - y);
                            destRectBtm.Y = -wallRect.Y - y;

                            Rectangle srcRectTop = new Rectangle(srcRect.X, srcRect.Y, destRectTop.Width, destRectTop.Height);
                            Rectangle srcRectBtm = new Rectangle(srcRect.X, srcRect.Y + srcRectTop.Height, destRectBtm.Width, destRectBtm.Height);

                            e.DrawImage(bmpImage, destRectTop, srcRectTop, GraphicsUnit.Pixel);
                            e.DrawImage(bmpImage, destRectBtm, srcRectBtm, GraphicsUnit.Pixel);
                        }

                        bmpImage.Dispose();
                        bmpImage = null;
                    }
                }
            }

            e.Flush(System.Drawing.Drawing2D.FlushIntention.Flush);

            try
            {
                bmp.Save(m_outputPath, m_format);
            }
            catch(Exception)
            {
                e.Dispose();
                e = null;
                bmp.Dispose();
                bmp = null;

                return false;
            }

            e.Dispose();
            e = null;
            bmp.Dispose();
            bmp = null;

            GC.Collect();
            return true;
        }

        private void BubbleSort(LayoutObject[] array)
        {
            int len = array.Length;

            do
            {
                int newn = 0;

                for (int i = 0; i < len - 1; i++ )
                {
                    if (array[i].Z > array[i + 1].Z)
                    {
                        LayoutObject temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                        newn = i + 1;
                    }
                }

                len = newn;

            } while ( len > 1);
        }
    
    
    }
}
