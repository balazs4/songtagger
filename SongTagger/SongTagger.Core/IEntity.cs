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
using System.Xml;

namespace SongTagger.Core
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using System.ComponentModel;
    using System.Collections.Concurrent;

    public interface IEntity
    {
        Guid Id { get; }

        String Name { get; }
    }

    [Serializable]
    [XmlRootAttribute("artist", Namespace = "http://musicbrainz.org/ns/mmd-2.0#", IsNullable = false)]
    public class Artist : IEntity
    {
        [XmlAttribute("id")]
        public Guid Id { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlArray("tag-list")]
        [XmlArrayItem(typeof(Tag))]
        public List<Tag> Tags { get; set; }

        [XmlAttribute("type")]
        public ArtistType Type { get; set; }

        [XmlAttribute("score", Namespace = "http://musicbrainz.org/ns/ext#-2.0")]
        public int Score { get; set; }

        private static Artist instance;

        internal static IEntity Empty
        {
            get { return instance ?? (instance = new Artist()); }
        }

        public Artist()
        {
            Tags = new List<Tag>();
        }
    }

    [Serializable]
    [XmlRootAttribute("release-group", Namespace = "http://musicbrainz.org/ns/mmd-2.0#", IsNullable = false)]
    public class ReleaseGroup : IEntity
    {
        [XmlAttribute("id")]
        public Guid Id { get; set; }

        [XmlElement("title")]
        public string Name { get; set; }

        public DateTime FirstReleaseDate { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("first-release-date")]
        public string RawFirstReleaseDate
        {
            get { return FirstReleaseDate.ToString(); }
            set
            {
                FirstReleaseDate = string.IsNullOrEmpty(value)
                    ? DateTime.MinValue
                    : System.Xml.XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Local);
            }
        }

        [XmlElement("primary-type")]
        public ReleaseGroupType PrimaryType { get; set; }

        [XmlIgnore]
        public Artist Artist { get; internal set; }

        public ReleaseGroup()
        {

        }

        public ReleaseGroup(Artist artist)
            : this()
        {
            Artist = artist;
        }
    }

    [Serializable]
    [XmlRootAttribute("release", Namespace = "http://musicbrainz.org/ns/mmd-2.0#", IsNullable = false)]
    public class Release : IEntity
    {
        [XmlAttribute("id")]
        public Guid Id { get; set; }

        [XmlElement("title")]
        public string Name { get; set; }

        [XmlElement("status")]
        public string Status { get; set; }

        [XmlElement("country")]
        public string Country { get; set; }

        [XmlIgnore]
        public ReleaseGroup ReleaseGroup { get; internal set; }

        [XmlIgnore]
        public bool HasPreferredCoverArt{ get { return CoverArtArchiveInfo.IsArtworkAvailable; } }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("cover-art-archive", typeof(CoverArtArchive))]
        public CoverArtArchive CoverArtArchiveInfo { get; set; }

        public Release()
        {
            CoverArtArchiveInfo = new CoverArtArchive();
        }

        public Release(ReleaseGroup group)
            : this()
        {
            ReleaseGroup = group;
        }
    }

    [Serializable]
    [XmlRootAttribute("track", Namespace = "http://musicbrainz.org/ns/mmd-2.0#", IsNullable = false)]
    public class Track : IEntity
    {
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("recording")]
        public Recording Record { get; set; }

        public Track()
        {
            Record = new Recording();
            DiscNumber = 1;
        }

        [XmlAttribute("id")]
        public Guid Id { get; set; }

        [XmlIgnore]
        public string Name
        {
            get { return Record.Name; }
            set { Record.Name = value; }
        }

        [XmlIgnore]
        public TimeSpan Length
        {
            get { return TimeSpan.FromMilliseconds(Record.Length); }
            set { Record.Length = Convert.ToInt32(value.TotalMilliseconds); }
        }

        [XmlElement("position")]
        public int Posititon { get; set; }

        [XmlElement("number")]
        public string Number { get; set; }

        [XmlIgnore]
        public Release Release { get; internal set; }

        [XmlIgnore]
        public int DiscNumber { get; internal set; } 

        public Track(Release release)
            : this()
        {
            Release = release;
        }
    }

    [Serializable]
    [XmlRootAttribute("recording", Namespace = "http://musicbrainz.org/ns/mmd-2.0#", IsNullable = false)]
    public class Recording : IEntity
    {
        [XmlAttribute("id")]
        public Guid Id { get; set; }

        [XmlElement("title")]
        public string Name { get; set; }

        [XmlElement("length")]
        public int Length { get; set; }
    }

    [Serializable]
    [XmlType("tag")]
    public class Tag
    {
        [XmlElement("name")]
        public string Name { get; set; }
    }

    [Serializable]
    [XmlType("cover-art-archive")]
    public class CoverArtArchive
    {
        [XmlElement("artwork")]
        public bool IsArtworkAvailable { get; set; }
    }

    [Serializable]
    public enum ReleaseGroupType
    {
        Undefined = 0,
        Album,
        EP,
        Live,
        Single,
        Other,
        Broadcast
    }

    [Serializable]
    public enum ArtistType
    {
        Undefined = 0,
        Group,
        Person
    }

    public class CoverArt
    {
        private CoverArt(Uri url, byte[] content)
        {
            Url = url;
            Data = content;
            cache.AddOrUpdate(this, DateTime.Now, (k,v) => DateTime.Now);
        }

        public Uri Url { get; private set; }

        public byte[] Data { get; private set; }

        internal static CoverArt Empty
        {
            get { return new CoverArt(null, null); }
        }

        public static CoverArt CreateCoverArt(Uri uri, byte[] data)
        {
            if (IsDataImage(data))
                return new CoverArt(uri, data);

            return CoverArt.Empty;
        }

        private static bool IsDataImage(byte[] data)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (Image.FromStream(stream))
                    {
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        
        }
        #region Cache
        private static ConcurrentDictionary<CoverArt, DateTime> cache = new ConcurrentDictionary<CoverArt, DateTime>();

        internal static void ClearRecentlyNotUsedItems(TimeSpan? diff = null)
        {
            if (diff == null)
                diff = TimeSpan.FromMinutes(5);
            var toBeDeleted = cache.Where(kv => DateTime.Now - kv.Value > diff);
            cache = new ConcurrentDictionary<CoverArt, DateTime>(cache.Except(toBeDeleted));
        }

        internal static void ClearAll()
        {
            cache = new ConcurrentDictionary<CoverArt, DateTime>();
        }

        internal static bool TryGetCoverArt(Uri uri, out CoverArt cover)
        {
            try
            {
                var cachedItem = cache.First(kv => kv.Key.Url.ToString() == uri.ToString() && kv.Key.Data != null);
                cache.TryUpdate(cachedItem.Key, DateTime.Now, cachedItem.Value);
                cover = cachedItem.Key;
                return true;
            }
            catch
            {
                cover = default(CoverArt);
                return false;
            }
        }
        #endregion
    }
}
