using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.Model
{
    public partial class PlayList : ObservableObject
    {
        public static BackgroundTask CreateFindPlayListTask(string directory)
        {
            var progress_reporter = new Progress<BackgroundTaskProgress>();
            var bg_task = new BackgroundTask()
            {
                Name = $"プレイリスト探索: {directory}",
                Task = new Task(() =>
                {
                    try
                    {
                        var playlists = PlayList.FindPlayListFromDirectory(directory, progress_reporter);
                        foreach (var e in playlists)
                        {
                            var same_source_playlist = StaticResource.PlayLists.FirstOrDefault(p => p.Source is not null && p.Source.Equals(e.Source), null!);
                            if (same_source_playlist is not null)
                            {
                                same_source_playlist.Tracks = e.Tracks;
                                same_source_playlist.Name = e.Name;
                                same_source_playlist.Source = e.Source;
                            } else
                            {
                                StaticResource.PlayLists.Add(e);
                            }
                        }
                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.Succeeded,
                            Progress = 1.0
                        });
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.WriteLine($"Cannot search playlists: {directory}");
                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.Failed,
                            Result = "プレイリストの探索に失敗しました。",
                            IsFailureFound = true,
                            Progress = 1.0
                        });
                    }
                }),
                ProgressReporter = progress_reporter,
                State = BackgroundTaskState.Waiting
            };

            return bg_task;
        }

        public PlayList() { }

        [ObservableProperty]
        public partial IPlayListSource? Source { get; set; } = null;

        [ObservableProperty]
        public partial IList<Track> Tracks { get; set; } = [];

        [ObservableProperty]
        public partial string Name { get; set; } = "New PlayList";

        public static List<PlayList> FindPlayListFromDirectory(string directory, IProgress<BackgroundTaskProgress> progress)
        {
            var list = new List<PlayList>();

            var enum_option = new EnumerationOptions()
            {
                RecurseSubdirectories = true
            };
            var filepathes = Directory.EnumerateFiles(directory, "*.m3u8", enum_option);

            foreach (var filepath in filepathes)
            {
                var src = new FileOriginPlayListSource()
                {
                    FilePath = filepath,
                    Format = new M3UPlayListFormat()
                };
                
                var play_list = src.LoadPlayList(progress);
                if (play_list is not null)
                {
                    list.Add(play_list);
                } else
                {
                    progress.Report(new BackgroundTaskProgress() { Result=$"プレイリストの読み込みに失敗しました: {filepath}", State=BackgroundTaskState.InProgress, IsFailureFound=true });
                }
            }

            return list;
        }
    }
}
