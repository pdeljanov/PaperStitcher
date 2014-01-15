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
using System.Windows;

using System.Xml;
using System.IO;

using PaperStitcher.Common;

namespace PaperStitcher2
{
    public class TopLevelManager
    {
        //  Layout control and mangement
        private LayoutManager m_layoutManager;
        private DisplayInformation m_displays;

        //  Library Manager
        private ImageLibraryController m_libraryManager;

        //  Window mangement
        private MainWindow m_primaryWindow;
        private List<MainWindow> m_childWindows;

        private ManagerMode m_mode;

        private string m_layoutFile;

        //  Application user data folder path
        private static string m_appUserDataFolder;


        public delegate void ResetDisplaySettingsDelegate();

        App appInstance;

        public TopLevelManager(ManagerMode initialMode, App app)
        {
            appInstance = app;
            appInstance.Exit += new ExitEventHandler(appInstance_Exit);

            m_displays = new DisplayInformation();
            m_layoutManager = new LayoutManager();
            m_libraryManager = new ImageLibraryController();

            m_childWindows = new List<MainWindow>();

            //  Check if the configuration directory exists, if it doesn't, create it
            ApplicationUserDataFolder = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar +
                App.NamespaceName + Path.DirectorySeparatorChar;

            if (!Directory.Exists(m_appUserDataFolder))
            {
                Directory.CreateDirectory(m_appUserDataFolder);
            }

            //  Load common settings
            LoadCommonSettings();

            //  Set the mode to operate in
            m_mode = initialMode;
        }

        public static string ApplicationUserDataFolder
        {
            get { return m_appUserDataFolder; }
            set { m_appUserDataFolder = value; }
        }

        void appInstance_Exit(object sender, ExitEventArgs e)
        {
            m_layoutManager.SaveLayout(m_layoutFile);
            System.Diagnostics.Debug.WriteLine("Saving common.");
            SaveCommonSettings();
        }

        #region Common Settings

        private void LoadCommonSettings()
        {
            XmlDocument xDoc = new XmlDocument();

            try
            {
                xDoc.Load(new StreamReader(TopLevelManager.ApplicationUserDataFolder + "Settings.xml", Encoding.UTF8));
                XmlNodeList libraryItems = xDoc.SelectNodes(App.NamespaceName + "/Library/Item");

                foreach (XmlNode item in libraryItems)
                {
                    string name = item.Attributes["Name"].Value;
                    string path = item.InnerText;
                    m_libraryManager.Add(new LibraryItem(name, path));
                }

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load settings. Error text: " + e.Message);
            }
        }

        private void SaveCommonSettings()
        {
            XmlDocument xDoc = new XmlDocument();

            //  Create declaration
            XmlDeclaration xDeclaration = xDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            //  Create root node
            XmlElement xRoot = xDoc.CreateElement(App.NamespaceName);
            xDoc.InsertBefore(xDeclaration, xDoc.DocumentElement);
            xDoc.AppendChild(xRoot);

            XmlElement xLibraryRoot = xDoc.CreateElement("Library");
            xDoc.DocumentElement.PrependChild(xLibraryRoot);

            foreach(LibraryItem item in m_libraryManager)
            {
                XmlElement xItem = xDoc.CreateElement("Item");
                xItem.SetAttribute("Name", item.Name);

                XmlText itemPath = xDoc.CreateTextNode(item.Location);
                xItem.AppendChild(itemPath);

                xLibraryRoot.AppendChild(xItem);
            }

            try
            {
                TextWriter t = new StreamWriter(TopLevelManager.ApplicationUserDataFolder + "Settings.xml", false, Encoding.UTF8);
                xDoc.Save(t);
                t.Close();
            }
            catch (XmlException e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to save settings. Error text: " + e.Message);
            }
        }

        #endregion

        private string GetFilePrefix()
        {
            string pathPrefix = string.Empty;

            if (m_mode == ManagerMode.Logon)
            {
                pathPrefix = "Logon_";
            }

            return pathPrefix;
        }

        private void SetupLayoutManager()
        {
            string layoutFile = ApplicationUserDataFolder + GetFilePrefix() + m_displays.ToString() + ".xml";
            bool hasLayout = File.Exists(layoutFile);

            bool okay = false;

            if (hasLayout)
            {
                okay = m_layoutManager.LoadLayout(layoutFile);
            }

            if(!okay)
            {
                System.Diagnostics.Debug.WriteLine("Generating empty layout.");

                m_layoutManager.Clear();

                if (m_mode == ManagerMode.Desktop)
                {
                    int numDisplays = m_displays.Screens.Count;
                    for (int i = 0; i < numDisplays; ++i)
                    {
                        m_layoutManager.Add(i, new LayoutCanvas());
                    }
                }
                else
                {
                    m_layoutManager.Add(m_displays.Primary.Index, new LayoutCanvas());
                }
            }

            m_layoutFile = layoutFile;
        }

        private void ResetAll()
        {
            m_layoutManager.SaveLayout(m_layoutFile);
            SetupLayoutManager();

            m_primaryWindow.SetupLayout(m_layoutManager[m_displays.Primary.Index],
                m_displays.Primary, m_mode == ManagerMode.Desktop);

            CentreWindow(m_primaryWindow, m_displays.Primary);

            CloseChildWindows();

            if(m_mode == ManagerMode.Desktop)
                SpawnChildWindows();
        }

        #region Window Management Functions

        private void CentreWindow(Window win, Screen screen)
        {
            win.Left = (screen.Width / 2) - (win.Width / 2) + screen.X;
            win.Top = (screen.Height / 2) - (win.Height / 2) + screen.Y;
        }

        private MainWindow CreateWindow(Screen screen, LayoutCanvas canvas, bool primary)
        {
            MainWindow win = new MainWindow(primary);

            win.Closed += new EventHandler(winChild_Closed);

            win.SetImageLibraryController(m_libraryManager);
            win.SetupLayout(canvas, screen, m_mode == ManagerMode.Desktop);

            CentreWindow(win, screen);
            win.Show();

            return win;
        }

        private void CloseChildWindows()
        {
            //  Close the old ones
            if (m_childWindows.Count > 0)
            {
                for (int i = 0; i < m_childWindows.Count; i++)
                {
                    m_childWindows[i].Closed -= new EventHandler(winChild_Closed);
                    m_childWindows[i].Close();
                }
                m_childWindows.Clear();
            }
        }

        private void SpawnChildWindows()
        {
            foreach (Screen screen in m_displays.Screens)
            {
                if (!screen.IsPrimary)
                {
                    MainWindow win = CreateWindow(screen, m_layoutManager[screen.Index], false);
                    win.Show();
                    m_childWindows.Add(win);
                }
            }
        }

        #endregion

        public void Start()
        {
            SetupLayoutManager();

            MainWindow win = CreateWindow(m_displays.Primary, m_layoutManager[m_displays.Primary.Index], true);

            win.ManagerModeChanged += new MainWindow.ManagerModeChangedHandler(win_ManagerModeChanged);
            win.RenderWallpaper += new EventHandler(win_RenderWallpaper);

            win.resetDisplayDelegate = new ResetDisplaySettingsDelegate(this.ResetDisplaySettings); ;

            m_primaryWindow = win;

            SpawnChildWindows();

            appInstance.Run(win);
        }

        void win_ManagerModeChanged(ManagerMode mode)
        {
            m_mode = mode;
            ResetAll();            
        }

        void win_RenderWallpaper(object sender, EventArgs e)
        {
            m_layoutManager.SaveLayout(m_layoutFile);

            WallpaperController.RenderWallpaper(m_displays, m_layoutManager, 
                m_mode == ManagerMode.Desktop, ApplicationUserDataFolder);
        }

        private void ResetDisplaySettings()
        {
            m_displays.RefreshDisplays();
            ResetAll();
        }

        void winChild_Closed(object sender, EventArgs e)
        {
            appInstance.Shutdown();
        }

    }
}
