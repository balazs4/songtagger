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

        public IArtist GetArtistById(Guid artistId)
        {
            throw new NotImplementedException("TODO");
        }

        public IArtist GetArtist(string nameStub)
        {
            ArtistStubEntity rawArtist = new ArtistStubEntity(nameStub);

            IEnumerable<IArtist> artistList = PerformQuery<ArtistStubEntity, IArtist>(
                rawArtist,
                (source) => !String.IsNullOrWhiteSpace(source.Name),
                MusicBrainzService,
                () => MusicBrainz.CreateArtistQueryUri(rawArtist.Name),
                (xml) => MusicBrainz.ParseXmlToListOf<IArtist>(xml),
                logger);

            IArtist artist = artistList.FirstOrDefault() ?? new UnknownArtist();
       
            logger.Info("...artist: {0}", artist.Name);
            return artist;
        }

        public IEnumerable<IAlbum> GetAlbums(IArtist artist)
        {
            IEnumerable<IAlbum> albumList = PerformQuery<IArtist,IAlbum>(artist,
                                                                         IsNotNullOrEmptyGuid,
                                                                         MusicBrainzService,
                                                                         () => MusicBrainz.CreateQueryUriTo<IAlbum>(artist.Id),
                                                                         (xml) => MusicBrainz.ParseXmlToListOf<IAlbum>(xml),
                                                                         logger);

            foreach (Album album in albumList)
            {
                album.ArtistOfRelease = artist;
            }


            foreach (Album album in albumList.Where(a => a != null && a.Id != Guid.Empty))
            {
                IEnumerable<ICoverArt> coverArts = PerformQuery<IAlbum,ICoverArt>(album,
                                                                                  IsNotNullOrEmptyGuid,
                                                                                  LastFmService, 
                                                                                  () => LastFm.CreateAlbumCoverQueryUri(album), 
                                                                                  (xml) => LastFm.ParseXmlToCoverList(xml), 
                                                                                  logger
                                                                                  );

                ((List<ICoverArt>)album.Covers).AddRange(coverArts);
            }


            return albumList ?? new List<IAlbum>();
        }

        public IEnumerable<IRelease> GetReleases(IAlbum album)
        {
            IEnumerable<IRelease> releaseList = PerformQuery<IAlbum,IRelease>(album,
                                                                              IsNotNullOrEmptyGuid,
                                                                              MusicBrainzService,
                                                                              () => MusicBrainz.CreateQueryUriTo<IRelease>(album.Id),
                                                                              (xml) => MusicBrainz.ParseXmlToListOf<IRelease>(xml),
                                                                              logger);

            foreach (Release release in releaseList)
            {
                release.Album = album;
                release.QuerySongDelegate = GetSongs;
            }

            return releaseList ?? new List<IRelease>();
        }

        public IEnumerable<ISong> GetSongs(IRelease release)
        {
           
            IEnumerable<ISong> songs = PerformQuery<IRelease,ISong>(release,
                                                IsNotNullOrEmptyGuid,
                                                MusicBrainzService, 
                                                () => MusicBrainz.CreateQueryUriTo<ISong>(release.Id), 
                                                (xml) => MusicBrainz.ParseXmlToListOf<ISong>(xml),
                                                logger
            );


            foreach (Song song in songs)
            {
                song.Release = release;
            }

            return songs;
        }
        #endregion



        private static bool IsNotNullOrEmptyGuid<TSource>(TSource entity)
            where TSource : IEntity
        {
            return (entity != null && entity.Id != Guid.Empty);
        }

        private static IEnumerable<TResult> PerformQuery<TSource,TResult>(
            TSource sourceItem,
            Func<TSource,bool> argumentCheck,
            IWebService service, 
            Func<Uri> createUri, 
            Func<XDocument, IEnumerable<TResult>> parseResult, 
            Logger loggerInstance)
            where TSource : IEntity
        {
            if (!argumentCheck(sourceItem))
            {
                loggerInstance.Warn("Unknow {0}...skip", typeof(TSource).Name);
                return new List<TResult>();
            }

            loggerInstance.Info("Search for '{0}' {1}}", sourceItem.Name, typeof(TSource).Name);
            Uri queryUri = createUri();

            loggerInstance.Info("Download content from '{0}'", queryUri.ToString());
            XDocument result = service.ExecuteQuery(queryUri);

            return parseResult(result);
        }

    }
}

