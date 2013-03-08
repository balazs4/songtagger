//
//  MusicDataProviderTests.cs
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
using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Moq;
using SongTagger.Core.Service;

namespace SongTagger.Core.Test.Integration.Service
{
    [TestFixture()]
    [Explicit]
    public class MusicDataProviderTests
    {

        [TestCase("Rise Against",Result = "606bf117-494f-4864-891f-09d63ff6aa4b")]
        [TestCase("Korn",Result = "ac865b2e-bba8-4f5a-8756-dd40d5e39f46")]
        [TestCase("4Lyn",Result = "03df376e-f696-4df0-a8e4-3bbc9c8c1c5d")]
        [TestCase("LiveOnRelease",Result = "70bd46ea-684a-4a2b-a3b6-4bf825476e25")]
        public string GetArtistTest(string artistName)
        {
            IArtist artist = null;
            Assert.That(() => {
                artist = MusicData.Provider.GetArtist(artistName);
            }, Throws.Nothing);

            Assert.That(artist, Is.Not.Null);
            Assert.That(artist, Is.Not.InstanceOf<UnknownArtist>());
            StringAssert.AreEqualIgnoringCase(artistName, artist.Name);

            return artist.Id.ToString();
        }

        [TestCaseSource(typeof(AlbumTestCaseSources),"AlbumSource")]
        public int GetAlbumTest(IArtist artist)
        {
            IEnumerable<IAlbum> releases = null;

            Assert.That(
                () => {
                releases = MusicData.Provider.GetAlbums(artist);
            }, Throws.Nothing);

            Assert.That(releases, Is.Not.Null);
            CollectionAssert.AllItemsAreNotNull(releases);
            CollectionAssert.AllItemsAreUnique(releases);

            Assert.That(releases.All(a => a.ArtistOfRelease == artist), Is.True);

            Assert.That(releases.Any(a => a.Covers.Count > 0), Is.True);

            return releases.Count();
        }
    }

    class AlbumTestCaseSources
    {

        private static IArtist CreateMock(string name, string id)
        {
            Mock<IArtist> mock = new Mock<IArtist>();
            mock.Setup(a => a.Id).Returns(new Guid(id));
            mock.Setup(a => a.Name).Returns(name);
            return mock.Object as IArtist;
        }

        private IArtist DefLeppard
        {
            get{ return CreateMock("Def Leppard", "7249b899-8db8-43e7-9e6e-22f1e736024e");}
        }
        private IArtist RiseAgainst
        {
            get{ return CreateMock("Rise Against", "606bf117-494f-4864-891f-09d63ff6aa4b");}
        }
        private IArtist Depresszio
        {
            get{ return CreateMock("Depresszió", "79a8d8a6-012a-4dd9-b5e2-ed4b52a5d55e");}
        }
        private IArtist Deftones
        {
            get{ return CreateMock("Deftones", "7527f6c2-d762-4b88-b5e2-9244f1e34c46");}
        }




        internal IEnumerable AlbumSource
        {
            get
            {
                yield return new TestCaseData(DefLeppard).Returns(91); // PO....
                yield return new TestCaseData(RiseAgainst).Returns(20);
                yield return new TestCaseData(Depresszio).Returns(3);
                yield return new TestCaseData(Deftones).Returns(54);
            }
        }
    }
}

