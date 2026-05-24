using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.Model
{
    public interface IPlayListFormat
    {
        public abstract IEnumerable<string> GetTrackPathes();
        public abstract string? GetPlayListName();
        public abstract string CreateContent(IEnumerable<string> track_pathes, string play_list_name);

        public string Content { get; set; }
    }

    public class M3UPlayListFormat : IPlayListFormat
    {
        private static readonly string[] separator = ["\r\n", "\n"];

        public string Content { get; set; } = string.Empty;

        public string CreateContent(IEnumerable<string> track_pathes, string play_list_name)
        {
            Content = string.Join(
                "\r\n",
                track_pathes);
            return Content;
        }

        public IEnumerable<string> GetTrackPathes()
        {
            return Content.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.Trim().StartsWith('#'));
        }

        public string? GetPlayListName()
        {
            return null;
        }
    }

    public interface IPlayListSource : IEquatable<IPlayListSource>
    {
        public abstract void SavePlayList(PlayList play_list);
        public abstract PlayList? LoadPlayList(IProgress<BackgroundTaskProgress> progress);
        //public abstract string? GetBasePath();
    }

    public class FileOriginPlayListSource : IPlayListSource, IEquatable<IPlayListSource>
    {
        public string FilePath { get; set; } = string.Empty;
        public IPlayListFormat? Format { get; set; } = null;

        public bool Equals(IPlayListSource? other)
        {
            if (other is null) return false;
            if (other is not FileOriginPlayListSource) return false;
            if (ReferenceEquals(this, other)) return true;

            return FilePath == ((FileOriginPlayListSource)other).FilePath;
        }

        public string? GetBasePath()
        {
            return Directory.GetParent(FilePath)?.FullName;
        }

        public PlayList? LoadPlayList(IProgress<BackgroundTaskProgress> progress)
        {
            progress.Report(new BackgroundTaskProgress() { Result = $"プレイリストロード開始: {FilePath}", State = BackgroundTaskState.InProgress });
            if (Format is null) return null;

            try
            {
                var file_content = File.ReadAllText(FilePath);
                Format.Content = file_content;
            }
            catch (Exception)
            {
                return null;
            }

            var play_list = new PlayList()
            {
                Source = this,
                Name = Path.GetFileNameWithoutExtension(FilePath),
                Tracks = new System.Collections.ObjectModel.ObservableCollection<Track>(Format.GetTrackPathes()
                    .Select(path => {
                        var fullpath = Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(GetBasePath() ?? string.Empty, path));
                        try
                        {
                            return TrackFactory.LoadFromFile(fullpath, false);
                        }
                        catch (Exception)
                        {
                            progress.Report(new BackgroundTaskProgress() { Result= $"トラック設定の読み込みに失敗しました: {fullpath}", State = BackgroundTaskState.InProgress, IsFailureFound=true });
                            return TrackFactory.LoadFromFile(fullpath, true);
                        }
                        }))
            };
            progress.Report(new BackgroundTaskProgress() { Result = $"プレイリストロード完了: {FilePath}", State = BackgroundTaskState.InProgress });

            return play_list;
        }

        public void SavePlayList(PlayList play_list)
        {
            if (Format is null) throw new Exception();

            var track_pathes = play_list.Tracks
                .Select(track => track.Source switch
                {
                    FileOriginTrackSource fsrc => fsrc.FilePath,
                    _ => throw new NotImplementedException()
                })
                .Select(path => Path.GetRelativePath(GetBasePath() ?? string.Empty, path));

            var file_content = Format.CreateContent(track_pathes, play_list.Name);
            File.WriteAllText(FilePath, file_content);
        }
    }
}
