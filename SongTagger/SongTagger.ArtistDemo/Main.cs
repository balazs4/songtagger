//
//  Main.cs
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
using System.Collections.Generic;
using System.Linq;
using SongTagger.Core;
using System.Diagnostics;

namespace SongTagger.ArtistDemo
{
    class MainClass
    {


        public static void Main(string[] args)
        {
            string rootMusicFolder = "/home/balazs4/Music/Rock"; //ToDo: arg...
            IEnumerable<string> rawArtistCollection = GetRawArtistCollection(rootMusicFolder);


            IDictionary<string, IArtist> foundArtistList = new Dictionary<string, IArtist>();

            Stopwatch watcher = Stopwatch.StartNew();

            int counter = 0;
            int countOfRawArtist = rawArtistCollection.Count();
            foreach (string rawArtist in rawArtistCollection)
            {
                counter++;
                Console.Write("[{0}/{1}   {2}]", counter, countOfRawArtist, ((double)counter / (double)countOfRawArtist).ToString("P"));
                Console.Write("Searching...{0}", rawArtist);
                IArtist artist = MusicData.Provider.GetArtist(rawArtist);
                //IArtist artist = new UnknowArtist();
                Console.WriteLine("...Found: {0}", artist.ToString());
                foundArtistList.Add(rawArtist, artist);
            }

            watcher.Stop();

            Console.WriteLine("Elapsed: {0}", watcher.Elapsed);
            Console.WriteLine("Raw artist: {0}", countOfRawArtist);
            Console.WriteLine("Found artist: {0}", foundArtistList.Values.Count(a => !(a is UnknowArtist)));

        }

        private static IEnumerable<string> GetRawArtistCollection(string rootMusicFolder)
        {
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(rootMusicFolder);
            return rootDir.EnumerateDirectories("*", System.IO.SearchOption.TopDirectoryOnly).Select(d => d.Name);
        }

    }
}
