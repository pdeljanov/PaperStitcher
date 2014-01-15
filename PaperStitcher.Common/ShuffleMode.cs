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

namespace PaperStitcher.Common
{

    public class ShuffleMode
    {
        private ShuffleType m_type;
        private TimeSpan m_interval;

        public ShuffleType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        public TimeSpan Interval
        {
            get { return m_interval; }
            set
            {
                m_type = ShuffleType.Interval;
                m_interval = value;
            }
        }

        public bool IsInterval
        {
            get { return (m_type == ShuffleType.Interval); }
        }

        public override string ToString()
        {
            switch (m_type)
            {
                case ShuffleType.Daily:
                    return "Daily";
                case ShuffleType.Monthly:
                    return "Monthly";
                case ShuffleType.Weekly:
                    return "Weekly";
                default:
                    return m_interval.ToString();
            }
        }

        public static ShuffleMode Parse(string input)
        {
            ShuffleMode result = new ShuffleMode();

            if (input == "Daily")
            {
                result.Type = ShuffleType.Daily;
            }
            else if (input == "Weekly")
            {
                result.Type = ShuffleType.Weekly;
            }
            else if (input == "Monthly")
            {
                result.Type = ShuffleType.Monthly;
            }
            else
            {
                try
                {
                    result.Interval = TimeSpan.Parse(input); ;
                }
                catch (Exception)
                {
                    result.Interval = TimeSpan.FromMinutes(1);
                }
            }

            return result;
        }

    }
}
