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

namespace SongTagger.Core
{
    internal class LastFm : IWebService
    {
        //api_key='b25b959554ed76058ac220b7b2e0a026'
        //http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key=$api_key&artist=$artist&album=$album

        private Uri baseUri;
        private readonly string LastFmApiKey = "b25b959554ed76058ac220b7b2e0a026";

        internal Uri LastFmBaseUrl
        {
            get { return baseUri;}
        }

        internal LastFm()
        {
            UriBuilder uriBuilder = new UriBuilder("http://ws.audioscrobbler.com/2.0/") 
            {
                Query = String.Format("api_key={0}", LastFmApiKey)
            };


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
    }
}

