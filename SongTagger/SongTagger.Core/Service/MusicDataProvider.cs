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
using System.Diagnostics;
using NLog;

namespace SongTagger.Core.Service
{
    public class MusicData : IProvider
    {
        private readonly Logger logger;

        #region Singleton pattern
        private static MusicData singletonInstance;

        public static IProvider Provider
        {
            get{ return singletonInstance ?? (singletonInstance = new MusicData());}
        }

        private MusicData()
        {
            logger = LogManager.GetCurrentClassLogger();
        }
        #endregion

        #region Services
        private static IWebService MusicBrainzService
        {
            get
            {
                return WebServices.Instance(ServiceName.MusicBrainz);
            }
        }

        private static IWebService LastFmService
        {
            get
            {
                return WebServices.Instance(ServiceName.LastFm);
            }
        }
        #endregion

        #region IProvider implementation
        public IArtist GetArtist(string nameStub)
        {
            #region Argument check
            if (String.IsNullOrWhiteSpace(nameStub))
                throw new ArgumentException("Artist name stub could not be null or empty", "nameStub");
            #endregion

            logger.Info("Search for '{0}'", nameStub);
            Uri queryUri = MusicBrainz.CreateArtistQueryUri(nameStub);

            logger.Info("Download content from '{0}'", queryUri.ToString());
            XDocument result = MusicBrainzService.ExecuteQuery(queryUri);

            logger.Info("Parse xml content....");
            IEnumerable<IArtist> artistList = MusicBrainz.ParseXmlToListOf<IArtist>(result);

            IArtist artist = artistList.FirstOrDefault() ?? new UnknownArtist();
       
            logger.Info("...artist: {0}", artist.Name);
            return artist;
        }

        public IEnumerable<IAlbum> GetAlbums(IArtist artist)
        {
            #region Argument check
            if (artist == null)
                throw new ArgumentException("artist", "Artist could not be null");

            #endregion
                
            if (artist is UnknownArtist || artist.Id == Guid.Empty)
            {
                logger.Warn("Skip album query because 'UnknownArtist'");
                return new List<IAlbum>();
            }

            logger.Info("Search for albums of '{0}'", artist.Name);
            Uri queryUri = MusicBrainz.CreateQueryUriTo<IAlbum>(artist.Id);

            logger.Info("Download content from '{0}'", queryUri.ToString());
            XDocument result = MusicBrainzService.ExecuteQuery(queryUri);

            logger.Info("Parse xml content....");
            IEnumerable<IAlbum> albumList = MusicBrainz.ParseXmlToListOf<IAlbum>(result);

            foreach (Album album in albumList)
            {
                album.ArtistOfRelease = artist;
            }


            foreach (Album album in albumList.Where(a => a != null && a.Id != Guid.Empty))
            {
                logger.Info("Search cover for '{0}'", album.Name);

                Uri coverQuery = LastFm.CreateAlbumCoverQueryUri(album);
                logger.Info("Download content from '{0}'", coverQuery.ToString());

                XDocument lasfFmResult = LastFmService.ExecuteQuery(coverQuery);

                (album.Covers as List<ICoverArt>).AddRange(LastFm.ParseXmlToCoverList(lasfFmResult));
            }


            return albumList ?? new List<IAlbum>();
        }

        public IEnumerable<IRelease> GetReleases(IAlbum album)
        {
            //TODO: releases; mbid = album.Id
            //http://musicbrainz.org/ws/2/release-group/mbid?inc=releases


            //TODO: songs of releases; mbid = release.Id
            //http://musicbrainz.org/ws/2/release/mbid?inc=recordings


            throw new NotImplementedException();
        }
        #endregion

    }
}

