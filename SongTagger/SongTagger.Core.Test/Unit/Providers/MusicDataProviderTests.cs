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

using SongTagger.Core;

namespace SongTagger.Core.Test.Unit.Providers
{
    [TestFixture()]
    public class MusicDataProviderTests
    {
        [Test()]
        public void SingletonTest()
        {
            IProvider instance = MusicData.Provider;

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.SameAs(instance));
        }


        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void GetArtist_ArgumentCheck_ArgumentException_Expected(string aritstName)
        {
            Assert.Throws<ArgumentException>(
                () => MusicData.Provider.GetArtist(aritstName)
            );
        }
    }
}

