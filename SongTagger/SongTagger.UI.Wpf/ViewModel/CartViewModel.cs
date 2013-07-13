using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using SongTagger.Core;

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