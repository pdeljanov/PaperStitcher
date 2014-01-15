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
using System.Drawing;

namespace PaperStitcher.Common
{
    public class Screen
    {
        private Rectangle m_bounds;
        private bool m_isPrimary;
        private int m_index;

        public Screen(int index, Rectangle bounds, bool isPrimary)
        {
            this.m_isPrimary = isPrimary;
            this.m_bounds = bounds;
            this.m_index = index;
        }

        public Rectangle Bounds
        {
            get { return this.m_bounds; }
        }

        public int X
        {
            get { return this.m_bounds.X; }
            set { this.m_bounds.X = value; }
        }

        public int Y
        {
            get { return this.m_bounds.Y; }
            set { this.m_bounds.Y = value; }
        }

        public int Width
        {
            get { return this.m_bounds.Width; }
        }

        public int Height
        {
            get { return this.m_bounds.Height; }
        }

        public bool IsPrimary
        {
            get { return m_isPrimary; }
        }

        public int Index
        {
            get { return m_index; }
        }
    }

}
