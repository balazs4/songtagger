//
//  MusicBrainz.cs
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
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace SongTagger.Core
{
    internal class MusicBrainz : IWebService
    {
        private static readonly Object lockObject = new Object();
        internal static readonly TimeSpan WAIT_TIME_BETWEEN_QUERIES = new TimeSpan(0, 0, 2); //http://musicbrainz.org/doc/XML_Web_Service/Rate_Limiting
        internal static readonly Uri baseUrl = new Uri("http://musicbrainz.org/ws/2/");


        #region C'tors
        internal MusicBrainz()
        {

        }
        #endregion

        internal static String DownloadContentSafely(Uri queryUrl, WebServices.DownloadContentDelegate downloadAction)
        {
            String content = String.Empty;
            lock (lockObject)
            {
                Stopwatch timer = Stopwatch.StartNew();

                content = downloadAction(queryUrl);

                timer.Stop();

                if (timer.Elapsed < WAIT_TIME_BETWEEN_QUERIES)
                    Thread.Sleep(WAIT_TIME_BETWEEN_QUERIES - timer.Elapsed);
            }
            return content;
        }

        #region IWebService implementation
        public XDocument ExecuteQuery(Uri queryUrl)
        {
            if (queryUrl == null)
            {
                throw new ArgumentException("queryUrl could not be null", "queryUrl");
            }

            String content = DownloadContentSafely(queryUrl, WebServices.DownloadContent);

            if (String.IsNullOrWhiteSpace(content))
                return null;

            XDocument result;
            if (!WebServices.TryParse(content, out result))
            {
                return null;
            }
            return result;
        }
        #endregion

        #region Parse methods
        internal static IArtist ParseXmlToArtist(XDocument result, int minimumScore)
        {
            if (result == null)
                return new UnknowArtist();


            IArtist artist = null;
            try
            {
                XNamespace mb = XNamespace.Get("http://musicbrainz.org/ns/mmd-2.0#");
                XNamespace ext = XNamespace.Get("http://musicbrainz.org/ns/ext#-2.0");

                #region XName definitions
                XName xnameArtist = XName.Get("artist", mb.NamespaceName);
                XName xnameScore = XName.Get("score", ext.NamespaceName);
                XName xnameId = XName.Get("id");
                XName xnameName = XName.Get("name", mb.NamespaceName);
                XName xnameTag = XName.Get("tag", mb.NamespaceName);
                XName xnameTagName = XName.Get("name", mb.NamespaceName);
                #endregion

                #region Xml Query
                XElement artistElement = result.Descendants(xnameArtist)
                    .First(e => e.HasAttributes && Convert.ToInt32(e.Attribute(xnameScore).Value) >= minimumScore);

                IEnumerable<string> rawGenreList = artistElement
                        .Descendants(xnameTag)
                        .Elements(xnameTagName)
                        .Where(e => !String.IsNullOrWhiteSpace(e.Value))
                        .Select(e => e.Value);
                #endregion


                artist = new Artist()
                {
                    Id = new Guid(artistElement.Attribute(xnameId).Value),
                    Name = artistElement.Element(xnameName).Value,
                };

                artist.Genres.AddRange(rawGenreList);

            } catch (Exception)
            {
                return new UnknowArtist();
            }

            return artist ?? new UnknowArtist();
        }

        internal static IEnumerable<IAlbum> ParseXmlToAlbum(XDocument result)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Uri methods

        internal static string SplitArtistName(string rawName)
        {
            if (rawName.Contains(" "))
            {
                return rawName;
            }

            string onlyUpperCasePattern = "^[A-Z]+$";
            if (Regex.Match(rawName, onlyUpperCasePattern).Success)
            {
                return Regex.Replace(rawName, "([A-Z])", "$1.", RegexOptions.Compiled).Trim();
            }

            return Regex.Replace(rawName, "([0-9]+|[A-Z])", " $1", RegexOptions.Compiled).Trim();

        }

        internal static Uri CreateArtistQueryUri(string nameOfArtist)
        {

            UriBuilder queryUri = new UriBuilder(baseUrl.ToString());
            queryUri.Path += "artist";

            if (String.IsNullOrWhiteSpace(nameOfArtist))
            {
                queryUri.Query = "query=";
                return queryUri.Uri;
            }

            string encoded = SplitArtistName(nameOfArtist);

            string luceneQuery = String.Format("{0}%20AND%20type:group", encoded.Replace(" ", "%20"));

            queryUri.Query = String.Format("query={0}", luceneQuery);


            return queryUri.Uri;
        }

        internal static Uri CreateAlbumQueryUri(Guid id)
        {
            //http://musicbrainz.org/ws/2/artist/606bf117-494f-4864-891f-09d63ff6aa4b?inc=release-groups

            UriBuilder queryUri = new UriBuilder(baseUrl.ToString());
            queryUri.Path += String.Format("artist/{0}",id.ToString());
      
            queryUri.Query = "inc=release-groups";
            return queryUri.Uri;
        }
        #endregion
    }

}

