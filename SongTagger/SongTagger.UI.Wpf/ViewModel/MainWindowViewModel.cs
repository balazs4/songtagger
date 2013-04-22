using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            provider = dataProvider;
            WindowTitle = GetType().Namespace.ToString().Split('.').FirstOrDefault();
            StatusCollection = new ObservableCollection<string>();
            Artist = new ArtistViewModel();
            SearchArtistCommand = new DelegateCommand(SearchArtistExecute, CanExecuteSearchArtist);
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

        private ObservableCollection<string> statusCollection;
        public ObservableCollection<string> StatusCollection
        {
            get { return statusCollection; }
            set
            {
                statusCollection = value;
                RaisePropertyChangedEvent("StatusCollection");
            }
        }

        #region Commands
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
        }

        public void Drop(IDropInfo dropInfo)
        {
            System.Windows.DataObject dataObject = dropInfo.Data as DataObject;
            string directory = ((string[])dataObject.GetData(DataFormats.FileDrop)).FirstOrDefault();
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            StatusCollection.Add("Search for '" + directoryInfo.Name + "' artist...");
            SearchArtistCommand.Execute(directoryInfo.Name);
        }


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
        internal IArtist Artist { get; private set; }

        public ArtistViewModel()
        {
            Status = ArtistViewModelStatus.WaitingForUser;
            ArtistGenres = new ObservableCollection<string>();
        }

        private string artistName;
        public string ArtistName
        {
            get { return artistName; }
            private set
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
            Artist = artist;
            ArtistName = artist.Name;
            ArtistGenres = new ObservableCollection<string>(artist.Genres);
            Status = ArtistViewModelStatus.DisplayInfo;
        }
    }

}
