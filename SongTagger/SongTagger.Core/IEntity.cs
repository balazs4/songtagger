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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
                    : System.Xml.XmlConvert.ToDateTime(value);
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

        public Release()
        {

        }

        public Release(ReleaseGroup group)
            : this()
        {
            ReleaseGroup = group;
        }

        private CoverArt coverArt;

        [XmlIgnore]
        public CoverArt CoverArt
        {
            get { return coverArt ?? (coverArt = GetSpeculativeCoverArt(CoverArtStrategies.ToArray())); }
        }

        private static CoverArt GetSpeculativeCoverArt(params Uri[] uriArray)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();

            Task<CoverArt>[] tasks = uriArray
                .Select(url => Task<CoverArt>.Factory.StartNew(() => GetOptimisticCoverArt(url, cancellation.Token), cancellation.Token))
                .ToArray();

            int index = Task<CoverArt>.WaitAny(tasks);
            cancellation.Cancel();
            return tasks [index].Result;
        }

        private static CoverArt GetOptimisticCoverArt(Uri uri, CancellationToken token)
        {
            CoverArt cover = CoverArt.Empty;
            WebClient client = Service.ServiceClient.CreateWebClient();
            EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.AutoReset);
            client.DownloadDataCompleted += (sender, args) =>
            {
                if (args.Cancelled)
                {
                    handle.Set();
                    return;
                }

                if (args.Error != null)
                {
                    handle.Set();
                    return;
                }

                cover = IsDataImage(args.Result)
                            ? new CoverArt(uri, args.Result)
                            : CoverArt.Empty;

                handle.Set();
            };

            token.Register(() =>
            {
                client.CancelAsync();
                handle.Set();
            });

            client.DownloadDataAsync(uri);
            handle.WaitOne(TimeSpan.FromSeconds(10));
            return cover;
        }

        [XmlIgnore]
        private List<Uri> coverArtUriList;

        [XmlIgnore]
        internal List<Uri> CoverArtStrategies
        {
            get
            {
                if (coverArtUriList == null)
                    coverArtUriList = new List<Uri>();

                if (coverArtUriList.Count == 0)
                    coverArtUriList.Add(this.GetCoverArt());

                return coverArtUriList;
            }
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
            } catch
            {
                return false;
            }
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
        public int Number { get; set; }

        [XmlIgnore]
        public Release Release { get; internal set; }

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

    [Serializable]
    public class CoverArt
    {
        public CoverArt(Uri url, byte[] content)
        {
            Url = url;
            Data = content;
        }

        public Uri Url { get; private set; }

        public byte[] Data { get; private set; }

        public static CoverArt Empty
        {
            get { return new CoverArt(null, null); }
        }
    }

    [Serializable]
    public class VirtualRelease
    {
        public IEnumerable<Song> SongList{ get; private set; }

        public static VirtualRelease Create(IEnumerable<Track> tracks)
        {
            return new VirtualRelease(tracks);
        }

        private VirtualRelease(IEnumerable<Track> tracks)
        {
            SongList = CreateVirtualSongList(tracks);
        }

        private static IEnumerable<Song> CreateVirtualSongList(IEnumerable<Track> tracks)
        {
            List<Song> songs = new List<Song>();
            //TODO
            return songs;
        }
    }

    [Serializable]
    public class Song
    {
        public IEnumerable<Track> Tracks { get; set; }
    }
}
