using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using SongTagger.Core;

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
            DataContext = new MainWindowViewModel(SongTagger.Core.Service.MusicData.Provider);
        }
    }


    public class MainWindowViewModelDesignData : MainWindowViewModel
    {
        public MainWindowViewModelDesignData()
            : base(DesignDataProvider.Instance)
        {
            WindowTitle = "Design data";
            InitDesignData(CartInit, Tracks);
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
                provider.SearchArtist("Rise Against").Select(e => new EntityViewModel(e)),
                Reset
                );
        }

        private void ReleaseGroupMarket()
        {
            Cart.EntityItem = new EntityViewModel(DesignDataProvider.RiseAgainst);
            Workspace = new MarketViewModel(State.SelectReleaseGroup,
               provider.BrowseReleaseGroups(DesignDataProvider.RiseAgainst).Select(e => new EntityViewModel(e)),
               Reset
               );
        }

        private void ReleaseMarket()
        {
            Cart.EntityItem = new EntityViewModel(DesignDataProvider.AppealToReason);
            Workspace = new MarketViewModel(State.SelectRelease,
               provider.BrowseReleases(DesignDataProvider.AppealToReason).Select(e => new EntityViewModel(e)),
               Reset
               );
        }

        private void Tracks()
        {
            Cart.EntityItem = new EntityViewModel(DesignDataProvider.AppealToReasonRelease);
            Workspace = new MapViewModel(provider.LookupTracks(DesignDataProvider.AppealToReasonRelease).Select(e => new MapEntityViewModel(e) {File = new FileInfo(Path.GetTempFileName())}),
              Reset
              );
        }

        private void CartInit()
        {
            Cart = new CartViewModel(LoadEntitiesAsync);  
        }

        private void ErrorMessage()
        {
            ShowErrorMessage(new NotImplementedException("Design data exception bla bla bla bla bla bla"),
                new ErrorActionViewModel("Ok", () => { }),
                new ErrorActionViewModel("Cancel", () => { })
            );
        }

    }

    public class DesignDataProvider : SongTagger.Core.Service.IProvider
    {
        #region Singleton pattern
        private static SongTagger.Core.Service.IProvider instance;
        public static SongTagger.Core.Service.IProvider Instance
        {
            get
            {
                if (instance == null)
                    instance = new DesignDataProvider();
                return instance;
            }
        }

        private DesignDataProvider()
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
            return new[]
                {
                    new Track(AppealToReasonRelease) {Name = "Savior", Number = 11, Posititon = 11, Length = TimeSpan.FromMilliseconds(123456)}, 
                    new Track(AppealToReasonRelease) {Name = "Savior", Number = 11, Posititon = 11, Length = TimeSpan.FromMilliseconds(123456)}, 
                    new Track(AppealToReasonRelease) {Name = "Savior", Number = 11, Posititon = 11, Length = TimeSpan.FromMilliseconds(123456)}, 
                };
        }
    }
}
