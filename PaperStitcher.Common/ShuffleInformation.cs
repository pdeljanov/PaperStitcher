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
using System.Xml;
using System.IO;

namespace PaperStitcher.Common
{
    public class ShuffleInformation
    {

        public static DateTime GetEpoch(string file, string layout)
        {
            XmlDocument xDoc = new XmlDocument();

            try
            {
                StreamReader reader = new StreamReader(file, Encoding.UTF8);
                xDoc.Load(reader);
                reader.Close();

                XmlNodeList libraryItems = xDoc.SelectNodes("Shuffle/Epoch");

                foreach (XmlNode item in libraryItems)
                {
                    string key = item.Attributes["Key"].Value;
                    if (key.Equals(layout))
                        return DateTime.Parse(item.InnerText);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load epoch. Error text: " + e.Message);
            }

            return DateTime.MinValue;
        }

        public static void SetEpoch(string file, string layout, DateTime epoch)
        {
            bool changed = false;

            XmlDocument xDoc = new XmlDocument();

            if (File.Exists(file))
            {
                StreamReader r = new StreamReader(file, Encoding.UTF8);
                xDoc.Load(r);
                r.Close();
                XmlNodeList libraryItems = xDoc.SelectNodes("Shuffle/Epoch");

                foreach (XmlNode item in libraryItems)
                {
                    string key = item.Attributes["Key"].Value;
                    if (key.Equals(layout))
                    {
                        item.InnerText = epoch.ToString();
                        changed = true;
                    }
                }

                if (!changed)
                {
                    XmlNode shuffleNode = xDoc.SelectSingleNode("Shuffle");
                    XmlElement xEpoch = xDoc.CreateElement("Epoch");
                    xEpoch.SetAttribute("Key", layout);
                    XmlText xDate = xDoc.CreateTextNode(epoch.ToString());
                    xEpoch.AppendChild(xDate);
                    shuffleNode.AppendChild(xEpoch);
                }
            }
            else
            {
                XmlDeclaration xDeclaration = xDoc.CreateXmlDeclaration("1.0", "utf-8", null);

                XmlElement xRoot = xDoc.CreateElement("Shuffle");
                xDoc.InsertBefore(xDeclaration, xDoc.DocumentElement);
                xDoc.AppendChild(xRoot);

                XmlElement xEpoch = xDoc.CreateElement("Epoch");
                xEpoch.SetAttribute("Key", layout);

                XmlText xDate = xDoc.CreateTextNode(epoch.ToString());

                xEpoch.AppendChild(xDate);
                xRoot.AppendChild(xEpoch);
            }


            TextWriter t = new StreamWriter(file, false, Encoding.UTF8);
            xDoc.Save(t);
            t.Close();
        }



    }
}
