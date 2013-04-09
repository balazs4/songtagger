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
    [TestFixture()]
    public class TagHandlerTests
    {
        private string tempFileName;

        [SetUp]
        public void Setup() 
        {
            tempFileName = Path.Combine(Path.GetTempPath(), "tagHandlerTest.mp3");
            string fileName = TestHelper.GetInputDataFilePath("01_kelet_vagy_nyugat.mp3");
            File.Copy(fileName,tempFileName,true);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }
        }

        [Test]
        public void Save_FileExist_SongInfoValid()
        {
            Mock<IArtist> artistMock = new Mock<IArtist>();
            artistMock.Setup(a => a.Name).Returns("Blub artist");
            artistMock.Setup(a => a.Genres).Returns(new List<String> { "rock", "alternative", "punk"});

            Mock<IAlbum> albumMock = new Mock<IAlbum>();
            albumMock.Setup(a => a.Name).Returns("Foobar");
            albumMock.Setup(a => a.ReleaseDate).Returns(new DateTime(1986,03,16));
            albumMock.Setup(a => a.ArtistOfRelease).Returns(artistMock.Object);


            Mock<IRelease> releaseMock = new Mock<IRelease>();
            releaseMock.Setup(r => r.Name).Returns("Foobar");
            releaseMock.Setup(r => r.Album).Returns(albumMock.Object);

            Mock<ISong> songMock = new Mock<ISong>();
            songMock.Setup(s => s.Name).Returns("I wanna be...");
            songMock.Setup(s => s.Number).Returns(4);
            songMock.Setup(s =>s.Release).Returns(releaseMock.Object);



            ISong songInfo = songMock.Object;
            FileInfo mp3 = new FileInfo(tempFileName);
            TagHandler.Save(songMock.Object, mp3);


            TagLib.Mpeg.AudioFile file = new TagLib.Mpeg.AudioFile(mp3.FullName);
            List<TagLib.TagTypes> tags = new List<TagLib.TagTypes> 
            {
                TagLib.TagTypes.Id3v1,
                TagLib.TagTypes.Id3v2
            };

            foreach (TagLib.TagTypes tag in tags)
            {
                TagLib.Tag id3Tag = file.GetTag(tag);
                Assert.AreEqual(songInfo.Name, id3Tag.Title, "Wrong title in " + tag);
                Assert.AreEqual(songInfo.Number, id3Tag.Track, "Wrong track number in " + tag);
                Assert.AreEqual(songInfo.Release.Album.Name, id3Tag.Album, "Wrong album name in " + tag);
                Assert.AreEqual(songInfo.Release.Album.ReleaseDate.Year.ToString(), id3Tag.Year.ToString(), "Wrong album year in " + tag);
                Assert.AreEqual(songInfo.Release.Album.ArtistOfRelease.Name, id3Tag.FirstAlbumArtist, "Wrong artist in " + tag);
                CollectionAssert.AreEquivalent(songInfo.Release.Album.ArtistOfRelease.Genres, id3Tag.Genres);
                Assert.IsTrue(file.Tag.Pictures.Length > 0);

                //TODO: Country...Ids
            }

        }
    }
}

