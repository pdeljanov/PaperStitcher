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
using System.Text;

namespace PaperStitcher.Common
{
    public abstract class LayoutObject
    {
        protected int m_x;
        protected int m_y;
        protected int m_z;
        protected int m_actualWidth;
        protected int m_actualHeight;
        protected int m_width;
        protected int m_height;
        private bool m_flippedHorizontally;
        private bool m_flipVertically;
        private int m_key;


        public LayoutObject()
        {
            m_flippedHorizontally = false;
            m_flipVertically = false;

        }

        public abstract string Serialize();

        public abstract void Deserialize(string data);

        public abstract string Source { get; set; }

        public int BindingKey
        {
            get { return m_key; }
            set { m_key = value; }
        }

        public int SourceWidth
        {
            get { return m_width; }
        }

        public int SourceHeight
        {
            get { return m_height; }
        }

        public int ActualWidth
        {
            get
            {
                return m_actualWidth;
            }
            set
            {
                m_actualWidth = value;
            }
        }

        public int ActualHeight
        {
            get
            {
                return m_actualHeight;
            }
            set
            {
                m_actualHeight = value;
            }
        }

        public int X
        {
            get
            {
                return m_x;
            }
            set
            {
                m_x = value;
            }
        }

        public int Y
        {
            get
            {
                return m_y;
            }
            set
            {
                m_y = value;
            }
        }

        public int Z
        {
            get
            {
                return m_z;
            }
            set
            {
                m_z = value;
            }
        }

        public bool IsFlippedX
        {
            get { return m_flippedHorizontally; }
            set { m_flippedHorizontally = value; }
        }

        public bool IsFlippedY
        {
            get { return m_flipVertically; }
            set { m_flipVertically = value; }
        }

        public double Aspect
        {
            get { return ((double)m_height / m_width); }
        }



    }

}
