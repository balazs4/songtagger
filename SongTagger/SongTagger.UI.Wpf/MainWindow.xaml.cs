using System;
using System.Collections.Generic;
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
            WindowTitle = GetType().Namespace + " | Design data";
            Artist = new ArtistViewModel();
            Artist.Status = ArtistViewModelStatus.DisplayInfo;
            Artist.ArtistName = "Design artist";
            Artist.ArtistGenres.Add("genre");


            GetAlbumsCommand.Execute(null);

        }
    }


    public class DesignDataProvider : SongTagger.Core.Service.IProvider
    {
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

        public IArtist GetArtist(string nameStub)
        {
            System.Threading.Thread.Sleep(5000);
            return new ArtistDesignData { Name = "Desing band", Genres = new List<string> { "rock", "metal" } };
        }

        public IEnumerable<IAlbum> GetAlbums(IArtist artist)
        {
            System.Threading.Thread.Sleep(1000);
            List<IAlbum> albums = new List<IAlbum>
                {
                    new AlbumDesignData 
                    { 
                        Name = "Retro active", ReleaseDate = new DateTime(1993,1,1), TypeOfRelease = ReleaseType.Album, 
                        Covers = new List<ICoverArt>{ new CoverArtDesignData{SizeCategory = SizeType.Large, Url = new Uri("http://images.uulyrics.com/cover/d/def-leppard/album-retro-active.jpg") }}
                    },

                    new AlbumDesignData 
                    { 
                        Name = "Hysteria (unrelease B-side tracks live demo)", ReleaseDate = new DateTime(1987,1,1), TypeOfRelease = ReleaseType.Album, 
                        Covers = new List<ICoverArt>{ new CoverArtDesignData{SizeCategory = SizeType.Large, Url = new Uri("http://4.bp.blogspot.com/-FZCXTJLTFYk/TfMPiqBgw7I/AAAAAAAAALg/yMicagJg3Xk/s1600/HISTERIA.jpg") }}
                    },

                    new AlbumDesignData
                    {
                            Name = "In the round, in your face live",
                            ReleaseDate = new DateTime(1988,08,08),
                            TypeOfRelease = ReleaseType.Live,
                            Covers = new List<ICoverArt>()
                    }
                };
            return albums;
        }

        public IEnumerable<IRelease> GetReleases(IAlbum album)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ISong> GetSongs(IRelease release)
        {
            throw new NotImplementedException();
        }
    }

    public class ArtistDesignData : IArtist
    {

        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<string> Genres { get; set; }
    }

    public class AlbumDesignData : IAlbum
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IArtist ArtistOfRelease { get; set; }
        public IList<ICoverArt> Covers { get; set; }
        public DateTime ReleaseDate { get; set; }
        public ReleaseType TypeOfRelease { get; set; }
    }

    public class CoverArtDesignData : ICoverArt
    {
        public Uri Url { get; set; }
        public SizeType SizeCategory { get; set; }
    }
}
