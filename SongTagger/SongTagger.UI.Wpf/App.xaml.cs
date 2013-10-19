using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SongTagger.UI.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
            timer.Start();
            timer.Tick += (sender, args) => CommandManager.InvalidateRequerySuggested();

            Trace.Listeners.Add(new ConsoleTraceListener(true));
        }
    }
}
