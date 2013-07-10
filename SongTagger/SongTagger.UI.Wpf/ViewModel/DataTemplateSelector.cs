using System.Windows;
using System.Windows.Controls;
using SongTagger.Core;

namespace SongTagger.UI.Wpf
{
    public class WorkspaceDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SearchArtistDataTemplate { get; set; }
        public DataTemplate MarketDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SearchViewmodel)
                return SearchArtistDataTemplate;

            if (item is MarketViewModel)
                return MarketDataTemplate;

            return base.SelectTemplate(item, container);
        }
    }

    public class EntityDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ArtistDataTemplate { get; set; }


        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Artist)
                return ArtistDataTemplate;


            return base.SelectTemplate(item, container);
        }
    }

    public class CartDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NonVisibleTemplate { get; set; }
        public DataTemplate EmptyDataTemplate { get; set; }
        public DataTemplate DefaultDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            CartViewModel viewModel = item as CartViewModel;

            if (viewModel == null)
                return NonVisibleTemplate;

            if (viewModel.Collection.Count == 0)
                return EmptyDataTemplate;

            return DefaultDataTemplate;
        }
    }
}