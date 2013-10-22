//
//  TagHandlerTests.cs
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
//  Foundation, Inc., 5 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
using System;
using NUnit.Framework;
using System.IO;
using System.Linq;
using SongTagger.Core.Mp3Tag;
using System.Collections.Generic;

namespace SongTagger.Core.Test.Integration.Tag
{
    [TestFixture()]
    public class TagHandlerSaveTests
    {
        private string tempFileName;
        private Track track;
        private TagLib.Mpeg.AudioFile mp3File;
        private IDictionary<TagLib.TagTypes, TagLib.Tag> tags;

        private static string GetFileName()
        {
            string tmpDir = Path.Combine(Path.GetTempPath(), "songtagger");

            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, true);

            Directory.CreateDirectory(tmpDir);

            string tmp = Path.Combine(tmpDir, "tagHandlerTest.mp3");
            string fileName = TestHelper.GetInputDataFilePath("office.mp3");
            File.Copy(fileName, tmp, true);
            return tmp;
        }

        private static byte[] GetCoverArt()
        {
            string fileName = TestHelper.GetInputDataFilePath("DefLeppard-Erlangen.png");
            return System.IO.File.ReadAllBytes(fileName);
        }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            tempFileName = GetFileName();

            track = new Track();
            track.Release = TestHelper.AppealToReasonRelease;
            track.Record.Name = "Savior";
            track.Number = "11";
            track.Position = 11;


            TagHandler.Save(track, new FileInfo(tempFileName), GetCoverArt());

            mp3File = new TagLib.Mpeg.AudioFile(tempFileName);

            tags = new Dictionary<TagLib.TagTypes, TagLib.Tag>
            {
                {TagLib.TagTypes.Id3v1, mp3File.GetTag(TagLib.TagTypes.Id3v1)},
                {TagLib.TagTypes.Id3v2, mp3File.GetTag(TagLib.TagTypes.Id3v2)}
            };
        }

        [SetUp]
        public void Setup()
        {
            Assert.IsNotNull(mp3File);
            Assert.IsNotNull(track);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            FileInfo tmpFile = new FileInfo(tempFileName);
            if (Directory.Exists(tmpFile.DirectoryName))
            {
                Directory.Delete(tmpFile.DirectoryName, true);
            }
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_Title(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags[tag];
            Assert.AreEqual(track.Name, id3Tag.Title, tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_TrackNumber(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags[tag];
            Assert.AreEqual(track.Number, id3Tag.Track.ToString(), tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_AlbumName(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags[tag];
            Assert.AreEqual(track.Release.ReleaseGroup.Name, id3Tag.Album, tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_AlbumReleaseYear(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags[tag];
            Assert.AreEqual(track.Release.ReleaseGroup.FirstReleaseDate.Year.ToString(), id3Tag.Year.ToString(), tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_ArtistName(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags[tag];
            Assert.AreEqual(track.Release.ReleaseGroup.Artist.Name, id3Tag.FirstPerformer, tag.ToString());
        }
        //[TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_ArtistGenres(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags[tag];
            CollectionAssert.AreEquivalent(track.Release.ReleaseGroup.Artist.Tags.Select(t => t.Name), id3Tag.Genres, tag.ToString());
        }
        //[TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_CoverArt(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags[tag];
            CollectionAssert.IsNotEmpty(id3Tag.Pictures, tag.ToString());
        }
    }

    [TestFixture()]
    public class TagHandlerGetInfoTests
    {
        [Test]
        public void GetInfo()
        {
            FileInfo mp3 = new FileInfo(TestHelper.GetInputDataFilePath("office.mp3"));

            var actualTags = TagHandler.GetSongTags(mp3);
            var expectedTags = new [] { "4", "the office theme song" };

            CollectionAssert.AreEquivalent(expectedTags, actualTags);
        }
    }
}

