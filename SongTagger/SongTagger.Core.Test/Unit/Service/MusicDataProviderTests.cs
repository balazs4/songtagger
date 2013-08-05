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
using SongTagger.Core.Service;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SongTagger.Core.Test.Unit.Service
{
    [TestFixture()]
    public class MusicDataProviderTests
    {
      
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [ExpectedException(typeof(ArgumentException))]
        public void SearchArtist_IfSearchTextNullOrEmpty(string nullOrEmpty)
        {
            MusicData.Provider.SearchArtist(nullOrEmpty);
        }

        [Test]
        [Category("MusicBrainz")]
        public void MusicBrainzPreparation()
        {
            Stopwatch watcher = Stopwatch.StartNew();
            MusicData.MusicBrainzPreparation(DateTime.MinValue);
            Assert.That(watcher.Elapsed.TotalMilliseconds, 
                        Is.Not.GreaterThanOrEqualTo(MusicData.MINIMUM_TIME_BETWEEN_QUERIES.TotalMilliseconds),
                        "First query time...");

            watcher = Stopwatch.StartNew();
            MusicData.MusicBrainzPreparation(DateTime.Now);
            watcher.Stop();
            double actualTime = Math.Round(watcher.Elapsed.TotalSeconds, 2);
            double expectedTime = Math.Round(MusicData.MINIMUM_TIME_BETWEEN_QUERIES.TotalSeconds, 2);

            Assert.That(actualTime, 
                        Is.GreaterThanOrEqualTo(expectedTime),
                        "Time diff between queries..");
        }

        internal static string GetContent(string testFile)
        {
            string file = TestHelper.GetInputDataFilePath(testFile);
            if (!File.Exists(file))
                Assert.Fail("Missing file: {0}", testFile);

            return File.ReadAllText(file);
        }

        [Test]
        public void DeserializeContent_Artist()
        {
            string content = GetContent("MusicBrainz.Artist.Search.xml");

            IEnumerable<Artist> result = MusicData.DeserializeContent<Artist>(content);

            Assert.IsNotNull(result);
            CollectionAssert.IsNotEmpty(result);
            Assert.IsTrue(result.All(a => a != null), "result contains one or more null artist");
            Assert.IsTrue(result.Any(a => a.Name == "Rise Against"), "Not found expected artist");

            Artist artist = result.First(a => a.Name == "Rise Against");
            Assert.AreEqual("606bf117-494f-4864-891f-09d63ff6aa4b", artist.Id.ToString(), "Artist id");
            Assert.AreEqual(9, artist.Tags.Count(), "Tags count");
            Assert.AreEqual(ArtistType.Group, artist.Type, "artist type");
            Assert.AreEqual(100, artist.Score, "Score");
        }

        [Test]
        public void DeserializeContent_ReleaseGroup()
        {
            string content = GetContent("MusicBrainz.ReleaseGroup.Browse.xml");

            IEnumerable<ReleaseGroup> result = MusicData.DeserializeContent<ReleaseGroup>(content);

            Assert.IsNotNull(result);
            CollectionAssert.IsNotEmpty(result);
            Assert.IsTrue(result.All(a => a != null), "result contains one or more null artist");
            Assert.IsTrue(result.Any(a => a.Name == "Appeal to Reason"), "Not found expected release group");

            ReleaseGroup item = result.First(a => a.Name == "Appeal to Reason");
            Assert.AreEqual("0b0e4477-4b04-3683-8f01-3a4544c36b41", item.Id.ToString(), "Release group id");
            Assert.AreEqual(2008, item.FirstReleaseDate.Year, "First release date");
            Assert.AreEqual(ReleaseGroupType.Album, item.PrimaryType, "Primary type");
        }

        [Test]
        public void DeserializeContent_Release()
        {
            string content = GetContent("MusicBrainz.Release.Browse.xml");

            IEnumerable<Release> result = MusicData.DeserializeContent<Release>(content);

            Assert.IsNotNull(result);
            CollectionAssert.IsNotEmpty(result);
            Assert.IsTrue(result.All(a => a != null), "result contains one or more null");
            Assert.IsTrue(result.All(a => a.Name == "Appeal to Reason"), "result contains other releases too");

            Release release = result.First(a => a.Id.ToString() == "205f2019-fc18-477a-971c-ecc37aa216fc");
            Assert.AreEqual("DE", release.Country, "Release country");
            Assert.AreEqual("Official", release.Status, "Release status");
            Assert.AreEqual("Appeal to Reason", release.Name, "Release name");

            Assert.AreEqual(result.Count(r => r.HasPreferredCoverArt), 3, "Preferred cover art count");
        }

        [Test]
        public void DeserializeContent_Track()
        {
            string content = GetContent("MusicBrainz.Recording.Lookup.xml");
            
            IEnumerable<Track> result = MusicData.DeserializeContent<Track>(content);

            Assert.IsNotNull(result);
            CollectionAssert.IsNotEmpty(result);
            Assert.IsTrue(result.All(a => a != null), "result contains one or more null item");

            Track track = result.First(a => a.Id.ToString() == "e5265bc0-c138-34c4-ba9c-a8c366acad6c");
            Assert.AreEqual("The Dirt Whispered", track.Name, "wrong title");
            Assert.AreEqual(4, track.Number, "wrong number");
            Assert.AreEqual(4, track.Posititon, "wrong position");
            Assert.AreEqual(TimeSpan.FromMilliseconds(189126), track.Length, "wrong length");
        }
    }
}

