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

        [Description("Select a release group")]
        SelectReleaseGroup,

        [Description("Select a release")]
        SelectRelease,

        [Description("Maptracks with mp3s")]
        MapTracks
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
}
