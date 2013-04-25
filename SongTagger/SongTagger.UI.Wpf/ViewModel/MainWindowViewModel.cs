using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;
using SongTagger.Core;
using SongTagger.Core.Service;

namespace SongTagger.UI.Wpf.ViewModel
{




    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainWindowViewModel : ViewModelBase, IDropTarget
    {
        private readonly IProvider provider;

        public MainWindowViewModel(IProvider dataProvider)
        {
            PropertyChanged += OnPropertyChangedDispatcher;

            provider = dataProvider;
            WindowTitle = GetType().Namespace.ToString().Split('.').FirstOrDefault();

            Artist = new ArtistViewModel();

            ResetCommand = new DelegateCommand(ResetExecute, CanExecuteReset);
            SearchArtistCommand = new DelegateCommand(SearchArtistExecute, CanExecuteSearchArtist);
            GetAlbumsCommand = new DelegateCommand(GetAlbumsExecute, CanExecuteGetAlbums);
        }

        private void OnPropertyChangedDispatcher(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == "Artist")
            {
                Status = ViewModelStatus.ArtistSearch;
                Albums = new ObservableCollection<AlbumViewModel>();
                Albums.CollectionChanged += Albums_CollectionChangedDispatcher;
            }

        }

        void Albums_CollectionChangedDispatcher(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Status = ViewModelStatus.AlbumsReady;
            }
        }

        private string windowTitle;
        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                windowTitle = value;
                RaisePropertyChangedEvent("WindowTitle");
            }
        }

        private ViewModelStatus status;
        public ViewModelStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChangedEvent("Status");
            }
        }

        #region Reset command
        public ICommand ResetCommand { get; private set; }
        private void ResetExecute(object parameter)
        {
            Artist = new ArtistViewModel();
        }

        private bool CanExecuteReset(object parameter)
        {
            return true;
        }
        #endregion


        #region SearchArtist Command
        public ICommand SearchArtistCommand { get; private set; }
        private void SearchArtistExecute(object parameter)
        {
            if (Artist == null)
                return;

            string searchPattern = Artist.SearchText;
            if (String.IsNullOrWhiteSpace(searchPattern))
                return;

            Func<string, IArtist> searchArtistAsync = provider.GetArtist;
            searchArtistAsync.BeginInvoke(searchPattern, FoundArtistCallBack, searchArtistAsync);
            Artist.Status = ArtistViewModelStatus.SearchInProgress;
        }

        private bool CanExecuteSearchArtist(object parameter)
        {
            if (Artist == null)
                return false;

            if (Artist.Status != ArtistViewModelStatus.WaitingForUser)
                return false;

            return !String.IsNullOrWhiteSpace(Artist.SearchText);
        }

        private void FoundArtistCallBack(IAsyncResult asyncResult)
        {
            Func<string, IArtist> asyncCall = asyncResult.AsyncState as Func<string, IArtist>;
            if (asyncCall == null)
                return;

            IArtist result = asyncCall.EndInvoke(asyncResult);
            Artist.Fill(result);
        }
        #endregion

        private ArtistViewModel artist;
        public ArtistViewModel Artist
        {
            get { return artist; }
            set
            {
                artist = value;
                RaisePropertyChangedEvent("Artist");
            }
        }

        #region GetAlbums Command
        public ICommand GetAlbumsCommand { get; private set; }

        private void GetAlbumsExecute(object parameter)
        {
            Func<IArtist, IEnumerable<IAlbum>> getAlbumsAsync = provider.GetAlbums;
            getAlbumsAsync.BeginInvoke(Artist.ArtistModel, FillAlbumListCallback, getAlbumsAsync);
            Status = ViewModelStatus.GettingAlbums;
        }

        private void FillAlbumListCallback(IAsyncResult asyncResult)
        {
            Func<IArtist, IEnumerable<IAlbum>> asyncCall = asyncResult.AsyncState as Func<IArtist, IEnumerable<IAlbum>>;
            if (asyncCall == null)
                return;

            IEnumerable<IAlbum> albumList = asyncCall.EndInvoke(asyncResult);
            foreach (IAlbum album in albumList)
            {
                Action addAction = () => Albums.Add(AlbumViewModel.CreateAlbumViewModel(album));
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, addAction);
            }
        }

        private bool CanExecuteGetAlbums(object parameter)
        {
            if (Artist == null)
                return false;

            if (Artist.ArtistModel is UnknownArtist)
                return false;

            if (Status != ViewModelStatus.ArtistSearch)
                return false;

            if (Artist.Status == ArtistViewModelStatus.DisplayInfo)
                return true;


            return false;
        }
        #endregion


        private ObservableCollection<AlbumViewModel> albums;
        public ObservableCollection<AlbumViewModel> Albums
        {
            get { return albums; }
            private set
            {
                albums = value;
                RaisePropertyChangedEvent("Albums");
            }
        }

        #region IDroptTarget Implementation

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo == null)
            {
                return;
            }

            System.Windows.DataObject dataObject = dropInfo.Data as DataObject;
            if (dataObject == null || !dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string directory = ((string[])dataObject.GetData(DataFormats.FileDrop)).FirstOrDefault();
            if (directory == null || !Directory.Exists(directory))
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }

            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = DragDropEffects.Copy;
            Artist.SearchText = new DirectoryInfo(directory).Name;
        }

        public void Drop(IDropInfo dropInfo)
        {
            System.Windows.DataObject dataObject = dropInfo.Data as DataObject;
            string directory = ((string[])dataObject.GetData(DataFormats.FileDrop)).FirstOrDefault();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            SearchArtistCommand.Execute(directoryInfo.Name);
        }

        #endregion
    }

    public enum ViewModelStatus
    {
        ArtistSearch,
        GettingAlbums,
        AlbumsReady,
        GetSongs,
        SaveTags
    }


    public class ArtistDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate WaitingForUserDataTemplate { get; set; }
        public DataTemplate SearchInProgressDataTemplate { get; set; }
        public DataTemplate DisplayInfoDataTemplate { get; set; }
        public DataTemplate DropTargetDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            ArtistViewModel artistViewModel = item as ArtistViewModel;
            if (artistViewModel == null)
                return base.SelectTemplate(item, container);


            switch (artistViewModel.Status)
            {
                case ArtistViewModelStatus.SearchInProgress:
                    return SearchInProgressDataTemplate;

                case ArtistViewModelStatus.DisplayInfo:
                    return DisplayInfoDataTemplate;

                case ArtistViewModelStatus.DropTarget:
                    return DropTargetDataTemplate;

                case ArtistViewModelStatus.WaitingForUser:
                default:
                    return WaitingForUserDataTemplate;
            }
        }
    }

    public enum ArtistViewModelStatus
    {
        WaitingForUser,
        SearchInProgress,
        DisplayInfo,
        DropTarget
    }

    public class ArtistViewModel : ViewModelBase
    {
        internal IArtist ArtistModel { get; private set; }

        public ArtistViewModel()
        {
            Status = ArtistViewModelStatus.WaitingForUser;
            ArtistGenres = new ObservableCollection<string>();
        }

        private string artistName;
        public string ArtistName
        {
            get { return artistName; }
            internal set
            {
                artistName = value;
                RaisePropertyChangedEvent("ArtistName");
            }
        }

        private ObservableCollection<string> artistGenres;
        public ObservableCollection<string> ArtistGenres
        {
            get { return artistGenres; }
            private set
            {
                artistGenres = value;
                RaisePropertyChangedEvent("ArtistGenres");
            }
        }

        private string searchText;
        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                RaisePropertyChangedEvent("SearchText");
            }
        }

        private ArtistViewModelStatus status;
        public ArtistViewModelStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChangedEvent("Status");
            }
        }

        public void Fill(IArtist artist)
        {
            ArtistModel = artist;
            ArtistName = artist.Name;
            ArtistGenres = new ObservableCollection<string>(artist.Genres.Take(8));
            Status = ArtistViewModelStatus.DisplayInfo;
        }
    }

    public class AlbumViewModel : ViewModelBase
    {
        internal IAlbum AlbumModel { get; private set; }

        internal AlbumViewModel()
        {

        }

        public static AlbumViewModel CreateAlbumViewModel(IAlbum model)
        {
            AlbumViewModel instance = new AlbumViewModel
                {
                    AlbumModel = model,
                    AlbumName = model.Name,
                    Cover = model.Covers.Where(c => c.SizeCategory == SizeType.Large).Select(c => c.Url).FirstOrDefault(),
                    Year = model.ReleaseDate.Year,
                    AlbumType = model.TypeOfRelease.ToString()
                };
            return instance;
        }

        private string albumName;
        public string AlbumName
        {
            get { return albumName; }
            set
            {
                albumName = value;
                RaisePropertyChangedEvent("AlbumName");
            }
        }

        private int year;
        public int Year
        {
            get { return year; }
            set
            {
                year = value;
                RaisePropertyChangedEvent("Year");
            }
        }

        private Uri cover;
        public Uri Cover
        {
            get { return cover; }
            set
            {
                cover = value;
                RaisePropertyChangedEvent("Cover");
            }
        }

        private string albumType;
        public string AlbumType
        {
            get { return albumType; }
            set
            {
                albumType = value;
                RaisePropertyChangedEvent("AlbumType");
            }
        }
    }

}
