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
            StatusCollection.Add("[START] This is only design data....");
            StatusCollection.Add("This is only design data....ready");
            Artist = new ArtistViewModel();
            //SearchArtistCommand.Execute("Foobar artist");
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
            return new ArtistDesignData {Name = "Desing band", Genres = new List<string> {"rock", "metal"}};
        }

        public IEnumerable<IAlbum> GetAlbums(IArtist artist)
        {
            throw new NotImplementedException();
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
}
