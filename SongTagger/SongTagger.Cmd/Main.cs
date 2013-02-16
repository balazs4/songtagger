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
using System.Net;

namespace SongTagger.Cmd
{
    class SizeComparer : IComparer<SizeType>
    {
        #region IComparer implementation        
        public int Compare(SizeType x, SizeType y)
        {
            if (x == SizeType.ExtraLarge)
                return 1;

            if (y == SizeType.ExtraLarge)
                return -1;

            return x.CompareTo(y);
        }        
        #endregion
    }

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

            String targetDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Library");

            CreateMusicLibrary(targetDirPath, albumList);
        }
        
        private static IEnumerable<string> GetRawArtistCollection(string rootMusicFolder)
        {
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(rootMusicFolder);
            return rootDir.EnumerateDirectories("*", System.IO.SearchOption.TopDirectoryOnly).Select(d => d.Name);
        }

        private delegate void ActionTask<S,T>(S item,List<T> target);

        private static IEnumerable<T> ExecuteTask<S,T>(IEnumerable<S> sourceList, ActionTask<S,T> query) where T : IEntity
        {
            List<T> targetList = new List<T>();

            Stopwatch watcher = Stopwatch.StartNew();
            try
            {
                foreach (S source in sourceList)
                {
                    Console.WriteLine("GET {0}", source);
                    query(source, targetList);
                }
                Console.WriteLine("Found {0}: {1} ### DURATION {2}", typeof(T).Name, targetList.Count(a => a.Id != Guid.Empty), watcher.Elapsed.ToString());
                watcher.Stop();
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return targetList;
        }

        private static String GetOrCreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        private static string GetCleanName(string name)
        {
            return name.Replace(" ", "")
                        .Replace("ü", "u")
                        .Replace("ö", "o");
            //TODO: ....
        }

        private static string GetAlbumDirectoryName(IAlbum release)
        {
            String name = String.Format("[{0}] {1} ({2})", release.ReleaseDate.Year, release.Name, release.TypeOfRelease);

            if (release.ReleaseDate == DateTime.MinValue)
            {
                name = name.Replace(
                            String.Format("[{0}]", release.ReleaseDate.Year),
                            "[yyyy]"
                        );
            }

            if (release.TypeOfRelease == ReleaseType.Album)
            {
                name = name.Replace(
                        String.Format("({0})", release.TypeOfRelease),
                        ""
                        );
            }

            return name.Trim();
        }

        private static void DownloadCoverArt(string albumDirPath, IAlbum release)
        {
            string name = "cover.jpg";

            Uri winningUrl = GetCoverUrl(release);

            if (winningUrl == null)
                return;

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(winningUrl, Path.Combine(albumDirPath, name));
            }
        }

        private static Uri GetCoverUrl(IAlbum release)
        {
            if (release.Covers == null || release.Covers.Count == 0)
                return null;


            return release.Covers
                .OrderByDescending(c => c.SizeCategory, new SizeComparer())
                .Select(c => c.Url)
                .FirstOrDefault();
        }

        private static void SaveMbidFile(string albumDirPath, IAlbum release)
        {
            if (release.Id == Guid.Empty)
                return;

            File.WriteAllText(
                Path.Combine(albumDirPath, String.Format("{0}.mbid", release.Id)),
                String.Format("{0}#{1}",release.Id.ToString(), release.ArtistOfRelease.Id.ToString())
            );
        }

        private static void CreateMusicLibrary(string targetDirPath, IEnumerable<IAlbum> albumList)
        {
       
            GetOrCreateDirectory(targetDirPath);

            foreach (var group in albumList.ToLookup(a => a.ArtistOfRelease))
            {
                Console.WriteLine(group.Key.Name);
                string artistDirPath = GetOrCreateDirectory(Path.Combine(targetDirPath, GetCleanName(group.Key.Name)));


                foreach (IAlbum release in group.Where(a => a.Id != Guid.Empty).OrderBy(r => r.ReleaseDate))
                {
                    Console.WriteLine("\t ({0}) [{1}] {2} (#{3})", release.ReleaseDate.Year, release.TypeOfRelease, release.Name, release.Covers.Count);
                    string albumDirPath = GetOrCreateDirectory(Path.Combine(artistDirPath, GetAlbumDirectoryName(release)));
                    SaveMbidFile(albumDirPath, release);
                    DownloadCoverArt(albumDirPath, release);
                }
            }
        }
    }
}
