using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;
using SongTagger.Core;
using SongTagger.Core.Service;

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

    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IProvider provider;

        public MainWindowViewModel(IProvider dataProvider)
        {
            provider = dataProvider;
            WindowTitle = GetType().Namespace.ToString().Split('.').FirstOrDefault();
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
    }
}
