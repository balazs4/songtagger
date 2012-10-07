//
//  ArtistTests.cs
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
using System.Collections.Generic;
using System.Linq;

namespace SongTagger.Core.Test.Unit
{
    [TestFixture()]
    public class ArtistTests
    {
        [Test()]
        public void ToStringTest()
        {
            string name = "MurderDolls";
            Guid fakeId = Guid.NewGuid();
            string genreList = "horror punk,heavy metal";

            IArtist artist = new Artist() 
            {
                Name = name,
                Id = fakeId,
            };
            artist.Genres.AddRange(genreList.Split(',').ToList());



            String expected = string.Format("[Id={0}, Name={1}, Genres={2}]", fakeId, name, genreList);

            Assert.That(artist.ToString(), Is.EqualTo(expected));
        }
    }
}

