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
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace PaperStitcher.Common
{
    public class WallpaperController
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        public static void SetWallpaper(string path)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true);
            key.SetValue(@"WallpaperStyle", "0");
            key.SetValue(@"TileWallpaper", "1");
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }

        public static void RenderWallpaper(DisplayInformation dpInfo, LayoutManager lm, bool isDesktopMode, string location)
        {
            if (isDesktopMode)
            {
                string p = location + dpInfo.ToString() + ".bmp";

                Renderer r = new Renderer();
                r.ImageFormat = ImageFormat.Bmp;
                r.OutputPath = p;
                r.Render(dpInfo, lm, isDesktopMode);

                WallpaperController.SetWallpaper(p);
            }
            else
            {
                string p = location + "Logon.jpg";

                string execPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string execDir = System.IO.Path.GetDirectoryName(execPath);

                Renderer r = new Renderer();
                r.ImageFormat = ImageFormat.Jpeg;
                r.OutputPath = p;
                r.FileSizeLimit = 256 * 1024;
                r.Render(dpInfo, lm, isDesktopMode);

                try
                {
                    System.Diagnostics.ProcessStartInfo procInfo = new System.Diagnostics.ProcessStartInfo();
                    procInfo.UseShellExecute = true;
                    procInfo.FileName = "PaperStitcher.Helper.exe";
                    procInfo.WorkingDirectory = execDir;
                    procInfo.Arguments = "--logon " + p;
                    procInfo.CreateNoWindow = false;
                    procInfo.Verb = "runas";
                    System.Diagnostics.Process.Start(procInfo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message.ToString());
                }
            }
        }



    }


}
