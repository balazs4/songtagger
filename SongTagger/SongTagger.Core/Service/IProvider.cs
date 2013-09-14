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
using System.Security.Policy;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace SongTagger.Core.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using System.Diagnostics;

    public interface IProvider
    {
        IEnumerable<Artist> SearchArtist(string name);

        IEnumerable<ReleaseGroup> BrowseReleaseGroups(Artist artist);

        IEnumerable<Release> BrowseReleases(ReleaseGroup releaseGroup);

        IEnumerable<Track> LookupTracks(Release release);

        void DownloadCoverArts(IEnumerable<Uri> uri, Action<CoverArt> callback, CancellationToken token);
    }

    public class MusicData : IProvider
    {
        #region IProvider implementation
        public IEnumerable<Artist> SearchArtist(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name", "Search text cannot be null or empty");

            return Query<Artist>(Artist.Empty.Search(name))
                .OrderByDescending(a => a.Score)
                .ThenBy(a => a.Name);
        }

        public IEnumerable<ReleaseGroup> BrowseReleaseGroups(Artist artist)
        {
            CheckArgument<Artist>(artist);
            Action<ReleaseGroup> postProcess = (item) => item.Artist = artist;
            return Query<ReleaseGroup>(artist.Browse<ReleaseGroup>(), postProcess)
                .OrderByDescending(rg => rg.FirstReleaseDate.Year)
                .ThenBy(rg => rg.Name);
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

        public void DownloadCoverArts(IEnumerable<Uri> uri, Action<CoverArt> callback, CancellationToken token)
        {
            if (uri == null)
                return;

            var tasks = uri.Select(url => Task<CoverArt>.Factory.StartNew(() => DownloadCovertArtFromUri(url, token), token)
                                                    .ContinueWith(task => callback(task.Result), token))
                                                    .ToArray();

            Task.WaitAll(tasks, token);
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
            IEnumerable<TResult> result;
            if (EntityCache.Instance.TryGetEntity(url, out result))
            {
                return result;
            }

            using (PerformanceTrace tracer = new PerformanceTrace("Query " + typeof(TResult).Name))
            {
                string content = ServiceClient.DownloadContent(url, MusicBrainzPreparation);
                tracer.WriteTrace("Download finished from " + url);

                result = DeserializeContent<TResult>(content);
                tracer.WriteTrace("Deserialization finished");

                if (postProcessActions != null)
                {
                    foreach (Action<TResult> post in postProcessActions)
                    {
                        Parallel.ForEach(result, post);
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

            if (entity.Id == Guid.Empty)
                throw new ArgumentException(typeof(TSource).Name, ".Id cannot be " + Guid.Empty.ToString());
        }

        private static CoverArt DownloadCovertArtFromUri(Uri uri, CancellationToken token)
        {
            byte[] data;
            try
            {
                using (WebClient client = ServiceClient.CreateWebClient())
                {
                    data = client.DownloadData(uri);
                }
            }
            catch (Exception)
            {
                data = null;
            }
            return CoverArt.CreateCoverArt(uri, data);
        }
    }

    internal class EntityCache
    {
        #region Singleton pattern
        private static EntityCache instance;

        internal static EntityCache Instance
        { 
            get{ return instance ?? (instance = new EntityCache());}
        }

        private EntityCache()
        {
            repository = new ConcurrentBag<EntityCacheItem>();
        }
        #endregion
        private ConcurrentBag<EntityCacheItem> repository;

        internal bool TryGetEntity<TValue>(Uri uri, out IEnumerable<TValue> cachedCollection) 
            where TValue : IEntity
        {
            try
            {
                EntityCacheItem cache = repository.First(ch => ch.SearchUri.ToString() == uri.ToString());
                lock (cache)
                {
                    cache.LastAccess = DateTime.Now;
                    cachedCollection = cache.Items.Cast<TValue>();
                }
                Trace.TraceInformation("Cache was used.");
                return true;
            }
            catch (InvalidOperationException)
            {
                cachedCollection = null;
                return false;
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Could not read entity cache. Details: " + e.Message);
                cachedCollection = null;
                return false;
            }
        }

        internal void ClearAll()
        {
            repository = new ConcurrentBag<EntityCacheItem>();
        }

        internal void ClearRecentlyNotUsedItems(TimeSpan? diff = null)
        {
            if (diff == null)
                diff = TimeSpan.FromMinutes(5);

            IEnumerable<EntityCacheItem> toBeDeleted = repository.Where(ch => DateTime.Now - ch.LastAccess > diff).AsEnumerable();
            repository = new ConcurrentBag<EntityCacheItem>(repository.Except(toBeDeleted));
        }

        internal void Add(Uri url, IEnumerable<IEntity> entities)
        {
            repository.Add(new EntityCacheItem(url, entities));
        }
    }

    internal class EntityCacheItem
    {
        public EntityCacheItem(Uri uri, IEnumerable<IEntity> entities)
        {
            SearchUri = uri;
            Items = entities.ToList();
            LastAccess = DateTime.Now;
        }

        internal Uri SearchUri { get; private set; }

        internal IEnumerable<IEntity> Items { get; private set; }

        internal DateTime LastAccess { get; set; }
    }
}
