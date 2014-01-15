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
using System.IO;

using PaperStitcher.Common;
using Microsoft.Win32;

namespace PaperStitcher.Helper
{
    class LogonHelper
    {

        public static bool SetLogon(string path)
        {
            if (!SecurityHelper.IsAdmin) return false;

            RegistryView view = RegistryView.Default;

            if (Environment.Is64BitOperatingSystem & !Environment.Is64BitProcess)
            {
                view = RegistryView.Registry64;
            }

            //  Setup registry properly first
            RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            RegistryKey bgKey = key.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Background", true);

            bgKey.SetValue("OEMBackground", 1, RegistryValueKind.DWord);

            bgKey.Close();
            key.Close();

            string winRoot = Environment.GetEnvironmentVariable("SystemRoot");

            string oobeDir = winRoot + @"\Sysnative\oobe\";
            string infoDir = oobeDir + @"Info\";
            string bgDir = infoDir + @"Backgrounds\";

            string bgDestfile = bgDir + "backgroundDefault.jpg";

            if (!Directory.Exists(oobeDir))
                return false;

            if (!Directory.Exists(infoDir))
                Directory.CreateDirectory(infoDir);

            if (!Directory.Exists(bgDir))
                Directory.CreateDirectory(bgDir);

            if (!File.Exists(path))
                return false;

            FileInfo bgFile = new FileInfo(path);
            if (bgFile.Length > 256 * 1024)
                return false;

            File.Copy(path, bgDestfile, true);

            return true;
        }


    }
}
