using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
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

    public class SongTaggerSettings : INotifyPropertyChanged
    {
        private static SongTaggerSettings instance;
        public static SongTaggerSettings Current { get { return instance ?? (instance = new SongTaggerSettings()); } }

        private SongTaggerSettings()
        {
            OutputFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            KeepOriginalFileAfterTagging = true;
        }

        private string outputFolderPath;
        public string OutputFolderPath
        {
            get { return outputFolderPath; }
            set
            {
                outputFolderPath = value;
                RaisePropertyChangedEvent("OutputFolderPath");
            }
        }

        private bool keepOriginalFileAfterTagging;
        public bool KeepOriginalFileAfterTagging
        {
            get { return keepOriginalFileAfterTagging; }
            set
            {
                keepOriginalFileAfterTagging = value;
                RaisePropertyChangedEvent("KeepOriginalFileAfterTagging");
            }
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(OutputFolderPath))
                return false;

            if (!Directory.Exists(OutputFolderPath))
                return false;

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public static void Reset()
        {
            instance = null;
        }
    }
}
