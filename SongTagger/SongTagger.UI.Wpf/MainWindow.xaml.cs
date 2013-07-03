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

        public IEnumerable<Artist> SearchArtist(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ReleaseGroup> BrowseReleaseGroups(Artist artist)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Release> BrowseReleases(ReleaseGroup releaseGroup)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Track> LookupTracks(Release release)
        {
            throw new NotImplementedException();
        }
    }
}
