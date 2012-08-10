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

namespace SongTagger.Core
{
    public enum ServiceName
    {
        MusicBrainz = 1,
        LastFm = 2
    }

    public static class WebServices
    {
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

        static WebServices()
        {
            serviceCollection = new Dictionary<ServiceName,IWebService>();
        }

        public static IWebService Instance(ServiceName serviceKey)
        {
            if (!serviceCollection.ContainsKey(serviceKey))
            {
                IWebService newInstance = CreateNewInstance(serviceKey);
                serviceCollection.Add(serviceKey, newInstance);
            }
            return serviceCollection [serviceKey];
        }


        #region Private helper methods
        private static String DownloadResult(Uri queryUrl)
        {
            String data = String.Empty;

            using (WebClient client = new WebClient())
            {   
                data = client.DownloadString(queryUrl);
            }
            return data;
        }
        #endregion
    }
}

