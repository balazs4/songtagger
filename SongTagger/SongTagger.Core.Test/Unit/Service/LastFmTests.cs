//
//  LastFmTests.cs
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
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using Moq;
using SongTagger.Core.Service;

namespace SongTagger.Core.Test.Unit.Service
{
    [TestFixture()]
    public class LastFmTests
    {
        [Test]
        public void BaseUrlTest()
        {
            Assert.That(LastFm.LastFmBaseUrl.ToString(), Contains.Substring("audioscrobbler"));
        }

        [Test]
        public void ExcuteQueryArgumentCheckTest()
        {
            LastFm instance = new LastFm();
            Assert.That(() => {
                instance.ExecuteQuery(null);}, 
            Throws.ArgumentException);
        }

        [Test]
        public void CreateAlbumCoverQueryUri_ValidId()
        {
            Regex regex = new Regex(@"http://ws.audioscrobbler.com/2.0/\?api_key=[0-9a-f]{32}&artist=[\w\s\d]*&album=[\w\s\d]*&method=album.getinfo");

            Mock<IArtist> artist = new Mock<IArtist>();
            artist.Setup(a => a.Name).Returns("Def Leppard");

            Mock<IAlbum> album = new Mock<IAlbum>();
            album.Setup(a => a.Name).Returns("Hysteria");
            album.Setup(a => a.ArtistOfRelease).Returns(artist.Object);

            Uri actualUri = LastFm.CreateAlbumCoverQueryUri(album.Object);
            Console.WriteLine(actualUri.ToString());
            Assert.That(
                regex.IsMatch(actualUri.ToString()), 
                Is.True);
        }

        [Test]
        public void ParseXmlToCoverList_ValidXDocument()
        {
            XDocument doc = XDocument.Load(TestHelper.GetInputDataFilePath("LastFmTest.ParseXmlToCoverList.ValidCovers.xml"));

            IEnumerable<ICoverArt> coverArt = LastFm.ParseXmlToCoverList(doc);

            CollectionAssert.IsNotEmpty(coverArt);
            CollectionAssert.AllItemsAreUnique(coverArt);

            Assert.That(coverArt.Count(), Is.EqualTo(5));

            Assert.That(coverArt.Any(c => c.SizeCategory == SizeType.Unknow), Is.False);

            Assert.That(coverArt.Count(c => c.SizeCategory == SizeType.Large), Is.EqualTo(1));
            Assert.That(coverArt.Count(c => c.SizeCategory == SizeType.ExtraLarge), Is.EqualTo(1));
            Assert.That(coverArt.Count(c => c.SizeCategory == SizeType.Mega), Is.EqualTo(1));
            Assert.That(coverArt.Count(c => c.SizeCategory == SizeType.Medium), Is.EqualTo(1));
            Assert.That(coverArt.Count(c => c.SizeCategory == SizeType.Small), Is.EqualTo(1));
        }

    }
}

