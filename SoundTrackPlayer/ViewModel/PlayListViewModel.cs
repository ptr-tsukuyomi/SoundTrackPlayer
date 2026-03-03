using CommunityToolkit.Mvvm.ComponentModel;
using SoundTrackPlayer.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;


namespace SoundTrackPlayer.ViewModel
{
    public partial class PlayListTrackViewModel : ObservableObject
    {
        private bool _is_playing;

        private PlayList _play_list;
        public PlayListTrackViewModel(Track t, PlayList p, int trackNoInPlayList)
        {
            Track = t;
            _play_list = p;

            PlayButtonCommand = new Command(async () =>
            {
                await StaticResource.Player.Stop();
                StaticResource.Player.Queue.Clear();
                StaticResource.Player.Queue.Append(_play_list.Tracks);
                StaticResource.Player.Queue.SetCurrentTrack(Track);
                await StaticResource.Player.Play();
            });
            IsPlaying = StaticResource.Player.Queue.CurrentTrack == Track;
            TrackNoInPlayList = trackNoInPlayList;
        }

        [ObservableProperty]
        public partial FontAttributes FontAttributes { get; set; } = FontAttributes.None;

        public bool IsPlaying
        {
            get
            {
                return _is_playing;
            }
            set
            {
                _is_playing = value;
                if (_is_playing)
                {
                    FontAttributes = FontAttributes.Bold;
                } else
                {
                    FontAttributes = FontAttributes.None;
                }
            }
        }

        public Track Track { get; set; }

        public Command PlayButtonCommand { get; set; }

        [ObservableProperty]
        public partial int TrackNoInPlayList { get; set; }
    }

    public partial class PlayListViewModel : ObservableObject
    {
        private PlayList _play_list;

        public PlayListViewModel(PlayList play_list)
        {
            _play_list = play_list;
            StaticResource.Player.Queue.CurrentTrackChanged += Queue_CurrentTrackChanged;
            Tracks = new ObservableCollection<PlayListTrackViewModel>(_play_list.Tracks.Select((e, i) => new PlayListTrackViewModel(e, _play_list, i + 1)));
        }

        private void Queue_CurrentTrackChanged(object? sender, Track? e)
        {
            foreach (var track in Tracks)
            {
                var is_playing = track.Track == StaticResource.Player.Queue.CurrentTrack;
                track.IsPlaying = is_playing;
            }
            OnPropertyChanged(nameof(Tracks));
        }

        [ObservableProperty]
        public partial ObservableCollection<PlayListTrackViewModel> Tracks { get; set; }

        public string Name
        {
            get
            {
                return _play_list.Name;
            }
        }
    }
}
