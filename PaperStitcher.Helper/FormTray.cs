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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using PaperStitcher.Common;
using System.IO;
using System.Threading;

namespace PaperStitcher.Helper
{
    public partial class FormTray : Form
    {

        private string m_homeDirectory;
        private string m_displayConfigName;

        private FileChangeWatcher m_watcher;

        private System.Threading.Timer m_timer;

        private DisplayInformation m_currentDisplayInfo;
        private LayoutManager m_currentLayout;
        private DateTime m_epoch;

        delegate void UpdateWallpaperCallback();

        public FormTray()
        {
            InitializeComponent();

            m_homeDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar +
                "PaperStitcher" + Path.DirectorySeparatorChar;

            m_currentLayout = new LayoutManager();

            m_currentDisplayInfo = new DisplayInformation();
            m_displayConfigName = m_currentDisplayInfo.ToString() + ".xml";

            m_currentLayout.LoadLayout(m_homeDirectory + m_displayConfigName);

            m_epoch = ShuffleInformation.GetEpoch(m_homeDirectory + "Shuffle.xml", m_displayConfigName);

            if (m_epoch == DateTime.MinValue)
                ShuffleInformation.SetEpoch(m_homeDirectory + "Shuffle.xml", m_displayConfigName, DateTime.Now);

            m_watcher = new FileChangeWatcher(m_homeDirectory, "*.xml");
            m_watcher.Changed += new FileSystemEventHandler(m_watcher_Changed);

            InitTimer();
        }

        void m_watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name == "Shuffle.xml")
            {
                m_epoch = ShuffleInformation.GetEpoch(m_homeDirectory + "Shuffle.xml", m_displayConfigName);
                System.Diagnostics.Debug.WriteLine("Reset epoch.  It is now: " + m_epoch.ToString());
                InitTimer();
            }
            else if (e.Name == m_displayConfigName)
            {
                m_currentLayout.LoadLayout(m_homeDirectory + m_displayConfigName);
                InitTimer();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Hide();
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            m_watcher.Changed -= new FileSystemEventHandler(m_watcher_Changed);
            ShuffleInformation.SetEpoch(m_homeDirectory + "Shuffle.xml", m_displayConfigName, m_epoch);
        }

        #region Timer Handling

        private void InitTimer()
        {
            TimeSpan sleepFor;
            bool usesShuffle = FindSleepTime(m_currentLayout, out sleepFor);

            if (usesShuffle)
            {
                if (sleepFor < TimeSpan.Zero)
                {
                    DoUpdateWallpaper();
                }
                else
                {
                    if (m_timer == null)
                    {
                        m_timer = new System.Threading.Timer(TimerCallbackFunction, null, sleepFor, TimeSpan.FromMilliseconds(-1));
                    }
                    else
                    {
                        m_timer.Change(sleepFor, TimeSpan.FromMilliseconds(-1));
                    }

                    System.Diagnostics.Debug.WriteLine("Will now sleep for: " + sleepFor.ToString());
                }
            }
        }

        private void TimerCallbackFunction(Object state)
        {
            DoUpdateWallpaper();
        }

        #endregion

        void DoUpdateWallpaper()
        {
            if (this.InvokeRequired)
            {
                UpdateWallpaperCallback callback = new UpdateWallpaperCallback(DoUpdateWallpaper);
                this.Invoke(callback);
            }
            else
            {
                m_epoch = DateTime.Now;
                System.Diagnostics.Debug.WriteLine("Change wallpaper now!");
                WallpaperController.RenderWallpaper(m_currentDisplayInfo, m_currentLayout, true, m_homeDirectory);
                InitTimer();
            }
        }

        private bool FindSleepTime(LayoutManager lm, out TimeSpan sleepTime)
        {
            TimeSpan minSleepTime = TimeSpan.MaxValue;
            int minSleepScreen = -1;

            TimeSpan timeInDay = TimeSpan.FromSeconds(86399);

            foreach(KeyValuePair<int, LayoutCanvas> kvp in lm)
            {
                LayoutCanvas canvas = kvp.Value;

                if (canvas.IsShuffleEnabled)
                {
                    TimeSpan interval = TimeSpan.MaxValue;
                    DateTime now = DateTime.Now;

                    if (canvas.ShuffleMode.Type == ShuffleType.Interval)
                    {
                        TimeSpan elapsed = now - m_epoch;
                        interval = canvas.ShuffleMode.Interval - elapsed;
                    }
                    else
                    {
                        if (canvas.ShuffleMode.Type == ShuffleType.Daily)
                        {
                            DateTime midnightTonight = DateTime.Today.Add(timeInDay);
                            interval = midnightTonight - now;
                        }
                        else if (canvas.ShuffleMode.Type == ShuffleType.Weekly)
                        {
                            int numDays = DateTime.Today.DayOfWeek - DayOfWeek.Sunday;
                            DateTime midnightSunday =
                                DateTime.Today.AddDays(numDays).Add(timeInDay);

                            interval = midnightSunday - now;
                        }
                        else if (canvas.ShuffleMode.Type == ShuffleType.Monthly)
                        {
                            int day = DateTime.Today.Day;
                            int daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);

                            DateTime midnightEndMonth = DateTime.Today.Date.AddDays(daysInMonth - day).Add(timeInDay);

                            interval = midnightEndMonth - now;
                        }
                    }


                    if (interval < minSleepTime)
                    {
                        minSleepTime = interval;
                        minSleepScreen = kvp.Key;
                    }

                }
            }

            sleepTime = minSleepTime;
            return (minSleepScreen != -1);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }






    }


}
