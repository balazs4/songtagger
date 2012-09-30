//
//  WebServicesTest.cs
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
using NUnit.Framework;

using SongTagger.Core;

namespace SongTagger.Core.Test.Unit.Services
{
    [TestFixture()]
    public class WebServicesTests
    {
        [TestCase(ServiceName.MusicBrainz)]
        [TestCase(ServiceName.LastFm)]
        public void GetInstance_IsNotNull(ServiceName service)
        {
            IWebService actual = WebServices.Instance(service);
            Assert.IsNotNull(actual);
        }

        [TestCase(ServiceName.MusicBrainz)]
        [TestCase(ServiceName.LastFm)]
        public void GetInstance_DoesNotThrowException(ServiceName service)
        {
            Assert.DoesNotThrow(() => 
            {
                WebServices.Instance(service);
            }
            );
        }

        [TestCase(ServiceName.MusicBrainz)]
        [TestCase(ServiceName.LastFm)]
        public void GetInstance_CheckIfInstanceIsSingleton(ServiceName service)
        {
            int checkCount = 3;
            IWebService serviceOne = WebServices.Instance(service);

            for (int i = 0; i < checkCount; i++)
            {
                IWebService serviceCurrent = WebServices.Instance(service);
                Assert.AreEqual(serviceOne, serviceCurrent);
                Assert.AreSame(serviceOne, serviceCurrent);
            }

        }
       
        [Test]
        public void DownloadContent_ArgumentCheck_ArgumentNullException_Expected()
        {
            Assert.Throws(typeof(ArgumentNullException), () => 
            { 
                WebServices.DownloadContent(null);
            }
            );
        }

        [Test]
        public void DownloadContent_ValidUrl_DoesNotThrowException()
        {
            Uri validUrl = new Uri("http://localhost");
            Assert.DoesNotThrow(() => 
            {
                WebServices.DownloadContent(validUrl);
            }
            );
        }

        [Test]
        //TODO: iTest or Expicit test...
        public void DownloadContent_ValidUrl_ResultNotEmptyOrNull()
        {
            Uri googleCom = new Uri("http://google.com");
            String content = WebServices.DownloadContent(googleCom);
            Assert.IsFalse(String.IsNullOrWhiteSpace(content),
                           String.Format("Content was empty from {0}", googleCom.ToString()));
        }

        [Test]
        public void DownloadContent_InvalidUrl_DoesNotThrowException()
        {
            Uri unreachableUrl = new Uri("htt://foobar.p");
            Assert.DoesNotThrow(() => 
            {
                WebServices.DownloadContent(unreachableUrl);
            }
            );
        }

        [TestCase(null,null)]
        [TestCase(null,"")]
        [TestCase("http://localhost","")]
        [TestCase("http://localhost",null)]
        public void BuildUri_ArgumentCheck_ArgumentNullException_Expected(String url, String query)
        {
            Uri testUri = url != null ? new Uri(url) : null;
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                WebServices.BuildUri(testUri, query);
            }
            );
        }


        [TestCase("http://localhost/","artist")]
        [TestCase("http://localhost/service","query=foobar")]
        [TestCase("http://localhost/service","query=foobar&condition=8")]
        [TestCase("http://localhost/service","query=foobar&condition=8&sort")]
        public void BuildUri_GetUrl(String url, String query)
        {
            String expected = String.Format("{0}?{1}", url, query);
            Uri actual = WebServices.BuildUri(new Uri(url), query);
            Assert.AreEqual(expected, actual.ToString());
        }

    }
}

