//using Microsoft.UI.Xaml.Documents;
using SoundTrackPlayer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
//using Windows.Media.Playlists;

namespace SoundTrackPlayer.ViewModel
{
    public class PlayListTrackViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _is_playing;

        private PlayList _play_list;
        public PlayListTrackViewModel(Track t, PlayList p)
        {
            Track = t;
            _play_list = p;

            PlayButtonCommand = new Command(async () =>
            {
                await StaticResource.Player.Stop();
                StaticResource.Player.Queue.Clear();
                //foreach (var e in _play_list.Tracks)
                //{
                //    StaticResource.Player.Queue.Append(e);
                //}
                StaticResource.Player.Queue.Append(_play_list.Tracks);
                StaticResource.Player.Queue.SetCurrentTrack(Track);
                await StaticResource.Player.Play();
            });
            IsPlaying = StaticResource.Player.Queue.CurrentTrack == Track;
        }

        public FontAttributes FontAttributes
        { 
            get
            {
                return _font_attributes;
            }
            set
            {
                _font_attributes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontAttributes)));
            }
        }
        private FontAttributes _font_attributes = FontAttributes.None;

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
    }

    public class PlayListViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private PlayList _play_list;

        public PlayListViewModel(PlayList play_list)
        {
            _play_list = play_list;
            //StaticResource.Player.Queue.QueueChanged += Queue_QueueChanged;
            StaticResource.Player.Queue.CurrentTrackChanged += Queue_CurrentTrackChanged;
            _tracks = new ObservableCollection<PlayListTrackViewModel>(_play_list.Tracks.Select((e) => new PlayListTrackViewModel(e, _play_list)));
        }

        private void Queue_CurrentTrackChanged(object? sender, Track? e)
        {
            foreach (var track in Tracks)
            {
                var is_playing = track.Track == StaticResource.Player.Queue.CurrentTrack;
                track.IsPlaying = is_playing;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tracks)));
        }

        //private void Queue_QueueChanged(object? sender, EventArgs e)
        //{
        //    Tracks = new ObservableCollection<PlayListTrackViewModel>(_play_list.Tracks.Select((e) => new PlayListTrackViewModel(e, _play_list)));
        //}

        public ObservableCollection<PlayListTrackViewModel> Tracks
        {
            get
            {
                return _tracks;
            }
            set
            {
                _tracks = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tracks)));
            }
        }
        public ObservableCollection<PlayListTrackViewModel> _tracks;
    }
}
