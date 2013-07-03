using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SongTagger.UI.Wpf.ViewModel
{
    public class DelegateCommand : ICommand
    {
        public DelegateCommand(Action<object> action) : 
            this (action, null)
        {
            
        }

        public DelegateCommand(Action<object> action, Predicate<object> canExecute )
        {
            if (action == null)
                throw new ArgumentNullException("action");
            CommandAction = action;
            CanExecuteCommandAction = canExecute;
        }

        public Action<object> CommandAction { get; private set; }

        public Predicate<object> CanExecuteCommandAction { get; private set; }

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
            return canexecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
