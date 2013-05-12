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
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
using System;
using NUnit.Framework;
using System.IO;
using SongTagger.Core.Tag;
using Moq;
using System.Diagnostics;
using System.Collections.Generic;

namespace SongTagger.Core.Test.Integration.Tag
{
    [TestFixture]
    public class TagHandlerSaveTests
    {
        private string tempFileName;
        private ISong songInfo;
        private TagLib.Mpeg.AudioFile mp3File;
        private IDictionary<TagLib.TagTypes, TagLib.Tag> tags;

        private static string GetFileName()
        {
            string tmpDir = Path.Combine(Path.GetTempPath(), "songtagger");

            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir);

            Directory.CreateDirectory(tmpDir);

            string tmp = Path.Combine(tmpDir, "tagHandlerTest.mp3");
            string fileName = TestHelper.GetInputDataFilePath("01_kelet_vagy_nyugat.mp3");
            File.Copy(fileName, tmp, true);
            return tmp;
        }

        private static ISong SetupMock()
        {
            Mock<IArtist> artistMock = new Mock<IArtist>();
            artistMock.Setup(a => a.Name).Returns("Blub artist");
            artistMock.Setup(a => a.Genres).Returns(new List<String> { "rock", "alternative", "punk"});
           
            Mock<ICoverArt> coverArt = new Mock<ICoverArt>();
            coverArt.Setup(c => c.SizeCategory).Returns(SizeType.Large);
            coverArt.Setup(c => c.Url).Returns(new Uri("http://a1-images.myspacecdn.com/images03/33/668528fd88864db9ba4f1b41efae24ca/lrg.jpg"));
            



            Mock<IAlbum> albumMock = new Mock<IAlbum>();
            albumMock.Setup(a => a.Name).Returns("Foobar");
            albumMock.Setup(a => a.ReleaseDate).Returns(new DateTime(1986, 03, 16));
            albumMock.Setup(a => a.ArtistOfRelease).Returns(artistMock.Object);
            albumMock.Setup(a => a.Covers).Returns(new List<ICoverArt> {coverArt.Object});
            
            Mock<IRelease> releaseMock = new Mock<IRelease>();
            releaseMock.Setup(r => r.Name).Returns("Foobar");
            releaseMock.Setup(r => r.Album).Returns(albumMock.Object);
            
            Mock<ISong> songMock = new Mock<ISong>();
            songMock.Setup(s => s.Name).Returns("I wanna be...");
            songMock.Setup(s => s.Number).Returns(16);
            songMock.Setup(s => s.Release).Returns(releaseMock.Object);
            
            return songMock.Object;
        }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            tempFileName = GetFileName();
            songInfo = SetupMock();

            TagHandler.Save(songInfo, new FileInfo(tempFileName));

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
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            FileInfo tmpFile = new FileInfo(tempFileName);
            if (Directory.Exists(tmpFile.DirectoryName))
            {
                Directory.Delete(tmpFile.DirectoryName,true);
            }
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_Title(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags [tag];
            Assert.AreEqual(songInfo.Name, id3Tag.Title, tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_TrackNumber(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags [tag];
            Assert.AreEqual(songInfo.Number, id3Tag.Track, tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_AlbumName(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags [tag];
            Assert.AreEqual(songInfo.Release.Album.Name, id3Tag.Album, tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_AlbumReleaseYear(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags [tag];
            Assert.AreEqual(songInfo.Release.Album.ReleaseDate.Year.ToString(), id3Tag.Year.ToString(), tag.ToString());
        }

        [TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_ArtistName(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags [tag];
            Assert.AreEqual(songInfo.Release.Album.ArtistOfRelease.Name, id3Tag.FirstPerformer, tag.ToString());
        }

        //[TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_ArtistGenres(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags [tag];
            CollectionAssert.AreEquivalent(songInfo.Release.Album.ArtistOfRelease.Genres, id3Tag.Genres, tag.ToString());
        }

        //[TestCase(TagLib.TagTypes.Id3v1)]
        [TestCase(TagLib.TagTypes.Id3v2)]
        public void Assert_CoverArt(TagLib.TagTypes tag)
        {
            TagLib.Tag id3Tag = tags [tag];
            CollectionAssert.IsNotEmpty(id3Tag.Pictures, tag.ToString());
        }

    }
}

