//
//  LastFm.cs
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
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SongTagger.Core.Service
{
    internal class LastFm : IWebService
    {
        //http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key=$api_key&artist=$artist&album=$album
        //http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key=xxx&mbid=3c186b52-ca73-46a3-a8e6-04559bfbb581
        private static Uri baseUri;
        private static string LastFmApiKey; //Request your own key ;-)

        internal static Uri LastFmBaseUrl
        {
            get { return baseUri;}
        }

        static LastFm()
        {
            UriBuilder uriBuilder = new UriBuilder("http://ws.audioscrobbler.com/2.0/");
            if (File.Exists("lastfm.api"))
                LastFmApiKey = File.ReadAllLines("lastfm.api").FirstOrDefault() ?? Guid.Empty.ToString().Replace("-",String.Empty); 
            else
                LastFmApiKey = Guid.Empty.ToString().Replace("-",String.Empty);

            baseUri = uriBuilder.Uri;
        }

        #region IWebService implementation

        public System.Xml.Linq.XDocument ExecuteQuery(Uri queryUri)
        {

            if (queryUri == null)
            {
                throw new ArgumentException("queryUrl could not be null", "queryUri");
            }

            String content = WebServices.DownloadContent(queryUri);

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

        internal static Uri CreateAlbumCoverQueryUri(IAlbum album)
        {
            return new UriBuilder(baseUri) 
            {
                Query = String.Format("api_key={0}&artist={1}&album={2}&method=album.getinfo",
                                      LastFmApiKey,
                                      album.ArtistOfRelease.Name.Replace(" ", "%20"), 
                                      album.Name.Replace(" ", "%20")
                                      )
            }.Uri;


        }

        internal static IEnumerable<ICoverArt> ParseXmlToCoverList(XDocument lasfFmResult)
        {
            try
            {
                IList<ICoverArt> coverList = new List<ICoverArt>();

                XName image = XName.Get("image");
                XName size = XName.Get("size");

                foreach (XElement element in lasfFmResult.Descendants(image).Where(e => !String.IsNullOrWhiteSpace(e.Value.ToString())))
                {
                    SizeType imgSize;
                    if (!Enum.TryParse<SizeType>(element.Attribute(size).Value.ToUpperInvariant(), true, out imgSize))
                    {
                        imgSize = SizeType.Unknown;
                    }

                    Uri imgUrl = new Uri(element.Value.ToString());

                    ICoverArt pic = new CoverArt() 
                    {
                        SizeCategory = imgSize,
                        Url = imgUrl
                    };

                    coverList.Add(pic);
                } 



                return coverList;
            } catch (Exception)
            {
                return new List<ICoverArt>();
            }
        }
    }
}

