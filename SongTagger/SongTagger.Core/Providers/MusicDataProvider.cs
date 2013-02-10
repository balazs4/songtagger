//
//  MusicDataProvider.cs
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
using System.Xml.Linq;
using System.Linq;

namespace SongTagger.Core
{
    public class MusicData : IProvider
    {
        private int MinimumScore { get; set; }

        #region Singleton pattern
        private static MusicData singletonInstance;

        public static IProvider Provider
        {
            get
            {
                if (singletonInstance == null)
                    singletonInstance = new MusicData();

                return singletonInstance as IProvider;
            }
        }

        private MusicData()
        {
            MinimumScore = 100;
        }
        #endregion

        private IWebService MusicBrainzService
        {
            get
            {
                return WebServices.Instance(ServiceName.MusicBrainz);
            }
        }

        private IWebService LastFmService
        {
            get
            {
                return WebServices.Instance(ServiceName.LastFm);
            }
        }

        #region IProvider implementation
        public IArtist GetArtist(string nameStub)
        {
            #region Argument check
            if (String.IsNullOrWhiteSpace(nameStub))
                throw new ArgumentException("Artist name stub could not be null or empty", "nameStub");
            #endregion

            Uri queryUri = MusicBrainz.CreateArtistQueryUri(nameStub);
            XDocument result = MusicBrainzService.ExecuteQuery(queryUri);

            IArtist artist = MusicBrainz.ParseXmlToArtist(result, MinimumScore);

            return artist;
        }

        public IEnumerable<IAlbum> GetReleases(IArtist artist)
        {
            #region Argument check
            if (artist == null)
                throw new ArgumentException("artist", "Artist could not be null");

            #endregion
                
            if (artist is UnknowArtist || artist.Id == Guid.Empty)
            {
                return new List<IAlbum>();
            }

            Uri queryUri = MusicBrainz.CreateAlbumQueryUri(artist.Id);
            XDocument result = MusicBrainzService.ExecuteQuery(queryUri);
            IEnumerable<IAlbum> albumList = MusicBrainz.ParseXmlToAlbum(result);

            foreach (Album album in albumList)
            {
                album.ArtistOfRelease = artist;
            }



            return albumList ?? new List<IAlbum>();
        }
        #endregion

    }
}

