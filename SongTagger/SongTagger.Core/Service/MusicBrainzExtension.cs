//
//  MusicBrainzExtension.cs
//
//  Author:
//       balazs4 <balazs4web@gmail.com>
//
//  Copyright (c) 2013 
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
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Xml.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Threading.Tasks;

namespace SongTagger.Core
{
    internal static class MusicBrainzExtension
    {
        #region Helper stuff

        internal static string GetMusicBrainzEntityName(Type type)
        {
            if (!mapping.ContainsKey(type))
                throw new NotSupportedException(type + " is currently not supported");

            return mapping [type];
        }

        private static Dictionary<Type, string> mapping = new Dictionary<Type, string>
        {
            {typeof(Artist),"artist"},
            {typeof(ReleaseGroup), "release-group"},
            {typeof(Release), "release"},
            {typeof(Recording), "recording"},
            {typeof(Track), "track"}
        };

        private static Uri baseUri = new Uri("http://musicbrainz.org/ws/2/");
        #endregion

        internal static Uri Search(this IEntity entity, string searchText)
        {
            string mbEntity = GetMusicBrainzEntityName(entity.GetType());
            string escapedSearchText = Uri.EscapeUriString(searchText)
                .Replace("%20", "+")
                .Replace(" ", "+");

            string relativeUrl = string.Format("{0}?query={1}", mbEntity, escapedSearchText);
            return new Uri(baseUri, relativeUrl);
        }

        internal static Uri Lookup<T>(this IEntity entity)
        {
            string relativeUrl = string.Format("{0}/{1}?inc={2}s",
                                               GetMusicBrainzEntityName(entity.GetType()),
                                               entity.Id.ToString(),
                                               GetMusicBrainzEntityName(typeof(T))
                                               );

            return new Uri(baseUri, relativeUrl);
        }

        internal static Uri Browse<T>(this IEntity entity,int limit = 200) where T : IEntity
        {
            string relativeUrl = string.Format("{0}?{1}={2}&limit={3}",
                                               GetMusicBrainzEntityName(typeof(T)),
                                               GetMusicBrainzEntityName(entity.GetType()),
                                               entity.Id.ToString(),
                                               limit
                                               );

            return new Uri(baseUri, relativeUrl);
        }

    }
}
