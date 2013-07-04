//
//  CoverArtExtensionTests.cs
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

    [TestFixture]
    public class CoverArtExtensionTests
    {
        internal static IEnumerable ReleaseSource
        {
            get 
            {
                yield return new TestCaseData(TestHelper.AppealToReasonRelease)
                    .Returns("http://coverartarchive.org/release/205f2019-fc18-477a-971c-ecc37aa216fc/front-500");
            }
        }

        [TestCaseSource("ReleaseSource")]
        public string Get_CoverArt(Release release)
        {
            return release.GetCoverArt().ToString();
        }
    }
}
