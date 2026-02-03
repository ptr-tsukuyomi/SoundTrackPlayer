using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.Model
{
    public static class StaticResource
    {
        public static Player Player { get; set; } = new();

        public static ObservableCollection<PlayList> PlayLists { get; set; } = [];

        public static BackgroundTaskRunner BackgroundTaskRunner { get; set; } = new();
    }
}
