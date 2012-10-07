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

namespace SongTagger.Core.Test.Integration
{
    [TestFixture()]
    public class MusicDataProviderTests
    {
        [TestCase("RiseAgainst")]
        [TestCase("Korn")]
        [TestCase("6test")]
        public void GetArtistTest(string artistName)
        {
            IArtist artist = null;
            Assert.That(() => {
                artist = MusicData.Provider.GetArtist(artistName);
            }, Throws.Nothing);
            Assert.That(artist, Is.Not.Null);
            Assert.That(artist, Is.Not.InstanceOf<UnknowArtist>());
        }
    }
}

