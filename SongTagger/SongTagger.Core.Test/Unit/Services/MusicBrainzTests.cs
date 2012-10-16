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
using System.IO;
using System.Xml.Linq;
using System.Collections;
using System.Reflection;

namespace SongTagger.Core.Test.Unit.Services
{
    class MusicBrainzTestCaseSource
    {

        private List<IArtist> artistCollection;

        public MusicBrainzTestCaseSource()
        {
            artistCollection = new List<IArtist>();
            #region Artist with genres
            {
                IArtist riseAgainst = new Artist() 
                { 
                    Name="Rise Against", 
                    Id = new Guid("606bf117-494f-4864-891f-09d63ff6aa4b"),
                };

                riseAgainst.Genres.AddRange(
                    new List<String> 
                    {
                    "rock",
                    "punk",
                    "american",
                    "punk rock",
                    "usa",
                    "hardcore punk",
                    "melodic hardcore",
                    "rock and indie",
                    "nervous breakdown"
                    }
                );
                artistCollection.Add(riseAgainst);
            }
            #endregion

            #region Artist without genres
            {
                IArtist depresszio = new Artist() 
                {
                    Name = "Depresszió",
                    Id = new Guid("79a8d8a6-012a-4dd9-b5e2-ed4b52a5d55e")
                };

                artistCollection.Add(depresszio);
            }
            #endregion

            #region Unknow artist
            {
                IArtist unknow = new UnknowArtist();
                artistCollection.Add(unknow);
            }
            #endregion
        }

        internal IEnumerable ParseXmlToArtistTestFactory
        {
            get
            {
                yield return new TestCaseData("MusicBrainzTest.ParseXmlToArtist.ValidArtistWithGenres.xml", 99, artistCollection.FirstOrDefault(a => a.Name == "Rise Against"));
                yield return new TestCaseData("MusicBrainzTest.ParseXmlToArtist.ValidArtistWithoutGenres.xml", 100, artistCollection.FirstOrDefault(a => a.Name == "Depresszió"));
                yield return new TestCaseData("MusicBrainzTest.Error.xml", 0, artistCollection.FirstOrDefault(a => a is UnknowArtist));
            }       
        }
    }

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
                return DateTime.Now.ToString("HH:mm:ss");
            };

            fakeUri = new Uri("http://localhost");
        }

        [Test]
        [Category("LongRunner")]
        public void DownloadContentSafely_CheckTimeDifferenceBetweenQueries_ExplicitTwoQueries()
        {
            String first = MusicBrainz.DownloadContentSafely(fakeUri, fakeDownloadAction);
            String second = MusicBrainz.DownloadContentSafely(fakeUri, fakeDownloadAction);    

            TimeSpan difference = DateTime.Parse(second) - DateTime.Parse(first);
            Assert.GreaterOrEqual(difference, MusicBrainz.WAIT_TIME_BETWEEN_QUERIES);
        }

        [Test]
        [Category("LongRunner")]
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

        [Test]
        public void ExcuteQueryArgumentCheckTest()
        {
            MusicBrainz instance = new MusicBrainz();
            Assert.That(() => {
                instance.ExecuteQuery(null);}, Throws.ArgumentException);
        }

        [TestCase(null, "")]
        [TestCase("","")]
        [TestCase("Korn","Korn")]
        [TestCase("Boy","Boy")]
        [TestCase("RiseAgainst","Rise Against")]
        public void ArtistUriTest(string name, string expectedQueryParameter)
        {
            Uri resultUri = null;
            Assert.That(() =>
            {
                resultUri = MusicBrainz.CreateArtistQueryUri(name);
            }, Throws.Nothing);

            Assert.That(resultUri, Is.Not.Null);
            string expected;
            if (String.IsNullOrWhiteSpace(expectedQueryParameter))
            {
                expected = String.Format("http://musicbrainz.org/ws/2/artist?query=");
            }
            else
            {
                expected = String.Format("http://musicbrainz.org/ws/2/artist?query=artist:{0} AND type:group", expectedQueryParameter);
            }
            
            Assert.That(resultUri.ToString(), Is.EqualTo(expected));
        }
    
        [TestCase("RiseAgainst", Result = "Rise Against")]
        [TestCase("TheNakedAndFamous", Result = "The Naked And Famous")]
        [TestCase("6test", Result = "6test")]
        [TestCase("4Lyn", Result = "4 Lyn")]
        [TestCase("12Stones", Result = "12 Stones")]
        [TestCase("36Crazyfits", Result = "36 Crazyfits")]
        [TestCase("The69Eyes", Result = "The 69 Eyes")]
        [TestCase("POD", Result = "P.O.D.")]
        [TestCase("The Offspring", Result = "The Offspring")]
        [TestCase("_unknow", Result = "_unknow")]
        public string ArtistNameSplitterTest(string rawName)
        {
            string actual = String.Empty;
            Assert.That(() =>
            {
                actual = MusicBrainz.SplitArtistName(rawName);
            }, Throws.Nothing);

            return actual;
        }


        #region Parsing tests
        [TestCaseSource(typeof(MusicBrainzTestCaseSource),"ParseXmlToArtistTestFactory")]
        public void ParseXmlToArtistTest(string xmlFileName, int minimumScore, IArtist expectedArtist)
        {
            string xmlPath = TestHelper.GetInputDataFilePath(xmlFileName);

            if (!File.Exists(xmlPath))
            {
                Assert.Ignore("{0} file is not available.", xmlPath);
            }

            IArtist artist = null;
            Assert.That(() => 
            {
                artist = MusicBrainz.ParseXmlToArtist(XDocument.Load(xmlPath), minimumScore);
            }, 
            Throws.Nothing);

            Assert.That(artist, Is.Not.Null);
            Assert.That(artist.GetType(), Is.EqualTo(expectedArtist.GetType()));
            Assert.That(artist.Name, Is.EqualTo(expectedArtist.Name));
            Assert.That(artist.Id, Is.EqualTo(expectedArtist.Id));
            Assert.That(artist.Genres, Is.EquivalentTo(expectedArtist.Genres));
        }

        [Test]
        public void ParseXmlToArtistTest_IfXDocumentIsNull_UnknowArtistExpected()
        {
            IArtist artist = null;
            Assert.That(() => 
            {
                artist = MusicBrainz.ParseXmlToArtist(null, 0);
            }, 
            Throws.Nothing);

            Assert.That(artist, Is.Not.Null);
            Assert.That(artist, Is.InstanceOf<UnknowArtist>());

        
        }
        #endregion


    }
}

