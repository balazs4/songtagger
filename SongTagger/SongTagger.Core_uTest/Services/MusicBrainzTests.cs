//
//  MusicBrainzTest.cs
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
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;
using System.Threading;

namespace SongTagger.Core.UnitTest
{
    [TestFixture()]
    public class MusicBrainzTests
    {
        private WebServices.DownloadContentDelegate fakeDownloadAction;
        private Uri fakeUri;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            fakeDownloadAction = delegate(Uri uri)
            {
                return DateTime.Now.ToString();
            };

            fakeUri = new Uri("http://localhost");
        }

        [Test]
        public void DownloadContentSafely_CheckTimeDifferenceBetweenQueries_ExplicitTwoQueries()
        {
            String first = MusicBrainz.DownloadContentSafely(fakeUri, fakeDownloadAction);
            String second = MusicBrainz.DownloadContentSafely(fakeUri, fakeDownloadAction);    

            TimeSpan difference = DateTime.Parse(second) - DateTime.Parse(first);
            Assert.GreaterOrEqual(difference, MusicBrainz.WAIT_TIME_BETWEEN_QUERIES);
        }

        [Test]
        public void DownloadContentSafely_CheckTimeDifferenceBetweenQueries_MultiThreadAccess()
        {
            String first = null;
            AutoResetEvent resetEvent = new AutoResetEvent(false);

            Thread anotherThread = new Thread(() => 
            {
                first = MusicBrainz.DownloadContentSafely(fakeUri, fakeDownloadAction);
                resetEvent.Set();
            }
            );

            anotherThread.Start();
            String second = MusicBrainz.DownloadContentSafely(fakeUri, fakeDownloadAction);

            resetEvent.WaitOne(new TimeSpan(0, 0, 10));

            if (String.IsNullOrWhiteSpace(first))
                Assert.Fail("Thread was not started or not finished properly.");

            TimeSpan difference = DateTime.Parse(second) - DateTime.Parse(first);
            Assert.GreaterOrEqual(difference.Duration(), MusicBrainz.WAIT_TIME_BETWEEN_QUERIES);

        }

    }
}

