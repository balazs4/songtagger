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

            //Reset();
            Workspace = new SearchViewmodel(SearchArtistAsync);
            Cart = null;

            WindowTitle = GetType().Namespace.Split('.').FirstOrDefault();
        }

        private void OnPropertyChangedDispatcher(object sender, PropertyChangedEventArgs eventArgs)
        {
            //TODO
        }

        protected void Reset()
        {
            if (Cart == null || Cart.Collection.Count <= 1)
            {
                Workspace = new SearchViewmodel(SearchArtistAsync);
                Cart = null;
                return;
            }
            Cart.Collection.Remove(Cart.Collection.Last());
            Cart.EntityItem = Cart.Collection.LastOrDefault();
        }

        private void CreateAndStartQueryTask<T>(Func<T> action, State nextWorkspaceState)
            where T : IEnumerable<IEntity>
        {

            Action<Task<T>> doneTask = task1 =>
                {
                    Workspace = new MarketViewModel(nextWorkspaceState, task1.Result.Select(a => new EntityViewModel(a)), Reset);
                    if (Cart == null)
                        Cart = new CartViewModel(LoadEntitiesAsync);
                };

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
                CreateAndStartQueryTask(() => provider.BrowseReleaseGroups((Artist)sourceEntity), State.SelectReleaseGroup);
                return;
            }

            if (sourceEntity is ReleaseGroup)
            {
                CreateAndStartQueryTask(() => provider.BrowseReleases((ReleaseGroup)sourceEntity), State.SelectRelease);
                return;
            }

            if (sourceEntity is Release)
            {
                CreateAndStartQueryTask(() => provider.LookupTracks((Release)sourceEntity), State.MapTracks);
                return;
            }
        }

        private void SearchArtistAsync(string searchText)
        {
            CreateAndStartQueryTask(() => provider.SearchArtist(searchText), State.SelectArtist);
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
                {typeof(Artist), Color.CornflowerBlue},
                {typeof(ReleaseGroup), Color.Orange},
                {typeof(Release), Color.Gray},
                {typeof(Track), Color.MediumPurple}
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
