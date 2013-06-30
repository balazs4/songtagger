//
//  IEntity.cs
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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel;

namespace SongTagger.Core
{
    public interface IEntity
    {
        Guid Id { get; }

        String Name { get; }
    }

    [Serializable]
    [XmlRootAttribute("artist", Namespace="http://musicbrainz.org/ns/mmd-2.0#", IsNullable = false)]
    public class Artist : IEntity
    {
        [XmlAttribute("id")]
        public Guid Id{ get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlArray("tag-list")]
        [XmlArrayItem(typeof(Tag))]
        public List<Tag> Tags { get; set; }

        [XmlAttribute("type")]
        public ArtistType Type { get; set; }

        [XmlAttribute("score", Namespace="http://musicbrainz.org/ns/ext#-2.0")]
        public int Score { get; set; }

        private static Artist instance;

        internal static IEntity Empty
        {
            get { return instance ?? (instance = new Artist());}
        }
    }

    [Serializable]
    [XmlRootAttribute("release-group", Namespace="http://musicbrainz.org/ns/mmd-2.0#", IsNullable = false)]
    public class ReleaseGroup : IEntity
    {
        [XmlAttribute("id")]
        public Guid Id{ get; set; }

        [XmlElement("title")]
        public string Name { get; set; }

        public DateTime FirstReleaseDate { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("first-release-date")]
        public string RawFirstReleaseDate
        {
            get { return FirstReleaseDate.ToString();}
            set
            {
                FirstReleaseDate = string.IsNullOrEmpty(value) 
                    ? DateTime.MinValue
                    : System.Xml.XmlConvert.ToDateTime(value);
            }     
        }

        [XmlElement("primary-type")]
        public ReleaseGroupType PrimaryType { get; set; }

        internal Artist Artist { get; set; }
    }

    public class Release : IEntity
    {
        public Guid Id{ get; set; }

        public string Name { get; set; }

        public ReleaseGroup ReleaseGroup { get; set; }
    }

    public class Recording  : IEntity
    {
        public Guid Id{ get; set; }

        public string Name { get; set; }
    }

    [Serializable]
    [XmlType("tag")]
    public class Tag
    {
        [XmlElement("name")]
        public string Name { get; set; }
    }

    [Serializable]
    public enum ReleaseGroupType
    {
        Undefined = 0,
        Album,
        EP,
        Live,
        Single
    }

    [Serializable]
    public enum ArtistType
    {
        Undefined = 0,
        Group,
        Person
    }
}
