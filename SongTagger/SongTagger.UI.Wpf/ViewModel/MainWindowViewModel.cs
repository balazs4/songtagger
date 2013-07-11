using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;
using SongTagger.Core;
using SongTagger.Core.Service;
using SongTagger.UI.Wpf.ViewModel;

namespace SongTagger.UI.Wpf
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

    public enum State
    {
        [Description("Search for artist")]
        SearchForAritst,

        [Description("Select an artist")]
        SelectArtist,

        [Description("Select a release group")]
        SelectReleaseGroup,

        [Description("Select a release")]
        SelectRelease,

        [Description("Pair tracks with mp3s")]
        PairTracks
    }

    public class MainWindowViewModel : ViewModelBase
    {
        protected readonly IProvider provider;

        public MainWindowViewModel(IProvider dataProvider)
        {
            if (dataProvider == null)
                throw new ArgumentNullException("No data provider");

            PropertyChanged += OnPropertyChangedDispatcher;
            provider = dataProvider;

            Reset();

            WindowTitle = GetType().Namespace.Split('.').FirstOrDefault();
        }

        private void OnPropertyChangedDispatcher(object sender, PropertyChangedEventArgs eventArgs)
        {
            //TODO
        }

        protected void Reset()
        {
            Workspace = new SearchViewmodel(SearchArtistAsync);
            Cart = null;
        }

        private static void CreateAndStartTask<T>(Func<T> action, Action<Task<T>> done, Action<Task<T>> error)
        {
            Task<T> task = Task<T>.Factory.StartNew(action);
            task.ContinueWith(error, TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(done, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        


        private void SearchArtistAsync(string searchText)
        {
            CreateAndStartTask(
                () =>
                {
                    Workspace.IsQueryRunning = true;
                    return provider.SearchArtist(searchText);
                },
                action =>
                {
                    Workspace = new MarketViewModel(State.SelectArtist, action.Result.Select(a => new EntityViewModel(a)), Reset);
                    Cart = new CartViewModel(LoadEntitiesAsync);
                },
                action =>
                {
                    Workspace.IsQueryRunning = false;
                    //TODO: get the first not AggregateException
                    ShowErrorMessage(action.Exception.InnerException);
                }
                );
        }

        protected void LoadEntitiesAsync(EntityViewModel entityViewModel)
        {
            if (entityViewModel == null)
                return;

            IEntity sourceEntity = entityViewModel.Entity;

            if (sourceEntity is Artist)
            {
                LoadReleaseGroupsAsync((Artist) sourceEntity);
                return;
            }

            if (sourceEntity is ReleaseGroup)
            {
                return;
            }

            if (sourceEntity is Release)
            {
                return;
            }
        }

        private void LoadReleaseGroupsAsync(Artist artist)
        {
            CreateAndStartTask(
                () =>
            {
                Workspace.IsQueryRunning = true;
                return provider.BrowseReleaseGroups(artist);
            },
            action =>
            {
                Workspace = new MarketViewModel(State.SelectReleaseGroup, action.Result.Select(a => new EntityViewModel(a)), Reset);
            },
            action =>
            {
                Workspace.IsQueryRunning = false;
                ShowErrorMessage(action.Exception.InnerException);
            }
            );
            
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

        private WorkspaceViewModel workspace;
        public WorkspaceViewModel Workspace
        {
            get { return workspace; }
            set
            {
                workspace = value;
                RaisePropertyChangedEvent("Workspace");
            }
        }

        private CartViewModel cart;
        public CartViewModel Cart
        {
            get { return cart; }
            set
            {
                cart = value;
                RaisePropertyChangedEvent("Cart");
            }
        }

        private ErrorViewModel error;
        public ErrorViewModel Error
        {
            get { return error; }
            set
            {
                error = value;
                RaisePropertyChangedEvent("Error");
            }
        }

        private void CloseErrorMessage()
        {
            Error = null;
        }

        protected void ShowErrorMessage(Exception exception, params ErrorActionViewModel[] actions)
        {
            if (actions == null || !actions.Any())
                Error = new ErrorViewModel(exception, new ErrorActionViewModel("Ok", CloseErrorMessage));
            else
                Error = new ErrorViewModel(exception, actions);
        }
    }

    public class ErrorViewModel : ViewModelBase
    {
        public ErrorViewModel(Exception exception, params ErrorActionViewModel[] actions)
        {
            if (actions == null)
                Actions = new ObservableCollection<ErrorActionViewModel>();
            else
                Actions = new ObservableCollection<ErrorActionViewModel>(actions.ToList());

            Exception = exception;
            Title = "Oops something went wrong...";
        }

        private Exception exception;
        public Exception Exception
        {
            get { return exception; }
            set
            {
                exception = value;
                RaisePropertyChangedEvent("Exception");
            }
        }

        private ObservableCollection<ErrorActionViewModel> actions;
        public ObservableCollection<ErrorActionViewModel> Actions
        {
            get { return actions; }
            set
            {
                actions = value;
                RaisePropertyChangedEvent("Actions");
            }
        }

        private string title;
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                RaisePropertyChangedEvent("Title");
            }
        }
    }

    public class ErrorActionViewModel : ViewModelBase
    {
        private string caption;
        public string Caption
        {
            get { return caption; }
            set
            {
                caption = value;
                RaisePropertyChangedEvent("Caption");
            }
        }

        public ErrorActionViewModel(string title, Action action)
        {
            Caption = title;
            HandlerCommand = new DelegateCommand(param => action());
        }

        public ICommand HandlerCommand { get; private set; }
    }

    public class EntityViewModel : ViewModelBase
    {
        #region Colors
        private static Dictionary<Type, Color> colors = new Dictionary<Type, Color>
            {
                {typeof(Artist), Color.LightGreen},
                {typeof(ReleaseGroup), Color.Orange},
                {typeof(Release), Color.Red},
                {typeof(Track), Color.LightBlue}
            };

        private static Color fallBackColor = Color.Purple;

        private static Color GetColor(Type type)
        {
            if (colors.ContainsKey(type))
                return colors[type];
            else
                return fallBackColor;
        }
        #endregion

        public EntityViewModel(IEntity entity)
        {
            PropertyChanged += OnPropertyChangedDispatcher;
            Entity = entity;
        }

        private void OnPropertyChangedDispatcher(object sender, PropertyChangedEventArgs eventArgs)
        {
            switch (eventArgs.PropertyName)
            {
                case "Entity":
                    EntityColor = GetColor(Entity.GetType());
                    break;

                case "EntityColor":
                default:
                    return;
            }
        }


        private Color entityColor;
        public Color EntityColor
        {
            get { return entityColor; }
            set
            {
                entityColor = value;
                RaisePropertyChangedEvent("EntityColor");
            }
        }

        private IEntity entity;
        public IEntity Entity
        {
            get { return entity; }
            set
            {
                entity = value;
                RaisePropertyChangedEvent("Entity");
            }
        }
    }

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
                RaisePropertyChangedEvent("Entities");
                RaisePropertyChangedEvent("EntitiesView");
            }
        }

        public ICollectionView EntitiesView
        {
            get
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(Entities);
                if (String.IsNullOrWhiteSpace(FilterText))
                    view.Filter = null;
                else
                    view.Filter = Filter;    
                return view;
            }
        }

        private bool Filter(object parameter)
        {
            if (String.IsNullOrWhiteSpace(FilterText))
                return true;

            string text = FilterText.ToLower();
            EntityViewModel item = (EntityViewModel) parameter;

            if (item.Entity.Name.ToLower().Contains(text))
                return true;

            IEntity entity = item.Entity;

            if (entity is Artist)
            {
                Artist artist = (Artist) entity;
                return PerformFilter(text, 
                                     artist.Type.ToString(), 
                                     string.Join(" ", artist.Tags.Select(t => t.Name)),
                                     artist.Score.ToString());
            }

            return false;
        }

        private static bool PerformFilter(string text, params string[] properties)
        {
            return properties.Any(s => s.ToLower().Contains(text));
        }

        public ICommand Reset { get; private set; }

    }

    public class CartViewModel : ViewModelBase, IDropTarget
    {
        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is EntityViewModel)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }            
        }

        public void Drop(IDropInfo dropInfo)
        {
            EntityItem = (EntityViewModel)dropInfo.Data;
        }


        private Action<EntityViewModel> loadSubEntities;
        public CartViewModel(Action<EntityViewModel> entityChangedCallback)
        {
            loadSubEntities = entityChangedCallback;
            PropertyChanged += OnPropertyChangedDispatcher;
            Collection = new ObservableCollection<EntityViewModel>();
        }

        private void OnPropertyChangedDispatcher(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == "EntityItem")
            {
                FillCollection(Collection, EntityItem);
                loadSubEntities(EntityItem);
                return;
            }
        }

        private void FillCollection(ObservableCollection<EntityViewModel> list , EntityViewModel selectedViewModel)
        {
            list.Clear();
            IEntity currentEntity = selectedViewModel.Entity;

            if (currentEntity is Artist)
            {
                Artist item = (Artist) currentEntity;
                list.Add(new EntityViewModel(item));
            }

            if (currentEntity is ReleaseGroup)
            {
                ReleaseGroup item = (ReleaseGroup) currentEntity;
                list.Add(new EntityViewModel(item.Artist));
                list.Add(new EntityViewModel(item));
            }

            if (currentEntity is Release)
            {
                Release item = (Release) currentEntity;
                list.Add(new EntityViewModel(item.ReleaseGroup.Artist));
                list.Add(new EntityViewModel(item.ReleaseGroup));
                list.Add(new EntityViewModel(item));
            
            }
        }

        private ObservableCollection<EntityViewModel> collection;
        public ObservableCollection<EntityViewModel> Collection
        {
            get { return collection; }
            set
            {
                collection = value;
                RaisePropertyChangedEvent("Collection");
            }
        }

        private EntityViewModel entity;
        internal EntityViewModel EntityItem
        {
            private get { return entity; }
            set
            {
                entity = value;
                RaisePropertyChangedEvent("EntityItem");
            }
        }
    }
}
