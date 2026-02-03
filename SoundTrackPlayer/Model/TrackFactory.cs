using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.Model
{
    internal class TrackFactory
    {
        static public Track LoadFromFile(string file_path, bool ignore_broken_config = false)
        {
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
            track.Info.Title = Path.GetFileName(file_path);
            return track;
        }
    }
}
