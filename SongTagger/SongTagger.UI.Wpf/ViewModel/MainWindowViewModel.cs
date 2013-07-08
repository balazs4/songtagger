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
            Workspace = new SearchViewmodel(SearchArtistAsync);
            Cart = new CartViewModel();
            WindowTitle = GetType().Namespace.Split('.').FirstOrDefault();
        }

        private void OnPropertyChangedDispatcher(object sender, PropertyChangedEventArgs eventArgs)
        {
            //TODO
        }

        protected void Reset()
        {
            Workspace = new SearchViewmodel(SearchArtistAsync);
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
            Title = "Opps something went wrong...";
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

        private ObservableCollection<EntityViewModel> entities;
        public ObservableCollection<EntityViewModel> Entities
        {
            get { return entities; }
            set
            {
                entities = value;
                RaisePropertyChangedEvent("Entities");
            }
        }

        public ICommand Reset { get; private set; }
    }

    public class CartViewModel : ViewModelBase, IDropTarget
    {
        public void DragOver(IDropInfo dropInfo)
        {
            throw new NotImplementedException();
        }

        public void Drop(IDropInfo dropInfo)
        {
            throw new NotImplementedException();
        }

        private EntityViewModel entity;
        public EntityViewModel Entity
        {
            get { return entity; }
            set
            {
                entity = value;
                RaisePropertyChangedEvent("Entity");
            }
        }
    }
}
