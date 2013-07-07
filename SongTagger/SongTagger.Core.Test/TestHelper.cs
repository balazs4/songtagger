//
//  TestHelper.cs
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
using System.IO;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;

namespace SongTagger.Core.Test
{
    internal class TestHelper
    {

        private static DirectoryInfo inputDataRoot = new DirectoryInfo("inputData");

        internal static string GetInputDataFilePath(string fileName)
        {
            return Path.Combine(inputDataRoot.FullName, fileName);
        }

        internal static Artist RiseAgainst
        {
            get
            {
                Artist artist = new Artist
                {
                    Id = new Guid("606bf117-494f-4864-891f-09d63ff6aa4b"),
                    Name = "Rise Against",
                    Tags = new List<Tag>() { new Tag{Name = "rock"}, new Tag {Name = "punk"}  }
                };

                return artist;
            }
        }

        internal static ReleaseGroup AppealToReason
        {
            get
            {
                return new ReleaseGroup
                { 
                    Id = new Guid("0b0e4477-4b04-3683-8f01-3a4544c36b41"), 
                    Name = "Appeal to Reason",
                    PrimaryType = ReleaseGroupType.Album,
                    FirstReleaseDate = new DateTime(2008,10,2),
                    Artist = RiseAgainst
                };
            }
        }

        internal static Release AppealToReasonRelease
        {
            get
            {
                return new Release
                {
                    ReleaseGroup = AppealToReason,
                    Id = new Guid("205f2019-fc18-477a-971c-ecc37aa216fc"),
                    Name = AppealToReason.Name,
                    Country = "DE",
                    Status = "Official"
                };
            }
        }
    }
}

