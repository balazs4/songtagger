//
//  ServiceClient.cs
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
using System.Xml.Linq;
using System.Net;
using System.Threading;

namespace SongTagger.Core.Service
{

    internal static class ServiceClient
    {
        private static object lockObject = new object();
        private static DateTime lastFinishedDownload = DateTime.MinValue;

        private static Func<Uri,String> download;
        internal static Func<Uri,String> Download
        { 
            get
            {
                return download ?? (download = DefaultContentDownloader);
            }
            set {download = value;}
        }
            
        private static string DefaultContentDownloader(Uri url)
        {
            string content;

            WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;

            using (WebClient client = new WebClient { Proxy = WebRequest.DefaultWebProxy })
            {
                content = client.DownloadString(url);
            }
            return String.IsNullOrWhiteSpace(content)
                    ? String.Empty
                    : content;
        }

        public static string DownloadContent(Uri url, Action<DateTime> prepareAction)
        {
            if (url == null)
                throw new ArgumentNullException("url", "Url is null.");

            lock (lockObject)
            {
                if (prepareAction != null)
                {
                    prepareAction(lastFinishedDownload);
                }

                string content = Download(url);
                lastFinishedDownload = DateTime.Now;
                return content;
            }
        }
    }
}
