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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace PaperStitcher2
{

    public delegate void LibraryRemovedItemHandler(LibraryItem item);
    public delegate void LibraryAddItemHandler(LibraryItem item);

    public class ImageLibraryController : KeyedCollection<string, LibraryItem>
    {
        public event LibraryRemovedItemHandler RemovedItem;
        public event LibraryAddItemHandler AddedItem;

        public ImageLibraryController()
        {
        }

        protected override string GetKeyForItem(LibraryItem item)
        {
            return item.Location;
        }

        public void AddSignalled(string path)
        {
            if (this.Contains(path))
                return;

            DirectoryInfo dInfo = new DirectoryInfo(path);

            string name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dInfo.Name);

            LibraryItem item = new LibraryItem(name, path);

            base.Add(item);

            if (AddedItem != null)
                AddedItem(item);
        }

        public void RemoveSignalled(string path)
        {
            LibraryItem item = this[path];

            base.Remove(path);

            if (RemovedItem != null)
                RemovedItem(item);
        }
    }


}
