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
using System.Windows;

using PaperStitcher.Common;

namespace PaperStitcher2
{
    public class App : System.Windows.Application
    {
        public static string ApplicationName = "PaperStitcher";
        public static string NamespaceName = "PaperStitcher";

        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent()
        {
            //this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
        }

        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main()
        {
            PaperStitcher2.App app = new PaperStitcher2.App();
            app.InitializeComponent();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            TopLevelManager tlm = new TopLevelManager(ManagerMode.Desktop, app);
            tlm.Start();
        }


    }

}
