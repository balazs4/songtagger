using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SongTagger.UI.Wpf.ViewModel
{
    public class DelegateCommand : ICommand
    {
        public Action<object> CommandAction { get; private set; }
        public Predicate<object> CanExecuteCommandAction { get; private set; }

        private void RaiseCanExecuteChangedEvent()
        {
            if (CanExecuteChanged == null)
                return;

            CanExecuteChanged(this, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            if (CommandAction == null)
                return;

            CommandAction(parameter);
        }

        public bool CanExecute(object parameter)
        {
            if (CanExecuteCommandAction == null)
                return true;

            bool canexecute = CanExecuteCommandAction(parameter);
            RaiseCanExecuteChangedEvent();
            return canexecute;
        }

        public event EventHandler CanExecuteChanged;
    }
}
