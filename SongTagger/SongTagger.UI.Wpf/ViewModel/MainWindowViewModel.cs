using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.IO;

namespace SongTagger.UI.Wpf.ViewModel
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
        public MainWindowViewModel()
        {
            StatusCollection = new ObservableCollection<string>();
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

        private ObservableCollection<string> statusCollection;
        public ObservableCollection<string> StatusCollection
        {
            get { return statusCollection; }
            set
            {
                statusCollection = value;
                RaisePropertyChangedEvent("StatusCollection");
            }
        }

    }

}
