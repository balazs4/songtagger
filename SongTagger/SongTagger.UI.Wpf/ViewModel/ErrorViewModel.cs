using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SongTagger.UI.Wpf.ViewModel;

namespace SongTagger.UI.Wpf
{
    public class ErrorViewModel : ViewModelBase
    {
        public ErrorViewModel(Exception exception, params ErrorActionViewModel[] actions)
        {
            if (actions == null)
                Actions = new ObservableCollection<ErrorActionViewModel>();
            else
                Actions = new ObservableCollection<ErrorActionViewModel>(actions.ToList());

            Exception = exception;
            Title = "Oops something went wrong...";
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
}