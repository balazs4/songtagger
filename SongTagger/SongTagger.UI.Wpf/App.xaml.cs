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
using System.Xml.Serialization;

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
        public static SongTaggerSettings Current { get { return instance ?? (instance = LoadOrDefault()); } }

        private static readonly string userConfig =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SongTagger.xml");


        private static SongTaggerSettings LoadOrDefault()
        {
            try
            {
                using (var fileStream = File.Open(userConfig, FileMode.Open))
                {
                    return (SongTaggerSettings) new XmlSerializer(typeof(SongTaggerSettings)).Deserialize(fileStream);
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Could not load settings. Details: " + e.ToString());
                return new SongTaggerSettings();
            }
        }

        public void Save()
        {
            try
            {
                using (var fileStream = File.Open(userConfig, FileMode.OpenOrCreate))
                {
                    new XmlSerializer(typeof(SongTaggerSettings)).Serialize(fileStream, this);
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Could not save settings. Details: " + e.ToString());
            }
        }

        private static readonly string songtaggeLastFmApiKey = "f935072042e3375d19bebcb0ea0cd972";

        private SongTaggerSettings()
        {
            OutputFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            KeepOriginalFileAfterTagging = true;
            LastFmApiKey = songtaggeLastFmApiKey;
        }

        private string outputFolderPath;
        
        [XmlElement]
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

        [XmlElement]
        public bool KeepOriginalFileAfterTagging
        {
            get { return keepOriginalFileAfterTagging; }
            set
            {
                keepOriginalFileAfterTagging = value;
                RaisePropertyChangedEvent("KeepOriginalFileAfterTagging");
            }
        }

        private string lastFmApiKey;

        [XmlElement]
        public string LastFmApiKey
        {
            get { return lastFmApiKey; }
            set
            {
                lastFmApiKey = value;
                RaisePropertyChangedEvent("LastFmApiKey");
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
            instance = new SongTaggerSettings();
        }

        public void SetLastFmApiKey(bool set)
        {
            LastFmApiKey = set ? songtaggeLastFmApiKey : string.Empty;
        }
    }
}
