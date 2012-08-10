//
//  MusicBrainz.cs
//
//  Author:
//       balazs4 <balazs4web@gmail.com>
//
//  Copyright (c) 2012 GPLv2 (CodePlex)
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
using System;
using System.Net;

namespace SongTagger.Core
{
    internal class MusicBrainz : IWebService
    {
        internal static Uri WebService
        {
            get;
            private set;
        }

        static MusicBrainz()
        {
            WebService = new Uri("http://musicbrainz.org/ws/2/");
        }

        internal MusicBrainz()
        {
        }


        #region IWebService implementation
        public System.Xml.Linq.XDocument ExecuteQuery(string queryString)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

}

