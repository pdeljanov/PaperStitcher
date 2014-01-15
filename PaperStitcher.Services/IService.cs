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
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace PaperStitcher.Services
{
    
    public interface IService
    {
        /// <summary>
        /// Gets the friendly name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an embedded assembly string path to the icon.
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// Gets the tooltip associated with the plugin category.
        /// </summary>
        string Tooltip { get; }

        /// <summary>
        /// Saves the plugin settings to an output stream.
        /// </summary>
        /// <param name="streamOut">A write-only data stream to save the settings in.</param>
        /// <returns></returns>
        bool SaveSettings(Stream streamOut);

        /// <summary>
        /// Loads plugin settings from an input stream.
        /// </summary>
        /// <param name="streamIn">A read-only data stream to load the settings from.</param>
        /// <returns></returns>
        bool LoadSettings(Stream streamIn);

        /// <summary>
        /// Creates an instasnce of the plugin specific property page.
        /// </summary>
        /// <param name="parent">The parent window to nest the page in.</param>
        /// <returns>Returns a ServicePropertyPage class.</returns>
        ServicePropertyPage CreatePropertyPage(Window parent);

        /// <summary>
        /// Creates an image fetcher based on a ServiceItem.
        /// </summary>
        /// <param name="item">The service item to create the fetcher around.</param>
        /// <returns>Returns an instance of a class that implements IFetcher.</returns>
        IFetcher CreateFetcher( ServiceItem item );

    }
}
