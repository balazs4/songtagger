﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using SongTagger.Core;
using SongTagger.UI.Wpf.View;
using MessageBox = System.Windows.MessageBox;

namespace SongTagger.UI.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if OFFLINE
            DataContext = new MainWindowViewModel(OfflineDataProvider.Instance, ShowErrorMessageBox, SelectDirectory);
#else
            DataContext = new MainWindowViewModel(SongTagger.Core.Service.MusicData.Provider, ShowErrorMessageBox, SelectDirectory);
#endif
        }

        private DirectoryInfo SelectDirectory(string initPath)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = true;
                dialog.SelectedPath = initPath;
                DialogResult result = dialog.ShowDialog();
                return result == System.Windows.Forms.DialogResult.OK
                    ? new DirectoryInfo(dialog.SelectedPath)
                    : new DirectoryInfo(initPath);
            }

        }


        private void ShowErrorMessageBox(Exception exception)
        {
            if (exception == null)
                return;

            string messageText = exception.Message;
            if (exception is AggregateException)
            {
                var messages = GetErrorMessages((AggregateException)exception);
                int maxErrorCount = 5;
                messageText = messages.Count() < maxErrorCount
                                  ? string.Join(Environment.NewLine, messages)
                                  : string.Join(Environment.NewLine,
                                                messages.Take(maxErrorCount).Concat(new[] { "...and other errors" }));
                Trace.TraceError(string.Join(Environment.NewLine, messages));
            }
            else
            {
                Trace.TraceError(string.Join(Environment.NewLine, messageText));
            }

            Action showError = () =>
                {
                    View.ErrorDialog dialog = new ErrorDialog(messageText)
                        {
                            Owner = this
                        };
                    dialog.ShowDialog();
                };

            //Action showError = () => MessageBox.Show(this, messageText, "Oops something went wrong...", MessageBoxButton.OK, MessageBoxImage.Error);
            Dispatcher.Invoke(showError, DispatcherPriority.Normal);
        }

        private static IEnumerable<string> GetErrorMessages(AggregateException aggregate)
        {
            if (aggregate == null)
                return new[] { string.Empty };

            var furtherAggregateExceptions = aggregate.InnerExceptions.Where(ex => ex is AggregateException);
            var simpleExceptions = aggregate.InnerExceptions.Except(furtherAggregateExceptions);

            var nestedMessages = furtherAggregateExceptions.Cast<AggregateException>().SelectMany(GetErrorMessages);

            return simpleExceptions.Select(ex => ex.Message).Concat(nestedMessages).Where(m => !String.IsNullOrWhiteSpace(m)).ToList();
        }
    }





    public class DynamicImage : Image
    {
        public static readonly RoutedEvent SourceChangedEvent = EventManager.RegisterRoutedEvent(
            "SourceChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(DynamicImage));

        static DynamicImage()
        {
            Image.SourceProperty.OverrideMetadata(typeof(DynamicImage), new FrameworkPropertyMetadata(SourcePropertyChanged));
        }

        public event RoutedEventHandler SourceChanged
        {
            add { AddHandler(SourceChangedEvent, value); }
            remove { RemoveHandler(SourceChangedEvent, value); }
        }

        private static void SourcePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Image image = obj as Image;
            if (image != null)
            {
                image.RaiseEvent(new RoutedEventArgs(SourceChangedEvent));
            }
        }
    }


    public class MainWindowViewModelDesignData : MainWindowViewModel
    {
        public MainWindowViewModelDesignData()
            : base(OfflineDataProvider.Instance, exception => { }, s => new DirectoryInfo(s))
        {
            WindowTitle = "Design data";
            InitDesignData(CartInit, Tracks);
        }

        private void ShowNotification()
        {
            LastTaggedAlbum = Tuple.Create(OfflineDataProvider.AppealToReason, new DirectoryInfo(@"C:\temp"), (byte[])null);
        }

        private void InitDesignData(params Action[] initActions)
        {
            foreach (Action initAction in initActions)
            {
                initAction();
            }
        }

        private void ArtistMarket()
        {
            Workspace = new MarketViewModel(State.SelectArtist,
                provider.SearchArtist("Rise Against").Select(e => new EntityViewModel(e))
                );
            Workspace.IsQueryRunning = true;
        }

        private void ReleaseGroupMarket()
        {
            ((CartViewModel)Cart).EntityItem = new EntityViewModel(OfflineDataProvider.RiseAgainst);
            Workspace = new MarketViewModel(State.SelectReleaseGroup,
               provider.BrowseReleaseGroups(OfflineDataProvider.RiseAgainst).Select(e => new EntityViewModel(e))
               );

        }

        private void ReleaseMarket()
        {
            ((CartViewModel)Cart).EntityItem = new EntityViewModel(OfflineDataProvider.AppealToReason);
            Workspace = new MarketViewModel(State.SelectRelease,
               provider.BrowseReleases(OfflineDataProvider.AppealToReason).Select(e => new EntityViewModel(e))
               );
        }

        private void Tracks()
        {
            ((CartViewModel)Cart).EntityItem = new EntityViewModel(OfflineDataProvider.AppealToReasonRelease);
            Workspace = new VirtualReleaseViewModel(provider.LookupTracks(OfflineDataProvider.AppealToReasonRelease), provider.DownloadCoverArts, (error) => { }, rgroup => { });
        }

        private void CartInit()
        {
            Cart = new CartViewModel(LoadEntitiesAsync, ResetToSearchArtist);
        }

        private void ErrorMessage()
        {

        }

        private void ShowSettings()
        {
            Settings = new SettingsViewModel(() => { }, s => new DirectoryInfo(s));
        }

    }

    public class OfflineDataProvider : SongTagger.Core.Service.IProvider
    {
        #region Singleton pattern
        private static SongTagger.Core.Service.IProvider instance;
        public static SongTagger.Core.Service.IProvider Instance
        {
            get
            {
                if (instance == null)
                    instance = new OfflineDataProvider();
                return instance;
            }
        }

        private OfflineDataProvider()
        {

        }
        #endregion

        private static List<Tag> CreateTagList(params string[] tags)
        {
            return tags.Select(tag => new Tag { Name = tag }).ToList();
        }

        internal static Artist RiseAgainst
        {
            get
            {
                Artist artist = new Artist
                {
                    Id = new Guid("606bf117-494f-4864-891f-09d63ff6aa4b"),
                    Name = "Rise Against",
                    Tags = CreateTagList("rock", "punk", "american"),
                    Score = 100
                };

                return artist;
            }
        }

        internal static ReleaseGroup AppealToReason
        {
            get
            {
                return new ReleaseGroup(RiseAgainst)
                {
                    Id = new Guid("0b0e4477-4b04-3683-8f01-3a4544c36b41"),
                    Name = "Appeal to Reason",
                    PrimaryType = ReleaseGroupType.Album,
                    FirstReleaseDate = new DateTime(2008, 10, 2),
                };
            }
        }

        internal static Release AppealToReasonRelease
        {
            get
            {
                return new Release(AppealToReason)
                {
                    Id = new Guid("205f2019-fc18-477a-971c-ecc37aa216fc"),
                    Name = AppealToReason.Name,
                    Country = "DE",
                    Status = "Official"
                };
            }
        }

        public IEnumerable<Artist> SearchArtist(string name)
        {
            return new[]
                {
                    new Artist {Name = "Def Leppard", Score = 100, Type = ArtistType.Group, Tags = CreateTagList("british","classic pop and rock","english","glam metal","hard rock","heavy metal","metal","nwobhm","pop rock","rock","uk")},
                    RiseAgainst,
                    new Artist {Name = "Rise", Score = 80, Type = ArtistType.Person, Tags = CreateTagList("pop", "shit")},
                    new Artist {Name = "Foobar", Score = 10, Type = ArtistType.Person, Tags = CreateTagList("wtf", "this", "shit")},
                };
        }

        public IEnumerable<ReleaseGroup> BrowseReleaseGroups(Artist artist)
        {
            return new[]
                {
                    AppealToReason, AppealToReason,
                    new ReleaseGroup(RiseAgainst) {Name = "One single", PrimaryType = ReleaseGroupType.Single},
                    new ReleaseGroup(RiseAgainst) {Name = "EP (maxi)", PrimaryType = ReleaseGroupType.EP},
                    new ReleaseGroup(RiseAgainst) {Name = "Another single", PrimaryType = ReleaseGroupType.Single},
                };
        }

        public IEnumerable<Release> BrowseReleases(ReleaseGroup releaseGroup)
        {
            return new[]
                {
                    AppealToReasonRelease,
                    new Release(AppealToReason) {Country = "UK", Name = "Foobar", Status = "Unreleased"},
                    new Release(AppealToReason) {Country = "HU", Name = "Another release", Status = "Official"} 
                };
        }

        public IEnumerable<Track> LookupTracks(Release release)
        {
            Random random = new Random();
            return Enumerable.Range(1, 18)
                .Select(i => new Track(AppealToReasonRelease)
                {
                    Name = "Track #" + i,
                    Position = i,
                    Length = TimeSpan.FromMilliseconds(random.Next(100000, 300000))
                }
                );
        }

        public void DownloadCoverArts(IEnumerable<Uri> uri, Action<CoverArt> callback, CancellationToken token)
        {
            var uriList =
                    Directory.EnumerateFileSystemEntries(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "*.*")
                             .Take(12)
                             .Select(f => new Uri(f)); ;
          
            foreach (Uri pic in uriList)
            {
                using (FileStream fs = File.Open(pic.LocalPath, FileMode.Open))
                {
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, (int)fs.Length);
                    callback(CoverArt.CreateCoverArt(pic, data));
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
    }
}
