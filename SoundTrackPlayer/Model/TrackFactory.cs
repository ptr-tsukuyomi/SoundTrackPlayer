
namespace SoundTrackPlayer.Model
{
    internal class TrackFactory
    {
        private static Dictionary<string, Track> FileOriginTracks = new();
        private static Dictionary<string, AlbumImage> DirectoryAlbumImage = new();

        static public Track LoadFromFile(string file_path, bool ignore_broken_config = false)
        {
            if (FileOriginTracks.TryGetValue(file_path, out var t))
            {
                return t;
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

            track.LoadMetadata();

            // load album image
            if (track.Info.AlbumImage is null)
            {
                var parent_dir = Path.GetDirectoryName(file_path);
                if (parent_dir is not null)
                {
                    if (DirectoryAlbumImage.TryGetValue(parent_dir, out var album_image))
                    {
                        track.Info.AlbumImage = album_image;
                    }
                    else
                    {
                        var album_image_path_candidates = new string[]
                        {
                            Path.Combine(parent_dir, "folder.jpg"),
                            Path.Combine(parent_dir, "cover.jpg"),
                            Path.Combine(parent_dir, "album.jpg"),
                        };
                        foreach (var e in album_image_path_candidates)
                        {
                            if (File.Exists(e))
                            {
                                var image = new AlbumImage(e);
                                track.Info.AlbumImage = image;
                                DirectoryAlbumImage.Add(parent_dir, image);
                                break;
                            }
                        }
                    }
                }
            }

            FileOriginTracks.Add(file_path, track);
            return track;
        }
    }
}
