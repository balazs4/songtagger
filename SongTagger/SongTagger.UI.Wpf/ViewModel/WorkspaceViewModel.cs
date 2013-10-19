using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;
using SongTagger.Core;
using SongTagger.UI.Wpf.ViewModel;

namespace SongTagger.UI.Wpf
{
    public class WorkspaceViewModel : ViewModelBase
    {
        public static string GetDescriptionFromEnumValue(Enum value)
        {
            DescriptionAttribute attribute = value.GetType()
                                                  .GetField(value.ToString())
                                                  .GetCustomAttributes(typeof(DescriptionAttribute), false)
                                                  .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public WorkspaceViewModel(State state)
        {
            Header = GetDescriptionFromEnumValue(state);
        }

        private string header;
        public string Header
        {
            get { return header; }
            set
            {
                header = value;
                RaisePropertyChangedEvent("Header");
            }
        }

        private bool isQueryRunning;
        public bool IsQueryRunning
        {
            get { return isQueryRunning; }
            set
            {
                isQueryRunning = value;
                RaisePropertyChangedEvent("IsQueryRunning");
            }
        }
    }

    public class SearchViewmodel : WorkspaceViewModel
    {

        public SearchViewmodel(Action<string> searchCallback)
            : base(State.SearchForAritst)
        {
            Search = new DelegateCommand
                (
                    param => searchCallback(SearchText),
                    param => !String.IsNullOrWhiteSpace(SearchText)
                );

            OpenLink = new DelegateCommand(p =>
                {
                    if (p == null)
                        return;

                    Uri url;
                    if (!Uri.TryCreate(p.ToString(), UriKind.Absolute, out url))
                        return;

                    System.Diagnostics.Process.Start(url.ToString());
                });
        }

        private string searchText;

        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                RaisePropertyChangedEvent("SearchText");
            }
        }

        public ICommand Search { get; private set; }

        public ICommand OpenLink { get; private set; }
    }

    public class MarketViewModel : WorkspaceViewModel
    {
        public MarketViewModel(State state, IEnumerable<EntityViewModel> entities)
            : base(state)
        {
            Entities = new ObservableCollection<EntityViewModel>(entities);
        }

        private string filterText;
        public string FilterText
        {
            get { return filterText; }
            set
            {
                filterText = value;
                RaisePropertyChangedEvent("FilterText");
                RaisePropertyChangedEvent("EntitiesView");
            }
        }

        private ObservableCollection<EntityViewModel> entities;
        public ObservableCollection<EntityViewModel> Entities
        {
            get { return entities; }
            set
            {
                entities = value;
                //RaisePropertyChangedEvent("Entities");
                RaisePropertyChangedEvent("EntitiesView");
            }
        }


        private ICollectionView entitiesView;
        public ICollectionView EntitiesView
        {
            get
            {
                if (entitiesView == null)
                {
                    entitiesView = CollectionViewSource.GetDefaultView(Entities);
                }

                if (String.IsNullOrWhiteSpace(FilterText))
                    entitiesView.Filter = null;
                else
                    entitiesView.Filter = Filter;

                return entitiesView;
            }
        }

        private bool Filter(object parameter)
        {
            if (String.IsNullOrWhiteSpace(FilterText))
                return true;

            string text = FilterText.ToLower();
            EntityViewModel item = (EntityViewModel)parameter;

            if (item.Entity.Name.ToLower().Contains(text))
                return true;

            IEntity entity = item.Entity;

            if (entity is Artist)
            {
                Artist artist = (Artist)entity;
                return PerformFilter(text,
                                     artist.Type.ToString(),
                                     string.Join(" ", artist.Tags.Select(t => t.Name)),
                                     artist.Score.ToString());
            }

            if (entity is ReleaseGroup)
            {
                ReleaseGroup group = (ReleaseGroup)entity;
                return PerformFilter(text, group.PrimaryType.ToString(), group.FirstReleaseDate.Year.ToString());
            }

            return false;
        }

        private static bool PerformFilter(string text, params string[] properties)
        {
            return properties.Any(s => s.ToLower().Contains(text));
        }
    }

    public class VirtualReleaseViewModel : WorkspaceViewModel, IDropTarget
    {
        private Artist artist;
        public Artist Artist
        {
            get { return artist; }
            private set
            {
                artist = value;
                RaisePropertyChangedEvent("Artist");
            }
        }

        private ReleaseGroup releaseGroup;
        public ReleaseGroup ReleaseGroup
        {
            get { return releaseGroup; }
            private set
            {
                releaseGroup = value;
                RaisePropertyChangedEvent("ReleaseGroup");
            }
        }

        private ObservableCollection<CoverArt> covers;
        public ObservableCollection<CoverArt> Covers
        {
            get { return covers; }
            private set
            {
                covers = value;
                RaisePropertyChangedEvent("Covers");
            }
        }

        private CoverArt selectedCover;
        public CoverArt SelectedCover
        {
            get { return selectedCover; }
            set
            {
                selectedCover = value;
                RaisePropertyChangedEvent("SelectedCover");
            }
        }

        private ObservableCollection<Song> songs;
        public ObservableCollection<Song> Songs
        {
            get { return songs; }
            private set
            {
                songs = value;
                RaisePropertyChangedEvent("Songs");
            }
        }

        private bool isFolderDragNDropActive;
        public bool IsFolderDragNDropActive
        {
            get { return isFolderDragNDropActive; }
            set
            {
                isFolderDragNDropActive = value;
                RaisePropertyChangedEvent("IsFolderDragNDropActive");
            }
        }

        private volatile bool IsCoversInitialized;

        public VirtualReleaseViewModel(IEnumerable<Track> tracks, Action<IEnumerable<Uri>,
            Action<CoverArt>, CancellationToken> coverDownloaderService,
            Action<Exception> reportError,
            Action<ReleaseGroup> reportDone)
            : base(State.MapTracks)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            #region Init commands

            AddCoverArtLink = new DelegateCommand(
                p =>
                {
                    Uri uri;
                    if (!Uri.TryCreate(Clipboard.GetText(TextDataFormat.Text), UriKind.Absolute, out uri))
                        return;

                    if (uri == null)
                        return;

                    if (Covers.All(cover => cover.Url == null || cover.Url.ToString() != uri.ToString()))
                    {
                        DownloadAndInitCovers(coverDownloaderService, source.Token, uri);
                    }
                    Clipboard.Clear();
                },
                p =>
                {
                    if (!IsCoversInitialized)
                        return false;
                    try
                    {
                        string data = Clipboard.GetText(TextDataFormat.Text);
                        Uri uri;
                        return Uri.TryCreate(data, UriKind.Absolute, out uri);
                    }
                    catch
                    {
                        return false;
                    }
                });

            CancelCustomCovertArt = new DelegateCommand(p => Clipboard.Clear());

            Save = new DelegateCommand(
                p => TagSongs(reportError, reportDone),
                p =>
                {
                    if (IsQueryRunning)
                        return false;
                    return Songs.Any(song => song.SourceFile != null && song.SourceFile.Exists);
                });
            #endregion

            ReleaseGroup = tracks.First().Release.ReleaseGroup;
            Artist = ReleaseGroup.Artist;

            #region Init cover arts
            Covers = new ObservableCollection<CoverArt>();
            Covers.CollectionChanged += (sender, args) =>
                {
                    if (args.Action != NotifyCollectionChangedAction.Add)
                        return;
                    SelectedCover = (CoverArt)args.NewItems[0];
                };

            var coverUriList = tracks.ToLookup(t => t.Release)
                                           .Where(group => group.Key.HasPreferredCoverArt)
                                           .Select(group => group.Key.GetCoverArt())
                                           .ToArray();
            //TODO: DisCogs,Last.FM
            DownloadAndInitCovers(coverDownloaderService, source.Token, coverUriList);
            #endregion

            Songs = new ObservableCollection<Song>();
            CollectAndInitSongs(tracks);
        }

        private void TagSongs(Action<Exception> reportError, Action<ReleaseGroup> reportDone)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            var initTask = new Task(() =>
                {
                    var song = Songs.First(s => s.SourceFile != null && s.SourceFile.Exists);
                    if (!Directory.Exists(song.TargetFile.DirectoryName))
                        Directory.CreateDirectory(song.TargetFile.DirectoryName);

                }, tokenSource.Token);



            List<Task> taggers = new List<Task>();

            foreach (Song song in Songs.Where(s => s.SourceFile != null && s.SourceFile.Exists))
            {
                var tagger = initTask.ContinueWith(prevTask =>
                   {
                       File.Copy(song.SourceFile.FullName, song.TargetFile.FullName, true);
                       song.SourceFile.Refresh();
                       song.TargetFile.Refresh();
                       Core.Mp3Tag.TagHandler.Save(song.Track, song.TargetFile, SelectedCover.Data);
                       
                   }, tokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);

                taggers.Add(tagger);

                tagger.ContinueWith(prevTask =>
                    {
                        if (File.Exists(song.TargetFile.FullName))
                            File.Delete(song.TargetFile.FullName);
                    }, TaskContinuationOptions.NotOnRanToCompletion);

                tagger.ContinueWith(prevTask =>
                {
                    if (!SongTaggerSettings.Current.KeepOriginalFileAfterTagging)
                    {
                        File.Delete(song.SourceFile.FullName);    
                    }
                    song.EjectSourceFile.Execute(null);
                }, TaskContinuationOptions.OnlyOnRanToCompletion);

            }

            initTask.Start(TaskScheduler.Current);
            IsQueryRunning = true;
            Task.Factory.StartNew(() => Task.WaitAll(taggers.ToArray()))
                .ContinueWith(prevTask =>
                    {
                        IsQueryRunning = false;
                        if (prevTask.IsFaulted)
                        {
                            reportError(prevTask.Exception);
                        }
                        else
                        {
                            reportDone(Songs.First().Track.Release.ReleaseGroup);
                        }
                    });
        }

        private void CollectAndInitSongs(IEnumerable<Track> tracks)
        {
            DirectoryInfo targetDir = new DirectoryInfo(SongTaggerSettings.Current.OutputFolderPath);

            var tracksByRelease = tracks.ToLookup(tr => tr.Release);
            var longestRelease = tracksByRelease.OrderByDescending(r => r.Count()).First();

            foreach (var track in longestRelease.OrderBy(t => t.DiscNumber).ThenBy(t => t.Posititon))
            {
                Songs.Add(new Song(track, Songs.Count + 1, targetDir));
            }

            foreach (var altenative in tracksByRelease.Except(new[] { longestRelease }))
            {
                foreach (var track in altenative.OrderBy(t => t.DiscNumber).ThenBy(t => t.Posititon))
                {
                    if (Songs.Any(song => song.Track.Name.ToLower() == track.Name.ToLower()))
                        continue;

                    Songs.Add(new Song(track, Songs.Count + 1, targetDir));
                }
            }
        }

        public ICommand AddCoverArtLink { get; private set; }
        public ICommand CancelCustomCovertArt { get; private set; }
        public ICommand Save { get; private set; }

        private void DownloadAndInitCovers(Action<IEnumerable<Uri>, Action<CoverArt>, CancellationToken> coverDownloaderService, CancellationToken token, params Uri[] coverUriList)
        {
            IsCoversInitialized = false;
            if (Covers.Any(cover => cover.Data == null))
            {
                Covers.Clear();
            }

            Task download = Task.Factory.StartNew(() => coverDownloaderService(coverUriList, AddToCoverArtCollectionThreadSafety, token), token);

            download.ContinueWith(task =>
                {
                    Action addAction = () =>
                    {
                        if (Covers.Any())
                            return;
                        Covers.Add(CoverArt.CreateCoverArt(null, null));
                    };
                    Application.Current.Dispatcher.BeginInvoke(addAction, DispatcherPriority.Normal);
                    IsCoversInitialized = true;
                }, token);
        }

        private void AddToCoverArtCollectionThreadSafety(CoverArt item)
        {
            if (item == null)
                return;

            if (item.Url == null)
                return;

            if (item.Data == null)
                return;

            if (!item.Data.Any())
                return;

            Action addAction = () => Covers.Insert(0, item);
            Application.Current.Dispatcher.BeginInvoke(addAction, DispatcherPriority.Normal);
        }

        private static bool TryGetDirNameFromDropInfo(IDropInfo info, out DirectoryInfo dir)
        {
            try
            {
                DataObject dataObject = info.Data as DataObject;
                string filePath = ((string[])dataObject.GetData(DataFormats.FileDrop)).First();
                dir = new DirectoryInfo(filePath);

                if (!dir.GetFiles().Any(f => f.Extension.ToLower().EndsWith("mp3")))
                      throw new NotSupportedException("Not supported extension");

                return true;
            }
            catch (Exception e)
            {
                dir = null;
                return false;
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            DirectoryInfo dir;
            if (TryGetDirNameFromDropInfo(dropInfo, out dir))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
                IsFolderDragNDropActive = true;
                Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(3000);
                        if (IsFolderDragNDropActive)
                            IsFolderDragNDropActive = false;
                    });
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            DirectoryInfo dir;
            IsFolderDragNDropActive = false;
            TryGetDirNameFromDropInfo(dropInfo, out dir);
            DetectTracks(dir);
        }

        private void DetectTracks(DirectoryInfo dir)
        {
            var mp3FileList = dir.EnumerateFiles("*.mp3", SearchOption.AllDirectories);

            Parallel.ForEach(mp3FileList, file =>
                {
                    var song = Songs.FirstOrDefault(s => s.SourceFile == null);
                    song.SourceFile = file;

                    //TODO
                    
                });

        }
    }

    public class Song : ViewModelBase, IDropTarget
    {
        public Song(Track track, int position, DirectoryInfo libraryRoot)
        {
            EjectSourceFile = new DelegateCommand(p => SourceFile = null, p => IsInitalized);

#if OFFLINE
            SourceFile = new FileInfo(Path.GetTempFileName());
#endif
            Track = track;
            Position = position;

            TargetFile = CreateTargetFile(libraryRoot);
        }

        private FileInfo CreateTargetFile(DirectoryInfo libraryRoot = null)
        {
            string dirPath;
            if (libraryRoot == null)
                dirPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            else
                dirPath = libraryRoot.FullName;

            var albumSuffix = new[]
                {
                    ReleaseGroupType.EP, ReleaseGroupType.Live, ReleaseGroupType.Single, 
                };

            string artistName = CreateValidName(name => name.ValidDirectoryName(), Track.Release.ReleaseGroup.Artist.Name);
            string albumName = CreateValidName(name => name.ValidDirectoryName(),
                Track.Release.ReleaseGroup.FirstReleaseDate.Year.ToString(),
                Track.Release.ReleaseGroup.Name,
                albumSuffix.Contains(Track.Release.ReleaseGroup.PrimaryType) ? Track.Release.ReleaseGroup.PrimaryType.ToString() : "");
            string fileName = CreateValidName(name => name.ValidFileName(), Position.ToString().PadLeft(2, '0'), Track.Name, "mp3");

            return new FileInfo(Path.Combine(dirPath, artistName, albumName, fileName));
        }

        private static string CreateValidName(Func<string, string> validator, params string[] parts)
        {
            char delimiter = '.';
            return validator(String.Join(delimiter.ToString(), parts).Replace(' ', delimiter).TrimEnd(delimiter).Trim());
        }

        internal FileInfo TargetFile { get; private set; }

        public ICommand EjectSourceFile { get; private set; }

        private int position;
        public int Position
        {
            get { return position; }
            set
            {
                position = value;
                RaisePropertyChangedEvent("Position");
            }
        }

        private Track track;
        public Track Track
        {
            get { return track; }
            set
            {
                track = value;
                RaisePropertyChangedEvent("Track");
            }
        }

        private FileInfo sourceFile;
        public FileInfo SourceFile
        {
            get { return sourceFile; }
            set
            {
                sourceFile = value;
                RaisePropertyChangedEvent("SourceFile");
                RaisePropertyChangedEvent("IsInitalized");
            }
        }


        public bool IsInitalized { get { return sourceFile != null; } }

        private static bool TryGetFileNameFromDropInfo(IDropInfo info, out FileInfo file)
        {
            try
            {
                DataObject dataObject = info.Data as DataObject;
                string filePath = ((string[])dataObject.GetData(DataFormats.FileDrop)).First();
                file = new FileInfo(filePath);

                if (!file.Extension.ToLower().Contains("mp3"))
                    throw new NotSupportedException("Not supported extension");

                return true;
            }
            catch (Exception e)
            {
                file = null;
                return false;
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            FileInfo file;
            if (TryGetFileNameFromDropInfo(dropInfo, out file))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            FileInfo file;
            TryGetFileNameFromDropInfo(dropInfo, out file);
            SourceFile = file;
        }
    }

    public static class StringExtension
    {
        private static string Validate(string text, Func<char[]> invalidChars)
        {
            return invalidChars().Select(c => c.ToString()).Concat(new[] { ":" })
                .Aggregate(text, (current, c) => current.Replace(c, ""));
        }

        public static string ValidFileName(this string text)
        {
            return Validate(text, Path.GetInvalidFileNameChars);
        }

        public static string ValidDirectoryName(this string text)
        {
            return Validate(text, Path.GetInvalidPathChars);
        }
    }
}