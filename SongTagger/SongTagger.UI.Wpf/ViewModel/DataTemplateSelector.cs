﻿using System.Windows;
using System.Windows.Controls;
using SongTagger.Core;

namespace SongTagger.UI.Wpf
{
    public class WorkspaceDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SearchArtistDataTemplate { get; set; }
        public DataTemplate MarketDataTemplate { get; set; }
        public DataTemplate VirtualReleaseDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SearchViewmodel)
                return SearchArtistDataTemplate;

            if (item is MarketViewModel)
                return MarketDataTemplate;

            if (item is VirtualReleaseViewModel)
                return VirtualReleaseDataTemplate;

            return base.SelectTemplate(item, container);
        }
    }

    public class EntityDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ArtistDataTemplate { get; set; }
        public DataTemplate ReleaseGroupDataTemplate { get; set; }
        public DataTemplate ReleaseDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Artist)
                return ArtistDataTemplate;

            if (item is ReleaseGroup)
                return ReleaseGroupDataTemplate;

            if (item is Release)
                return ReleaseDataTemplate;

            return base.SelectTemplate(item, container);
        }
    }

    public class CartDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NonVisibleTemplate { get; set; }
        public DataTemplate DefaultDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            CartViewModel viewModel = item as CartViewModel;

            if (viewModel == null)
                return NonVisibleTemplate;

            return DefaultDataTemplate;
        }
    }

    public class SettingDataTempalteSelector : DataTemplateSelector
    {
        public DataTemplate PathTemplate { get; set; }
        public DataTemplate SwitchTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PathSettingViewModel)
                return PathTemplate;

            if (item is SwitchSettingViewModel)
                return SwitchTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}