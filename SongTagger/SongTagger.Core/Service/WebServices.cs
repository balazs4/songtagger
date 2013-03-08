//
//  WebServices.cs
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
using System.Xml.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace SongTagger.Core.Service
{
    public enum ServiceName
    {
        Unknown = 0,
        MusicBrainz = 1,
        LastFm = 2
    }

    public static class WebServices
    {
        #region Private init methods
        private static Dictionary<ServiceName,IWebService> serviceCollection;

        private static IWebService CreateNewInstance(ServiceName serviceKey)
        {
            IWebService service;
            switch (serviceKey)
            {
                case ServiceName.MusicBrainz:
                    service = new MusicBrainz();
                    break;
                
                case ServiceName.LastFm:
                    service = new LastFm();
                    break;

                default:
                    service = null;
                    break;
            }
            return service;
        }

        private static readonly String userAgentInfo;

        private static String GetUserAgentInfo()
        {
            String appName = Assembly.GetExecutingAssembly().GetName().Name;
            Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            String runtimeInformation = String.Format(".Net {0}; {1}", Environment.Version, Environment.OSVersion.VersionString);

            return String.Format("{0}/{1} ({2})",
                                          appName,
                                          appVersion,
                                          runtimeInformation
            );

        }

        static WebServices()
        {
            serviceCollection = new Dictionary<ServiceName,IWebService>();
            userAgentInfo = GetUserAgentInfo();                       
        }
        #endregion

        public static IWebService Instance(ServiceName serviceKey)
        {
            if (!serviceCollection.ContainsKey(serviceKey))
            {
                IWebService newInstance = CreateNewInstance(serviceKey);
                serviceCollection.Add(serviceKey, newInstance);
            }
            return serviceCollection[serviceKey];
        }

        internal delegate String DownloadContentDelegate(Uri queryUrl);

        #region Helper methods
        /// <summary>
        /// Downloads the content.
        /// </summary>
        /// <returns>
        /// The content.
        /// </returns>
        /// <param name='queryUrl'>
        /// Query URL.
        /// </param>
        /// <exception cref="ArgumentNullException">If <param name="queryUrl"> is null</exception>
        internal static String DownloadContent(Uri queryUrl)
        {
            if (queryUrl == null)
                throw new ArgumentNullException("queryUrl", "Url could not be null");
            
            String data = String.Empty;
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("user-agent", userAgentInfo); //http://musicbrainz.org/doc/XML_Web_Service/Rate_Limiting
                    data = client.DownloadString(queryUrl);
                }

            } catch (WebException)
            {
            }
            return data;
        }
    

        internal static bool TryParse(string content, out XDocument parsedDocument)
        {
            try
            {
                parsedDocument = XDocument.Parse(content);
            } catch
            {
                parsedDocument = null;
                return false;
            }

            return true;
        }
    

        #endregion
    }
}

