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
            int actualTimeInSeconds = Convert.ToInt32(watcher.Elapsed.TotalSeconds);
            int expectedTimeInSeconds = Convert.ToInt32(MusicData.MINIMUM_TIME_BETWEEN_QUERIES.TotalSeconds);

            Assert.That(actualTimeInSeconds, 
                        Is.GreaterThanOrEqualTo(expectedTimeInSeconds),
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
        public void DeserializateContent_Artist()
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
        public void DeserializateContent_ReleaseGroup()
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
    }
}

