//
//  TagHandler.cs
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
using System.IO;
using System.Linq;
using TagLib;
using System.Collections.Generic;
using System.Net;

namespace SongTagger.Core.Mp3Tag
{
    internal static class TagHandler
    {
        public static void Save(Track songInfo, FileInfo mp3File)
        {
            if (mp3File == null)
                throw new ArgumentNullException("mp3File", "Mp3 file cannot be null");

            if (!mp3File.Exists)
                throw new FileNotFoundException("File does not exist", mp3File.FullName);

            if (songInfo == null)
                throw new NotSupportedException("Songinfo could not be null");

            List<TagTypes> tags = new List<TagTypes>
            {
                TagTypes.Id3v1,
                TagTypes.Id3v2
            };

            TagLib.Mpeg.AudioFile mp3 = new TagLib.Mpeg.AudioFile(mp3File.FullName);
            mp3.RemoveTags(TagTypes.AllTags);

            foreach (TagTypes tagType in tags)
            {
                TagLib.Tag tag = mp3.GetTag(tagType, true);

                tag.Title = songInfo.Name;

                tag.Track = (uint)songInfo.Number;

                tag.Album = songInfo.Release.ReleaseGroup.Name;

                tag.AlbumArtists = new string[] { songInfo.Release.ReleaseGroup.Artist.Name };
                tag.Performers = new string[] { songInfo.Release.ReleaseGroup.Artist.Name };

                tag.Year = Convert.ToUInt32(songInfo.Release.ReleaseGroup.FirstReleaseDate.Year);

                tag.Genres = songInfo.Release.ReleaseGroup.Artist.Tags.Select(t => t.Name).ToArray();

                //tag.Pictures = DownloadCoverArt(songInfo, mp3File.Directory);
            }

            mp3.Save();
        }

        private static IPicture[] DownloadCoverArt(Track songInfo, DirectoryInfo targetDir)
        {
            throw new NotImplementedException();
        }
    }
}

