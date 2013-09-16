//
//  MusicDataProviderTests.cs
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
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using SongTagger.Core.Service;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SongTagger.Core.Test.Integration.Service
{
    [TestFixture()]
    [Category("Acceptance")]
    public class MusicDataProviderTests
    {
        [TestCase("Rise Against", Result = "606bf117-494f-4864-891f-09d63ff6aa4b")]

        [TestCase("Depresszió", Result="79a8d8a6-012a-4dd9-b5e2-ed4b52a5d55e")]
        public string A_SearchArtistTest(string name)
        {   
            IEnumerable<Artist> result = MusicData.Provider.SearchArtist(name);

            Assert.IsNotNull(result, "Result collection null");
            CollectionAssert.IsNotEmpty(result, "Result collection is empty");
            Assert.IsTrue(result.Any(a => a.Name == name), "result does not contain the candidate");

            return result.First(a => a.Name == name).Id.ToString();
        }

        internal static IEnumerable ArtistSource
        {
            get
            {
                yield return new TestCaseData(TestHelper.RiseAgainst).Returns(22);
            }
        }

        [TestCaseSource("ArtistSource")]
        public int B_BrowseReleaseGroupsTest(Artist artist)
        {
            IEnumerable<ReleaseGroup> result = MusicData.Provider.BrowseReleaseGroups(artist);
            Assert.IsNotNull(result, "Result collection null");
            CollectionAssert.IsNotEmpty(result, "Result collection is empty");
            Assert.IsTrue(result.All(rg => rg.Artist == artist), "Artist of release group is wrong");
            return result.Count();
        }

        internal static IEnumerable ReleaseGroupSource
        {
            get
            {
                yield return new TestCaseData(TestHelper.AppealToReason).Returns(9);
            }
        }

        [TestCaseSource("ReleaseGroupSource")]
        public int C_BrowseReleaseTest(ReleaseGroup releaseGroup)
        {
            IEnumerable<Release> result = MusicData.Provider.BrowseReleases(releaseGroup);
            Assert.IsNotNull(result, "Result collection null");
            CollectionAssert.IsNotEmpty(result, "Result collection is empty");
            Assert.AreEqual(result.Count(), result.Count(r => r.ReleaseGroup == releaseGroup), "Wrong release group");
            Assert.AreEqual(result.Count(), result.Count(r => r.Name == releaseGroup.Name), "Wrong release in the collection");
            return result.Count();
        }

        internal static IEnumerable ReleaseSource
        {
            get
            {
                yield return new TestCaseData(TestHelper.AppealToReasonRelease).Returns(14);
            }
        }

        [TestCaseSource("ReleaseSource")]
        public int D_LookupTracks(Release release)
        {
            IEnumerable<Track> result = MusicData.Provider.LookupTracks(release);

            Assert.IsNotNull(result, "Result collection null");
            CollectionAssert.IsNotEmpty(result, "Result collection is empty");
            Assert.AreEqual(result.Count(), result.Count(r => r.Release == release), "Wrong release");

            List<Track> resultList = result.ToList();
            foreach (Track item in resultList)
            {
                int actualPosition = resultList.IndexOf(item) + 1;
                int expectedPosition = item.Posititon;
                Assert.AreEqual(expectedPosition, actualPosition, "Wrong position");
            }

            return result.Count();
        }

        [TestCase("Def Leppard", Result="7249b899-8db8-43e7-9e6e-22f1e736024e")]
        public string E_EntityCacheTest(string name)
        {
            Stopwatch watcher = Stopwatch.StartNew();
            IEnumerable<Artist> queryResult = MusicData.Provider.SearchArtist(name);
            watcher.Stop();
            Artist artist = queryResult.First(a => a.Name == name);
            TimeSpan referenceTime = watcher.Elapsed;

            Console.WriteLine("REF ##: " + referenceTime);
            List<TimeSpan> laps = new List<TimeSpan>();
            for (int i = 0; i < 5; i++)
            {
                watcher = Stopwatch.StartNew();
                queryResult = MusicData.Provider.SearchArtist(name);
                watcher.Stop();
                Artist entity = queryResult.First(a => a.Name == name);
                Assert.AreEqual(artist.Id, entity.Id);
                Assert.AreEqual(artist.Name, entity.Name);
                Assert.AreSame(artist, entity);
                laps.Add(watcher.Elapsed);
                Console.WriteLine("LAP #" + i + ": " + laps.Last());
            }
            Assert.IsTrue(laps.All(time => time < referenceTime), "Time compare...");

            return artist.Id.ToString();
        }

        [Test]
        public void F_CoverArtCacheTest()
        {
            string url = TestHelper.GetInputDataFilePath("DefLeppard-Erlangen.png");
            Uri uri = new Uri(url);
            CoverArt reference = null;
            Stopwatch referenceWatcher = Stopwatch.StartNew();
            MusicData.Provider.DownloadCoverArts(new Uri[] { uri }, ca => reference = ca, CancellationToken.None);
            referenceWatcher.Stop();
            Assert.IsNotNull(reference, "Reference coverart is null");
            Assert.IsNotNull(reference.Data, "Reference.Data is null");
            Assert.IsNotNull(reference.Url, "Reference.Url is null");
            Console.WriteLine("REF ####:" + referenceWatcher.Elapsed);

            for (int i = 0; i < 5; i++)
            {
                CoverArt current = null;
                Stopwatch watcher = Stopwatch.StartNew();
                MusicData.Provider.DownloadCoverArts(new Uri[] { uri }, ca => current = ca, CancellationToken.None);
                watcher.Stop();
                Assert.AreSame(reference, current, "Not the same objects");
                Assert.AreSame(reference.Data, current.Data, "Not the same data");
                Assert.AreSame(reference.Url, current.Url, "Not the same url");
                Console.WriteLine("Cache #" + i + ": " + watcher.Elapsed);
                Assert.LessOrEqual(watcher.ElapsedMilliseconds, referenceWatcher.ElapsedMilliseconds);
            }
        }
    }
}

