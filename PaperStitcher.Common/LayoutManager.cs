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

using System.Runtime;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using System.Drawing;

namespace PaperStitcher.Common
{
    public class LayoutManager : Dictionary<int, LayoutCanvas>
    {
        public LayoutManager()
        {
        }

        public bool SaveLayout(string filepath)
        {
            //  Save to home + currentFile
            System.Diagnostics.Debug.WriteLine("Saving " + filepath);

            XmlDocument xDoc = new XmlDocument();

            //  Create declaration
            XmlDeclaration xDeclaration = xDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            //  Create root node
            XmlElement xRoot = xDoc.CreateElement("Layout");
            xDoc.InsertBefore(xDeclaration, xDoc.DocumentElement);
            xDoc.AppendChild(xRoot);

            foreach(KeyValuePair<int, LayoutCanvas> kvp in this)
            {
                LayoutCanvas canvas = kvp.Value;

                XmlElement xScreen = xDoc.CreateElement("Screen");
                xScreen.SetAttribute("Key", kvp.Key.ToString());

                int bgInt = canvas.BackgroundColour.ToArgb();
                string bgString = String.Format("#{0}", bgInt.ToString("x"));

                xScreen.SetAttribute("Background", bgString);
                xScreen.SetAttribute("UseShuffle", canvas.IsShuffleEnabled.ToString());
                xScreen.SetAttribute("ShuffleInterval", canvas.ShuffleMode.ToString());
                xScreen.SetAttribute("SingleObjectStyle", ((int)canvas.SingleObjectStyle).ToString());

                xDoc.DocumentElement.PrependChild(xScreen);

                foreach(LayoutObject lo in canvas)
                {
                    XmlElement xObject = xDoc.CreateElement("Object");
                    xObject.SetAttribute("Type", "0");
                    xObject.SetAttribute("X", lo.X.ToString());
                    xObject.SetAttribute("Y", lo.Y.ToString());
                    xObject.SetAttribute("Z", lo.Z.ToString());
                    xObject.SetAttribute("Width", lo.ActualWidth.ToString());
                    xObject.SetAttribute("Height", lo.ActualHeight.ToString());
                    xObject.SetAttribute("FlipX", lo.IsFlippedX.ToString());
                    xObject.SetAttribute("FlipY", lo.IsFlippedY.ToString());
                    
                    XmlElement xSource = xDoc.CreateElement("Source");
                    XmlText xSourcePath = xDoc.CreateTextNode(lo.Serialize());
                    xSource.AppendChild(xSourcePath);

                    XmlElement xEffects = xDoc.CreateElement("Effects");

                    xObject.AppendChild(xSource);
                    xObject.AppendChild(xEffects);

                    xScreen.AppendChild(xObject);
                }

                xRoot.AppendChild(xScreen);
            }

            try
            {
                StreamWriter writer = new StreamWriter(filepath, false, Encoding.UTF8);
                xDoc.Save(writer);
                writer.Close();
                writer = null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error saving. Error message: " + e.Message);
                return false;
            }

            return true;
        }

        public bool LoadLayout(string filepath)
        {
            this.Clear();

            System.Diagnostics.Debug.WriteLine("Loading " + filepath);

            if (!File.Exists(filepath))
            {
                System.Diagnostics.Debug.WriteLine("Layout not found.");
                return false;
            }

            XmlDocument xDoc = new XmlDocument();

            try
            {   
                StreamReader reader = new StreamReader(filepath, Encoding.UTF8);
                xDoc.Load(reader);
                reader.Close();
                reader = null;

                XmlNodeList xScreens = xDoc.SelectNodes("Layout/Screen");

                foreach (XmlNode xScreen in xScreens)
                {
                    int key = int.Parse(xScreen.Attributes["Key"].Value);

                    //  Convert hex colour string into a color value
                    int argb = int.Parse(xScreen.Attributes["Background"].Value.Substring(1), System.Globalization.NumberStyles.AllowHexSpecifier);
                    Color bg = Color.FromArgb((byte)((argb & 0xFF000000) >> 24), (byte)((argb & 0xFF0000) >> 16), (byte)((argb & 0xFF00) >> 8), (byte)(argb & 0xFF));

                    LayoutCanvas canvas = new LayoutCanvas();

                    canvas.BackgroundColour = bg;
                    canvas.IsShuffleEnabled = bool.Parse(xScreen.Attributes["UseShuffle"].Value);
                    canvas.ShuffleMode = ShuffleMode.Parse(xScreen.Attributes["ShuffleInterval"].Value);
                    canvas.SingleObjectStyle = (PlacementStyle)int.Parse(xScreen.Attributes["SingleObjectStyle"].Value);

                    XmlNodeList xObjects = xScreen.ChildNodes;
                    foreach (XmlNode xObject in xObjects)
                    {
                        int type = int.Parse(xObject.Attributes["Type"].Value);
                        int x = int.Parse(xObject.Attributes["X"].Value);
                        int y = int.Parse(xObject.Attributes["Y"].Value);
                        int z = int.Parse(xObject.Attributes["Z"].Value);
                        int width = int.Parse(xObject.Attributes["Width"].Value);
                        int height = int.Parse(xObject.Attributes["Height"].Value);
                        
                        bool fx = bool.Parse(xObject.Attributes["FlipX"].Value);
                        bool fy = bool.Parse(xObject.Attributes["FlipY"].Value);

                        string source = string.Empty;

                        XmlNodeList xProps = xObject.ChildNodes;
                        foreach (XmlNode xProp in xProps)
                        {
                            if (xProp.Name == "Source")
                            {
                                source = xProp.InnerText;
                            }
                        }

                        SimpleImageObject sio = new SimpleImageObject(source);
                        sio.X = x;
                        sio.Y = y;
                        sio.Z = z;
                        sio.ActualWidth = width;
                        sio.ActualHeight = height;
                        sio.IsFlippedX = fx;
                        sio.IsFlippedY = fy;

                        canvas.Add(sio);
                    }

                    this.Add(key, canvas);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load settings. Error text: " + e.Message);
                return false;
            }

            return true;
        }

    }

}
