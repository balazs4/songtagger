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
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace SongTagger.Core.Service
{
    internal static class ServiceClient
    {
        private static object lockObject = new object();
        private static DateTime lastFinishedDownload = DateTime.MinValue;

        private static Func<Uri, String> download;
        internal static Func<Uri, String> Download
        {
            get
            {
                return download ?? (download = DefaultContentDownloader);
            }
            set { download = value; }
        }


        internal static WebClient CreateWebClient()
        {
            WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            return new WebClient { Proxy = WebRequest.DefaultWebProxy, Encoding = new UTF8Encoding() };
        }

        private static string DefaultContentDownloader(Uri url)
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    return Down(url);
                }
                catch (Exception)
                {
                    Thread.Sleep(3000);
                    continue;
                }
            }

            return "<?xml version=\"1.0\" encoding=\"UTF - 8\"?><metadata xmlns=\"http://musicbrainz.org/ns/mmd-2.0#\"><metadata></metadata>";
        }


        private static string Down(Uri url)
        {
            string content = string.Empty;
            Thread.Sleep(5000);

            using (WebClient client = CreateWebClient())
            {
                client.Headers.Add("user-agent", "songtagger http://songtagger.codeplex.com");
                content = client.DownloadString(url);
            }
            return content;
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
