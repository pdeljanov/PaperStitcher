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
using System.Drawing;

namespace PaperStitcher.Common
{
    public class LayoutCanvas : List<LayoutObject>
    {
        private Color background;
        private bool isShuffleEnabled;
        private ShuffleMode shuffleMode;
        private DateTime shuffleEpoch;
        private PlacementStyle pStyle;

        public LayoutCanvas()
        {
            this.background = Color.FromArgb(255, 58, 110, 165);
            this.isShuffleEnabled = false;
            this.shuffleMode = new ShuffleMode();
            this.shuffleMode.Interval = TimeSpan.FromMinutes(5);
            this.shuffleEpoch = DateTime.Now;
            this.pStyle = PlacementStyle.Centered;
        }

        public Color BackgroundColour
        {
            get { return this.background; }
            set { this.background = value; }
        }

        public bool IsShuffleEnabled
        {
            get { return isShuffleEnabled; }
            set { isShuffleEnabled = value; }
        }

        public ShuffleMode ShuffleMode
        {
            get { return shuffleMode; }
            set { shuffleMode = value; }
        }

        public DateTime ShuffleEpoch
        {
            get { return shuffleEpoch; }
            set { shuffleEpoch = value; }
        }

        public PlacementStyle SingleObjectStyle
        {
            get { return pStyle; }
            set { pStyle = value; }
        }

        public bool UsingSingleObjectMode
        {
            get
            {
                return (Count == 1) && (pStyle != PlacementStyle.Custom);
            }
        }

    }

}
