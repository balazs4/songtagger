//
//  IProvider.cs
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
using System.Net;
using System.Threading;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SongTagger.Core.Service
{
    public interface IProvider
    {
        IEnumerable<Artist> SearchArtist(string name);

        IEnumerable<ReleaseGroup> BrowseReleaseGroups(Artist artist);

        IEnumerable<Release> BrowseReleases(ReleaseGroup releaseGroup);

        IEnumerable<Track> LookupTracks(Release release);
    }

    public class MusicData : IProvider
    {
        #region IProvider implementation
        public IEnumerable<Artist> SearchArtist(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name", "Search text cannot be null or empty");

            return Query<Artist>(Artist.Empty.Search(name))
                .OrderByDescending(a => a.Score);
        }

        public IEnumerable<ReleaseGroup> BrowseReleaseGroups(Artist artist)
        {
            CheckArgument<Artist>(artist);
            Action<ReleaseGroup> postProcess = (item) => item.Artist = artist;
            return Query<ReleaseGroup>(artist.Browse<ReleaseGroup>(), postProcess)
                .OrderByDescending(rg => rg.FirstReleaseDate.Year);
        }

        public IEnumerable<Release> BrowseReleases(ReleaseGroup releaseGroup)
        {
            CheckArgument<ReleaseGroup>(releaseGroup);
            Action<Release> postProcess = (item) => item.ReleaseGroup = releaseGroup;
            return Query<Release>(releaseGroup.Browse<Release>(), postProcess);
        }

        public IEnumerable<Track> LookupTracks(Release release)
        {
            CheckArgument<Release>(release);
            Action<Track> postProcess = (item) => item.Release = release;
            return Query<Track>(release.Lookup<Recording>(), postProcess)
                .OrderBy(t => t.Posititon);
        }
        #endregion

        #region Singleton pattern
        private MusicData()
        {
            
        }

        private static MusicData instance;

        public static MusicData Provider
        { 
            get
            { 
                return instance ?? (instance = new MusicData());  
            }
        }
        #endregion

        #region Download prepare actions
        internal static readonly TimeSpan MINIMUM_TIME_BETWEEN_QUERIES = new TimeSpan(0, 0, 0, 1, 300);

        internal static void MusicBrainzPreparation(DateTime date)
        {
            TimeSpan difference = DateTime.Now - date;
            if (difference < MINIMUM_TIME_BETWEEN_QUERIES)
                Thread.Sleep(MINIMUM_TIME_BETWEEN_QUERIES - difference);
        }

        internal static void NoPreparaiton(DateTime date)
        {
            return;
        }
        #endregion

        internal static IEnumerable<TResult> DeserializeContent<TResult>(string content)
        {
            ConcurrentBag<TResult> result = new ConcurrentBag<TResult>();
            using (StringReader input = new StringReader(content))
            {
                XDocument doc = XDocument.Load(input);
                XName xName = XName.Get(MusicBrainzExtension.GetMusicBrainzEntityName(typeof(TResult)), 
                                        doc.Root.GetDefaultNamespace().NamespaceName);

                IEnumerable<XElement> elements = doc.Descendants(xName);
                XmlSerializer serializer = new XmlSerializer(typeof(TResult));

                Action<XElement> deserialization = (element) => 
                {
                    using (XmlReader reader = element.CreateReader(ReaderOptions.OmitDuplicateNamespaces))
                    {
                        result.Add((TResult)serializer.Deserialize(reader));
                    }   
                };

                Parallel.ForEach(elements.AsParallel(), deserialization);
            }
            return result;
        }

        private static IEnumerable<TResult> Query<TResult>(Uri url, params Action<TResult>[] postProcessActions) where TResult : IEntity
        {
            using (PerformanceTrace tracer = new PerformanceTrace("Query " + typeof(TResult).Name))
            {
                string content = ServiceClient.DownloadContent(url, MusicBrainzPreparation);
                tracer.WriteTrace("Download finished from " + url.ToString());

                IEnumerable<TResult> result = DeserializeContent<TResult>(content);
                tracer.WriteTrace("Deserialization finished");

                if (postProcessActions != null)
                {
                    foreach (Action<TResult> post in postProcessActions)
                    {
                        Parallel.ForEach(result.AsParallel(), post);    
                    }
                    tracer.WriteTrace("Postprocessing finished");
                }
                return result;
            }
        }

        private static void CheckArgument<TSource>(TSource entity) where TSource : IEntity
        {
            if (entity == null)
                throw new ArgumentNullException(typeof(TSource).Name, " cannot be null");

            if (entity.Id == null)
                throw new ArgumentNullException(typeof(TSource).Name, ".Id cannot be null");

            if (entity.Id == Guid.Empty)
                throw new ArgumentException(typeof(TSource).Name, ".Id cannot be " + Guid.Empty.ToString());
        }
    }
}
