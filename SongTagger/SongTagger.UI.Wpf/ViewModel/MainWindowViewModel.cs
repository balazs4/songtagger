using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
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

        [Description("Select an album")]
        SelectReleaseGroup,

        [Description("Select a release")]
        SelectRelease,

        [Description("Map tracks")]
        MapTracks
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private static object lockObject = new object();
        protected readonly IProvider provider;

        public MainWindowViewModel(IProvider dataProvider)
        {
            if (dataProvider == null)
                throw new ArgumentNullException("No data provider");

            PropertyChanged += OnPropertyChangedDispatcher;
            provider = dataProvider;

            Workspace = new SearchViewmodel(SearchArtistAsync);
            Cart = null;

            WindowTitle = GetType().Namespace.Split('.').FirstOrDefault();
        }

        private void OnPropertyChangedDispatcher(object sender, PropertyChangedEventArgs eventArgs)
        {
            //TODO
        }

        

        private void StartMarketQueryTask<T>(Func<T> action, State nextWorkspaceState)
            where T : IEnumerable<IEntity>
        {
            Action<Task<T>> doneTask = task1 =>
                {
                    Workspace = new MarketViewModel(nextWorkspaceState, task1.Result.Select(a => new EntityViewModel(a)));
                    if (Cart == null)
                        Cart = new CartViewModel(LoadEntitiesAsync, ResetToSearchArtist);
                };

            StartQueryTask(action, doneTask);
        }

        private void StartQueryTask<T>(Func<T> action, Action<Task<T>> doneTask)
             where T : IEnumerable<IEntity>
        {

            Action<Task<T>> errorTask = task1 =>
            {
                Workspace.IsQueryRunning = false;
                ShowErrorMessage(task1.Exception.InnerException);
            };

            Workspace.IsQueryRunning = true;
            Task<T> task = Task<T>.Factory.StartNew(action);
            task.ContinueWith(errorTask, TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(doneTask, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        protected void LoadEntitiesAsync(EntityViewModel entityViewModel)
        {
            if (entityViewModel == null)
                return;

            IEntity sourceEntity = entityViewModel.Entity;

            if (sourceEntity is Artist)
            {
                StartMarketQueryTask(() => provider.BrowseReleaseGroups((Artist)sourceEntity), State.SelectReleaseGroup);
                return;
            }

            if (sourceEntity is ReleaseGroup)
            {
                Func<IEnumerable<Track>> action = () =>
                    {
                        IEnumerable<Release> releases = provider.BrowseReleases((ReleaseGroup)sourceEntity);
                        List<Track> tracks = new List<Track>();
                        foreach (Release release in releases)
                        {
                            tracks.AddRange(provider.LookupTracks(release));
                        }
                        return tracks;
                    };

                Action<Task<IEnumerable<Track>>> doneTask = task1 =>
                {
                    Workspace = new VirtualReleaseViewModel(task1.Result, provider.DownloadCoverArts, exception => ShowErrorMessage(exception));
                    if (Cart == null)
                        Cart = new CartViewModel(LoadEntitiesAsync, ResetToSearchArtist);
                };

                StartQueryTask(action, doneTask);

                return;
            }
        }

        protected void ResetToSearchArtist()
        {
            Workspace = new SearchViewmodel(SearchArtistAsync);
            Cart = null;
        }

        private void SearchArtistAsync(string searchText)
        {
            StartMarketQueryTask(() => provider.SearchArtist(searchText), State.SelectArtist);
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

    public class EntityViewModel : ViewModelBase
    {
        #region Colors
        private static Dictionary<Type, Tuple<Color, string>> styles = new Dictionary<Type, Tuple<Color, string>>
            {
                {typeof(Artist), Tuple.Create(Color.CornflowerBlue, "artist.person.png")},
                {typeof(ReleaseGroup), Tuple.Create(Color.Orange,"disc.png")},
                {typeof(Release), Tuple.Create(Color.Gray,"")},
                {typeof(Track), Tuple.Create(Color.MediumPurple,"")}
            };

        private static Tuple<Color, string> fallback = Tuple.Create(Color.Purple, "play.png");

        private static Tuple<Color, string> GetStyle(Type type)
        {
            if (styles.ContainsKey(type))
                return styles[type];
            else
                return fallback;
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
                    var entityStyle = GetStyle(Entity.GetType());
                    EntityColor = entityStyle.Item1;
                    EntityImage = Path.Combine("Images", entityStyle.Item2);
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

        private string entityImage;
        public string EntityImage
        {
            get { return entityImage; }
            set
            {
                entityImage = value;
                RaisePropertyChangedEvent("EntityImage");
            }
        }
    }
}
