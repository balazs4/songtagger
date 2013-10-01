using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using SongTagger.Core;
using SongTagger.UI.Wpf.ViewModel;

namespace SongTagger.UI.Wpf
{
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
        public CartViewModel(Action<EntityViewModel> entityChangedCallback, Action reset)
        {
            loadSubEntities = entityChangedCallback;
            PropertyChanged += OnPropertyChangedDispatcher;
            Collection = new ObservableCollection<EntityViewModel>();
            Remove = new DelegateCommand(
                p =>
                    {
                        Collection.Remove(Collection.Last());
                        EntityItem = Collection.LastOrDefault();

                        if (Collection.Count == 0)
                            reset();
                    },
                p => Collection.Any());
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

        private void FillCollection(ObservableCollection<EntityViewModel> list, EntityViewModel selectedViewModel)
        {
            list.Clear();

            if (selectedViewModel == null)
                return;

            IEntity currentEntity = selectedViewModel.Entity;

            if (currentEntity is Artist)
            {
                Artist item = (Artist)currentEntity;
                list.Add(new EntityViewModel(item));
            }

            if (currentEntity is ReleaseGroup)
            {
                ReleaseGroup item = (ReleaseGroup)currentEntity;
                list.Add(new EntityViewModel(item.Artist));
                list.Add(new EntityViewModel(item));
            }

            if (currentEntity is Release)
            {
                Release item = (Release)currentEntity;
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
            get { return entity; }
            set
            {
                entity = value;
                RaisePropertyChangedEvent("EntityItem");
            }
        }

        public ICommand Remove { get; private set; }
    }
}