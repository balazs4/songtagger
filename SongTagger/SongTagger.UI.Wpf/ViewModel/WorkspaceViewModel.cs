using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;
using SongTagger.Core;
using SongTagger.UI.Wpf.ViewModel;

namespace SongTagger.UI.Wpf
{
    public class WorkspaceViewModel : ViewModelBase
    {
        public static string GetDescriptionFromEnumValue(Enum value)
        {
            DescriptionAttribute attribute = value.GetType()
                                                  .GetField(value.ToString())
                                                  .GetCustomAttributes(typeof(DescriptionAttribute), false)
                                                  .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public WorkspaceViewModel(State state)
        {
            Header = GetDescriptionFromEnumValue(state);
        }

        private string header;
        public string Header
        {
            get { return header; }
            set
            {
                header = value;
                RaisePropertyChangedEvent("Header");
            }
        }

        private bool isQueryRunning;
        public bool IsQueryRunning
        {
            get { return isQueryRunning; }
            set
            {
                isQueryRunning = value;
                RaisePropertyChangedEvent("IsQueryRunning");
            }
        }
    }

    public class SearchViewmodel : WorkspaceViewModel
    {

        public SearchViewmodel(Action<string> searchCallback)
            : base(State.SearchForAritst)
        {
            Search = new DelegateCommand
                (
                    param => searchCallback(SearchText),
                    param => !String.IsNullOrWhiteSpace(SearchText)
                );
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

        public ICommand Search { get; private set; }
    }

    public class MarketViewModel : WorkspaceViewModel
    {
        public MarketViewModel(State state, IEnumerable<EntityViewModel> entities, Action resetCallback)
            : base(state)
        {
            Entities = new ObservableCollection<EntityViewModel>(entities);
            Reset = new DelegateCommand(param => resetCallback());
        }

        private string filterText;
        public string FilterText
        {
            get { return filterText; }
            set
            {
                filterText = value;
                RaisePropertyChangedEvent("FilterText");
                RaisePropertyChangedEvent("EntitiesView");
            }
        }

        private ObservableCollection<EntityViewModel> entities;
        public ObservableCollection<EntityViewModel> Entities
        {
            get { return entities; }
            set
            {
                entities = value;
                //RaisePropertyChangedEvent("Entities");
                RaisePropertyChangedEvent("EntitiesView");
            }
        }


        private ICollectionView entitiesView;
        public ICollectionView EntitiesView
        {
            get
            {
                if (entitiesView == null)
                {
                    entitiesView = CollectionViewSource.GetDefaultView(Entities);
                }

                if (String.IsNullOrWhiteSpace(FilterText))
                    entitiesView.Filter = null;
                else
                    entitiesView.Filter = Filter;

                return entitiesView;
            }
        }

        private bool Filter(object parameter)
        {
            if (String.IsNullOrWhiteSpace(FilterText))
                return true;

            string text = FilterText.ToLower();
            EntityViewModel item = (EntityViewModel)parameter;

            if (item.Entity.Name.ToLower().Contains(text))
                return true;

            IEntity entity = item.Entity;

            if (entity is Artist)
            {
                Artist artist = (Artist)entity;
                return PerformFilter(text,
                                     artist.Type.ToString(),
                                     string.Join(" ", artist.Tags.Select(t => t.Name)),
                                     artist.Score.ToString());
            }

            if (entity is ReleaseGroup)
            {
                ReleaseGroup group = (ReleaseGroup)entity;
                return PerformFilter(text, group.PrimaryType.ToString(), group.FirstReleaseDate.Year.ToString());
            }

            return false;
        }

        private static bool PerformFilter(string text, params string[] properties)
        {
            return properties.Any(s => s.ToLower().Contains(text));
        }

        public ICommand Reset { get; private set; }
    }

    public class VirtualReleaseViewModel : WorkspaceViewModel
    {
        private Artist artist;
        public Artist Artist
        {
            get { return artist; }
            private set
            {
                artist = value;
                RaisePropertyChangedEvent("Artist");
            }
        }

        private ReleaseGroup releaseGroup;
        public ReleaseGroup ReleaseGroup
        {
            get { return releaseGroup; }
            private set
            {
                releaseGroup = value;
                RaisePropertyChangedEvent("ReleaseGroup");
            }
        }

        private ObservableCollection<CoverArt> covers;
        public ObservableCollection<CoverArt> Covers
        {
            get { return covers; }
            private set
            {
                covers = value;
                RaisePropertyChangedEvent("Covers");
            }
        }

        private ObservableCollection<Song> songs;
        public ObservableCollection<Song> Songs
        {
            get { return songs; }
            private set
            {
                songs = value;
                RaisePropertyChangedEvent("Songs");
            }
        }

        public VirtualReleaseViewModel(IEnumerable<Track> tracks, Action resetCallback, Action<IEnumerable<Uri>, Action<CoverArt>> coverDownloaderService)
            : base(State.MapTracks)
        {
            Reset = new DelegateCommand(p => resetCallback());

            ReleaseGroup = tracks.First().Release.ReleaseGroup;
            Artist = ReleaseGroup.Artist;

            InitCovers(tracks, coverDownloaderService);

            Songs = new ObservableCollection<Song>(tracks.Select(t => new Song {Track = t}));
        }

        public ICommand Reset { get; private set; }

        private void InitCovers(IEnumerable<Track> tracks, Action<IEnumerable<Uri>, Action<CoverArt>> coverDownloaderService)
        {
            List<Uri> coverUriList = tracks.ToLookup(t => t.Release)
                                           .Where(group => group.Key.HasPreferredCoverArt)
                                           .Select(group => group.Key.GetCoverArt())
                                           .ToList();
            //TODO: DisCogs,Last.FM
            Task download = Task.Factory.StartNew(() => coverDownloaderService(coverUriList, AddToCoverArtCollectionThreadSafety));
            
            download.ContinueWith(task =>
                {
                    Action addAction = () =>
                    {
                        if (Covers == null)
                            Covers = new ObservableCollection<CoverArt>();

                        if (Covers.Any())
                            return;
                        Covers.Add(CoverArt.CreateCoverArt(null, null));
                    };
                    Application.Current.Dispatcher.BeginInvoke(addAction, DispatcherPriority.Normal);
                });
        }

        private void AddToCoverArtCollectionThreadSafety(CoverArt item)
        {
            if (item == null)
                return;

            if (item.Url == null)
                return;

            if (item.Data == null)
                return;

            if (!item.Data.Any())
                return;

            Action addAction = () =>
                {
                    if (Covers == null)
                        Covers = new ObservableCollection<CoverArt>();
                    Covers.Add(item);
                };
            Application.Current.Dispatcher.BeginInvoke(addAction, DispatcherPriority.Normal);
        }

    }



    public class Song
    {
        public Track Track { get; set; }
        public IEnumerable<Track> SimilarTracks { get; set; }
    }
}