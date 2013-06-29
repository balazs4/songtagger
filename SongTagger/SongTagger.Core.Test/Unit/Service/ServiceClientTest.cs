//
//  ServiceClientTest.cs
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
using NUnit.Framework;
using SongTagger.Core.Service;
using System.Diagnostics;
using System.Threading;

namespace SongTagger.Core.Test.Unit.Service
{
    [TestFixture]
    public class ServiceClientTest
    {
        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            ServiceClient.Download = (uri) => DateTime.Now.ToString();
        }

        [Test]
        public void DownloadContent()
        {
            Uri fakeUri = new Uri("http://localhost/songtagger/test");
            string content = ServiceClient.DownloadContent(fakeUri, null);
            Assert.IsNotNullOrEmpty(content);
            DateTime date = DateTime.Parse(content);
            Assert.AreNotEqual(DateTime.MinValue, date);
        }

        [Test]
        public void DownloadContent_MultiThreading()
        {
            Uri fakeUri = new Uri("http://localhost/songtagger/test");

            Action<DateTime> fakePrepareAction = (date) => 
            {
                Thread.Sleep(500);
            };

            int counter = 0;
            ServiceClient.Download = (uri) => 
            {
                Thread.Sleep(500);
                counter++;
                return counter.ToString();
            };

            Action first = () => ServiceClient.DownloadContent(fakeUri, fakePrepareAction);
            Action second = () => ServiceClient.DownloadContent(fakeUri, fakePrepareAction);

            ManualResetEvent manual = new ManualResetEvent(false);

            AsyncCallback callback = (asyncResult) => 
            {
               manual.Set();
            };

            first.BeginInvoke(callback, null);
            second.Invoke();

            if (!manual.WaitOne(new TimeSpan(0,0,5)))
            {
                Assert.Fail("No async result");
            }

            Assert.That(counter, Is.EqualTo(2));
        }
    }
}
