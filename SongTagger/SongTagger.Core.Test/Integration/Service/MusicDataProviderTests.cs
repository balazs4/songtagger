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

        [TestCase("b2ea6506-645e-4935-8d82-84c3a95fe7f0", Result= "Cro")]
        [TestCase("4d7928cd-7ed2-4282-8c29-c0c9f966f1bd", Result= "Alice Cooper")]
        [TestCase("7249b899-8db8-43e7-9e6e-22f1e736024e", Result= "Def Leppard")]
        public string GetArtistByIdTest(string id)
        {
            IArtist result = MusicData.Provider.GetArtist(id);
            return result.Name;
        }

        [TestCaseSource(typeof(AlbumTestCaseSources),"Artists")]
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

        [TestCaseSource(typeof(ReleaseTestCaseSource),"Albums")]
        public int GetReleaseTest(IAlbum album)
        {
            return MusicData.Provider.GetReleases(album).Count();
        }

        [TestCaseSource(typeof(SongTestCaseSource), "Releases")]
        public int GetSongTest(IRelease release)
        {
            return MusicData.Provider.GetSongs(release).Count();
        }
    }

    class AlbumTestCaseSources
    {

        private IArtist DefLeppard
        {
            get{ return MockFactory.CreateArtist("Def Leppard", "7249b899-8db8-43e7-9e6e-22f1e736024e");}
        }

        private IArtist RiseAgainst
        {
            get{ return MockFactory.CreateArtist("Rise Against", "606bf117-494f-4864-891f-09d63ff6aa4b");}
        }

        private IArtist Depresszio
        {
            get{ return MockFactory.CreateArtist("Depresszió", "79a8d8a6-012a-4dd9-b5e2-ed4b52a5d55e");}
        }

        private IArtist Deftones
        {
            get{ return MockFactory.CreateArtist("Deftones", "7527f6c2-d762-4b88-b5e2-9244f1e34c46");}
        }

        internal IEnumerable Artists
        {
            get
            {
                //yield return new TestCaseData(DefLeppard).Returns(91);
                yield return new TestCaseData(RiseAgainst).Returns(20);
                yield return new TestCaseData(Depresszio).Returns(3);
                //yield return new TestCaseData(Deftones).Returns(54);
            }
        }
    }

    class ReleaseTestCaseSource
    {
     
        private IAlbum Endgame
        {
            get { return MockFactory.CreateAlbum("Endgame", "875b2ff0-604b-4db3-a2f8-b427d725caf2");}
        }

        private IAlbum Hysteria
        {
            get { return MockFactory.CreateAlbum("Hysteria", "12fa3845-7c62-36e5-a8da-8be137155a72");}
        }

        internal IEnumerable Albums
        {
            get
            {
                yield return new TestCaseData(null).Returns(0);
                yield return new TestCaseData(Endgame).Returns(5);
                yield return new TestCaseData(Hysteria).Returns(9);
            }
        }
    }

    class SongTestCaseSource
    {
        private IRelease Endgame
        {
            get { return MockFactory.CreateRelease("Endgame", "b71a0f31-c12f-4548-a9f6-740f737abad1");}
        }

        private IRelease Hysteria
        {
            get { return MockFactory.CreateRelease("Hysteria", "58b8eab9-cd8b-4c86-9031-ddb126071de4");}
        }
       
        internal IEnumerable Releases
        {
            get
            {
                yield return new TestCaseData(null).Returns(0);
                yield return new TestCaseData(Endgame).Returns(12);
                yield return new TestCaseData(Hysteria).Returns(12);
            }
        }
    }

    static class MockFactory
    {
        internal static IArtist CreateArtist(string name, string id)
        {
            Mock<IArtist> mockArtist = new Mock<IArtist>();
            
            mockArtist.Setup(a => a.Id).Returns(new Guid(id));
            mockArtist.Setup(a => a.Name).Returns(name);
            return mockArtist.Object;
        }
        
        internal static IAlbum CreateAlbum(string name, string id)
        {
            Mock<IAlbum> mockArtist = new Mock<IAlbum>();
            
            mockArtist.Setup(a => a.Id).Returns(new Guid(id));
            mockArtist.Setup(a => a.Name).Returns(name);
            return mockArtist.Object;
        }
        
        
        //        internal static T CreateMock<T>(string name, string id) 
        //            where T : SongTagger.Core.IEntity
        //        {
        //            Mock<T> mockArtist = new Mock<T>();
        //            
        //            mockArtist.Setup(a => a.Id).Returns(new Guid(id));
        //            mockArtist.Setup(a => a.Name).Returns(name);
        //            return mockArtist.Object as T;
        //        }
        
        internal static IRelease CreateRelease(string name, string id)
        {
            Mock<IRelease> mockRelease = new Mock<IRelease>();
            mockRelease.Setup(a => a.Id).Returns(new Guid(id));
            mockRelease.Setup(a => a.Name).Returns(name);
            return mockRelease.Object;
        }
    }
}

