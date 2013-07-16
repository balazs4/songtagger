using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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

    public class MapViewModel : WorkspaceViewModel
    {
        public MapViewModel(IEnumerable<MapEntityViewModel> entities, Action resetCallback)
            : base(State.MapTracks)
        {
            Reset = new DelegateCommand(param => resetCallback());

            Entities = new ObservableCollection<MapEntityViewModel>(entities);
            if (Entities.Count == 0)
                return;
            Track = Entities.First().Track;
        }

        private ObservableCollection<MapEntityViewModel> entities;
        public ObservableCollection<MapEntityViewModel> Entities
        {
            get { return entities; }
            set
            {
                entities = value;
                RaisePropertyChangedEvent("Entities");
            }
        }

        private Track track;
        private Track Track
        {
            get { return track; }
            set
            {
                track = value;
                RaisePropertyChangedEvent("Track");
                RaisePropertyChangedEvent("Release");
                RaisePropertyChangedEvent("ReleaseGroup");
                RaisePropertyChangedEvent("Artist");
            }
        }

        public Artist Artist
        {
            get { return Track.Release.ReleaseGroup.Artist; }
        }

        public ReleaseGroup ReleaseGroup
        {
            get { return Track.Release.ReleaseGroup; }
        }

        public Release Release
        {
            get { return Track.Release; }
        }

        public ICommand Reset { get; private set; }

        internal MediaPlayer Player { get; set; }
    }

    public class MapEntityViewModel : ViewModelBase, IDropTarget
    {
        public MapEntityViewModel(IEntity entity)
        {
            Track = (Track)entity;
            EjectFile = new DelegateCommand(
                (param) => File = null,
                IsFileSelected
                );
        }

        private FileInfo file;
        public FileInfo File
        {
            get { return file; }
            set
            {
                file = value;
                RaisePropertyChangedEvent("File");
            }
        }

        private Track track;
        public Track Track
        {
            get { return track; }
            set
            {
                track = value;
                RaisePropertyChangedEvent("Track");
            }
        }

        public ICommand EjectFile { get; private set; }

        public ICommand Play { get; private set; }

        private bool IsFileSelected(object param)
        {
            return File != null;
        }

        private static bool TryGetFileNameFromDropInfo(IDropInfo info, out FileInfo file)
        {
            try
            {
                DataObject dataObject = info.Data as DataObject;
                string filePath = ((string[])dataObject.GetData(DataFormats.FileDrop)).First();
                file = new FileInfo(filePath);

                if (!file.Extension.ToLower().Contains("mp3"))
                    throw new NotSupportedException("Not supported extension");

                return true;
            }
            catch (Exception e)
            {
                file = null;
                return false;
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            FileInfo file;
            if (TryGetFileNameFromDropInfo(dropInfo, out file))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            FileInfo file;
            TryGetFileNameFromDropInfo(dropInfo, out file);
            File = file;
        }
    }

}