//
//  MusicBrainzExtensionTests.cs
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
using System.Collections;

namespace SongTagger.Core.Test.Unit.Service
{
    [TestFixture()]
    public class MusicBrainzExtensionTests
    {
        [TestCase("Def Leppard", Result = "http://musicbrainz.org/ws/2/artist?query=Def+Leppard")]
        [TestCase("Rise Against", Result = "http://musicbrainz.org/ws/2/artist?query=Rise+Against")]
        [TestCase("Depresszio", Result = "http://musicbrainz.org/ws/2/artist?query=Depresszio")]
        public string Search_ArtistEntity(string searchText)
        {
            return Artist.Empty.Search(searchText).ToString();
        }

        internal static IEnumerable ArtistSource 
        {
            get 
            {
                yield return new TestCaseData(TestHelper.RiseAgainst)
                    .Returns("http://musicbrainz.org/ws/2/release-group?artist=606bf117-494f-4864-891f-09d63ff6aa4b&limit=444");
            }
        }

        [TestCaseSource("ArtistSource")]
        public string Browse_ReleaseGroups_OfArtist(Artist artist)
        {
            return artist.Browse<ReleaseGroup>(444).ToString();
        }

        internal static IEnumerable ReleaseSource
        {
            get 
            {
                yield return new TestCaseData(TestHelper.AppealToReasonRelease)
                    .Returns("http://musicbrainz.org/ws/2/release/205f2019-fc18-477a-971c-ecc37aa216fc?inc=recordings");
            }
        }

        [TestCaseSource("ReleaseSource")]
        public string Lookup_Tracks_OfRelease(Release release)
        {
            return release.Lookup<Recording>().ToString();
        }
    }
}

