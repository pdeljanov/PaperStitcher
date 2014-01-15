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
using System.IO;

namespace PaperStitcher.Helper
{
    class FileChangeWatcher
    {
        private FileSystemWatcher m_watcher;
        private Dictionary<string, DateTime> m_lastEventTime;

        public event FileSystemEventHandler Changed;

        public FileChangeWatcher(string path, string filter)
        {
            m_lastEventTime = new Dictionary<string, DateTime>();

            m_watcher = new FileSystemWatcher(path, filter);
            m_watcher.Changed += new FileSystemEventHandler(m_watcher_Changed);
            m_watcher.EnableRaisingEvents = true;
            m_watcher.NotifyFilter = NotifyFilters.LastWrite;
            m_watcher.IncludeSubdirectories = false;
        }

        void m_watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (m_lastEventTime.ContainsKey(e.Name))
            {
                double lastTime = DateTime.Now.Subtract(m_lastEventTime[e.Name]).TotalMilliseconds;

                if (lastTime < 500)
                {
                    return;
                }
                else
                {
                    m_lastEventTime[e.Name] = DateTime.Now;
                }
            }
            else
            {
                m_lastEventTime.Add(e.Name, DateTime.Now);
            }

            System.Threading.Thread.Sleep(100);
            this.Changed(sender, e);
        }


    }

}
