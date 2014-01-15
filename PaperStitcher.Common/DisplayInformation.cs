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
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.ObjectModel;

namespace PaperStitcher.Common
{
    public class DisplayInformation
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public uint size;
            public RectStruct monitor;
            public RectStruct workArea;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RectStruct
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hmon, ref MonitorInfo mi);

        private Rectangle m_desktopBounds;
        private List<Screen> m_screens;
        private int m_primary;

        public DisplayInformation()
        {
            m_screens = new List<Screen>();

            RefreshDisplays();
        }

        private bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
        {
            MonitorInfo mi = new MonitorInfo();
            mi.size = (uint)Marshal.SizeOf(mi);
            GetMonitorInfo(hMonitor, ref mi);

            bool isPrimary = (mi.flags == 1) ? true : false;

            Rectangle bounds = new Rectangle(mi.monitor.left, mi.monitor.top,
                mi.monitor.right - mi.monitor.left, mi.monitor.bottom - mi.monitor.top);

            Screen screen = new Screen(m_screens.Count, bounds, isPrimary);

            if (isPrimary) m_primary = m_screens.Count;

            m_screens.Add(screen);

            return true;
        }

        public void RefreshDisplays()
        {
            m_screens.Clear();

            MonitorEnumDelegate med = new MonitorEnumDelegate(MonitorEnum);
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, med, IntPtr.Zero);

            m_desktopBounds = new Rectangle();
            foreach (Screen screen in m_screens)
            {
                m_desktopBounds = Rectangle.Union(screen.Bounds, m_desktopBounds);
            }
        }

        public Screen Primary
        {
            get
            {
                return m_screens[m_primary];
            }
        }

        public Rectangle DesktopBounds
        {
            get
            {
                return m_desktopBounds;
            }
        }

        public ReadOnlyCollection<Screen> Screens
        {
            get { return new ReadOnlyCollection<Screen>(this.m_screens); }
        }

        public override string ToString()
        {
            int width = m_desktopBounds.Width;
            int height = m_desktopBounds.Height;
            string result = "";

            for (int i = 0; i < m_screens.Count; i++)
            {
                result += String.Format("{{{0},{1},{2},{3}}}{4}",
                    m_screens[i].X, m_screens[i].Y, m_screens[i].Width, m_screens[i].Height,
                    (i + 1 < m_screens.Count) ? ";" : string.Empty);
            }

            return result;
        }
    }



}