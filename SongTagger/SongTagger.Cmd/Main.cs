//
//  Main.cs
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
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

using SongTagger.Core;

namespace SongTagger.Cmd
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            IEnumerable<string> rawArtistCollection = 
                //GetRawArtistCollection(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Rock"));
                new List<string> 
            {
                "RiseAgainst", 
            };

            IEnumerable<IArtist> artistList = ExecuteTask<String, IArtist>(
                rawArtistCollection, 
                (rawName, temp) =>
                {
                    temp.Add(MusicData.Provider.GetArtist(rawName));
                }
            );

            IEnumerable<IAlbum> albumList = ExecuteTask<IArtist, IAlbum>(
                artistList, 
                (artist, temp) =>
                {
                    temp.AddRange(MusicData.Provider.GetReleases(artist));
                }
            );



            foreach (var group in albumList.ToLookup(a => a.ArtistOfRelease))
            {
                Console.WriteLine (group.Key.Name);
                foreach (IAlbum release in group.OrderBy(r => r.ReleaseDate)) 
                {
                    Console.WriteLine ("\t ({0}) [{1}] {2} (#{3})", release.ReleaseDate.Year, release.TypeOfRelease, release.Name, release.Covers.Count);
                }
            }

        }
        
        private static IEnumerable<string> GetRawArtistCollection(string rootMusicFolder)
        {
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(rootMusicFolder);
            return rootDir.EnumerateDirectories("*", System.IO.SearchOption.TopDirectoryOnly).Select(d => d.Name);
        }

        private delegate void ActionTask<S,T>(S item, List<T> target);

        private static IEnumerable<T> ExecuteTask<S,T>(IEnumerable<S> sourceList, ActionTask<S,T> query) where T : IEntity
        {
            List<T> targetList = new List<T>();

            Stopwatch watcher = Stopwatch.StartNew();
            try
            {
                foreach (S source in sourceList) 
                {
                    Console.WriteLine ("GET {0}", source);
                    query(source,targetList);
                }
                Console.WriteLine("Found {0}: {1} ### DURATION {2}", typeof(T).Name ,targetList.Count(a => a.Id != Guid.Empty), watcher.Elapsed.ToString());
                watcher.Stop();
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return targetList;
        }
    }
}
