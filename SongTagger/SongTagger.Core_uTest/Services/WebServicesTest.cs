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

namespace SongTagger.Core.Unittest
{
    [TestFixture()]
    public class WebServicesTest
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
                IWebService actual = WebServices.Instance(service);
            });
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
    }
}

