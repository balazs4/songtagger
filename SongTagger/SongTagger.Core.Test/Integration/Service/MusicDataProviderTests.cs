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

namespace SongTagger.Core.Test.Integration.Service
{
    [TestFixture()]
    public class MusicDataProviderTests
    {
        [TestCase("Rise Against", Result = "606bf117-494f-4864-891f-09d63ff6aa4b")]
        [TestCase("Def Leppard", Result="7249b899-8db8-43e7-9e6e-22f1e736024e")]
        [TestCase("Depresszió", Result="79a8d8a6-012a-4dd9-b5e2-ed4b52a5d55e")]
        [Category("Acceptance")]
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
        [Category("Acceptance")]
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
        [Category("Acceptance")]
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
        [Category("Acceptance")]
        public int D_LookupRecordings(Release release)
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
    }
}

