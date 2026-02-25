
namespace SoundTrackPlayer.Model
{
    internal class TrackFactory
    {
        private static Dictionary<string, Track> FileOriginTracks = new();


        static private TrackInfo LoadTrackInfo(string file_path)
        {
            var result = TagLibSharp2.Core.MediaFile.Read(file_path);
            var info = new TrackInfo();
            info.Title = Path.GetFileName(file_path);

            if (result.IsSuccess && result.Tag is not null)
            {
                if (result.Tag.Title is string s)
                {
                    info.Title = s;
                }
                if (result.Tag.Track is uint t)
                {
                    info.No = t;
                }
            }

            return info;
        }

        static public Track LoadFromFile(string file_path, bool ignore_broken_config = false)
        {
            if (FileOriginTracks.TryGetValue(file_path, out var e))
            {
                return e;
            }

            var source = new FileOriginTrackSource()
            {
                FilePath = file_path
            };

            TrackConfig? config = null;
            try
            {
                config = source.LoadTrackConfig();
            }
            catch (Exception)
            {
                if (!ignore_broken_config) throw;
            }

            var track = new Track
            {
                Source = source,
                Config = config ?? new TrackConfig()
            };

            track.Info = LoadTrackInfo(file_path);

            FileOriginTracks.Add(file_path, track);
            return track;
        }
    }
}
