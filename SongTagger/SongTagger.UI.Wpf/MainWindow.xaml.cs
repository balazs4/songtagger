using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SongTagger.Core;
using SongTagger.UI.Wpf.ViewModel;

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
            InitArtistMarket();
            Workspace = new MarketViewModel(State.SelectArtist,
                new EntityViewModel[0],
                Reset
                );
        }

        private void InitArtistMarket()
        {
            Workspace = new MarketViewModel(State.SelectArtist, 
                provider.SearchArtist("Rise Against").Select(e => new EntityViewModel(e)),
                Reset
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
                    AppealToReason
                };
        }

        public IEnumerable<Release> BrowseReleases(ReleaseGroup releaseGroup)
        {
            return new[]
                {
                    AppealToReasonRelease
                };
        }

        public IEnumerable<Track> LookupTracks(Release release)
        {
            return new[]
                {
                    new Track(AppealToReasonRelease) {Name = "Savior", Number = 11, Posititon = 11, Length = TimeSpan.FromSeconds(123456)}, 
                };
        }
    }
}
