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

namespace SoundTrackPlayer.ViewModel
{
    public class PlayQueueTrackViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _is_playing;
        public PlayQueueTrackViewModel(Track t)
        {
            Track = t;
            PlayButtonCommand = new Command(async () =>
            {
                StaticResource.Player.Queue.SetCurrentTrack(Track);
                switch (StaticResource.Player.State)
                {
                    case PlayerState.Playing:
                        break;
                    case PlayerState.Paused:
                        await StaticResource.Player.Play();
                        break;
                    case PlayerState.Stopped:
                        await StaticResource.Player.Play();
                        break;
                }
            }, () =>
            {
                //return !_is_playing;
                return true;
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
                //PlayButtonCommand.ChangeCanExecute();
            }
        }

        public Track Track { get; set; }

        public Command PlayButtonCommand { get; set; }
    }

    public class PlayQueueViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public PlayQueueViewModel()
        {
            StaticResource.Player.Queue.QueueChanged += Queue_QueueChanged;
            StaticResource.Player.Queue.CurrentTrackChanged += Queue_CurrentTrackChanged;
            Tracks = new ObservableCollection<PlayQueueTrackViewModel>(StaticResource.Player.Queue.GetTracks().Select((e) => new PlayQueueTrackViewModel(e)));

            ReorderCompletedCommand = new Command(() =>
            {
                switch (_collection_changed_events.Count)
                {
                    case 0:
                        break;
                    case 1:
                        {
                            var e = _collection_changed_events[0];
                            switch (e.Action)
                            {
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                                    {
                                        if (e.NewItems is null) throw new Exception();
                                        if (e.NewItems.Count != 1) throw new NotImplementedException();

                                        var target_item = e.NewItems[0] as PlayQueueTrackViewModel;
                                        if (target_item is null) throw new Exception();

                                        StaticResource.Player.Queue.InsertOrMove(target_item.Track, e.NewStartingIndex);
                                    }
                                    break;
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                                    throw new NotImplementedException();
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                                    {
                                        if (e.OldItems is null) throw new Exception();
                                        if (e.OldItems.Count != 1) throw new NotImplementedException();

                                        var target_item = e.OldItems[0] as PlayQueueTrackViewModel;
                                        if (target_item is null) throw new Exception();

                                        StaticResource.Player.Queue.Remove(target_item.Track);
                                    }
                                    break;
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                                    throw new NotImplementedException();
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                                    StaticResource.Player.Queue.Clear();
                                    break;
                            }
                        }
                        break;
                    case 2:
                        {
                            var e0 = _collection_changed_events[0];
                            var e1 = _collection_changed_events[1];

                            if (e0.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove &&
                                e1.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                            {
                                if (e0.OldItems is null) throw new Exception();
                                if (e1.NewItems is null) throw new Exception();

                                if (e0.OldItems.Count != 1) throw new NotImplementedException();
                                if (e1.NewItems.Count != 1) throw new NotImplementedException();

                                var target_item_0 = e0.OldItems[0] as PlayQueueTrackViewModel;
                                var target_item_1 = e1.NewItems[0] as PlayQueueTrackViewModel;

                                if (target_item_0 is null) throw new Exception();
                                if (target_item_1 is null) throw new Exception();

                                if (!target_item_0.Equals(target_item_1)) throw new Exception();

                                StaticResource.Player.Queue.InsertOrMove(target_item_0.Track, e1.NewStartingIndex);
                            } else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                _collection_changed_events.Clear();
            });

            AddCommand = new Command(async () =>
            {
                var results = await FilePicker.Default.PickMultipleAsync();

                foreach (var result in results)
                {
                    if (result is null) continue;
                    var filepath = result.FullPath;
                    var track = TrackFactory.LoadFromFile(filepath, true);
                    StaticResource.Player.Queue.Append(track);
                }
            });
            DeleteCommand = new Command(() =>
            {
                if (IsMultipleSelectionEnabled)
                {
                    if (SelectedTracks is null) return;

                    var targets = SelectedTracks.ToList();

                    foreach (PlayQueueTrackViewModel e in targets)
                    {
                        StaticResource.Player.Queue.Remove(e.Track);
                    }
                } else
                {
                    if (SelectedTrack is null) return;
                    StaticResource.Player.Queue.Remove(((PlayQueueTrackViewModel)SelectedTrack).Track);
                }

            }, () => 
            {
                if (IsMultipleSelectionEnabled)
                {
                    return SelectedTracks is not null && SelectedTracks.Count > 0;
                } else
                {
                    return SelectedTrack is not null;
                }
            });
            ClearCommand = new Command(() =>
            {
                StaticResource.Player.Queue.Clear();
            }, () => Tracks.Count > 0);

            SelectedTracksChangedCommand = new Command(() =>
            {
                DeleteCommand.ChangeCanExecute();
            });
        }

        private void Queue_CurrentTrackChanged(object? sender, Track? e)
        {
            if (Tracks is not null)
            {
                foreach (var track in Tracks)
                {
                    var is_playing = track.Track == StaticResource.Player.Queue.CurrentTrack;
                    track.IsPlaying = is_playing;
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tracks)));
        }

        private void Queue_QueueChanged(object? sender, QueueChangedEventArgs e)
        {
            if (Tracks is null) throw new Exception();

            var tracks_in_vm = Tracks.Select((t) => t.Track).ToList() ?? [];
            var tracks_in_queue = StaticResource.Player.Queue.GetTracks();
            if (tracks_in_vm.SequenceEqual(tracks_in_queue))
            {
                return;
            }

            switch (e.Action)
            {
                case QueueChangeAction.Add:
                    {
                        if (e.Tracks is null) throw new Exception();
                        foreach (var t in e.Tracks)
                        {
                            Tracks.Insert(e.NewStartingIndex, new PlayQueueTrackViewModel(t));
                        }
                    }
                    break;
                case QueueChangeAction.Remove:
                    {
                        if (e.Tracks is null) throw new Exception();
                        foreach (Track t in e.Tracks)
                        {
                            var target = Tracks!.First((vm) => vm.Track == t);
                            Tracks?.Remove(target);
                        }
                    }
                    break;
                case QueueChangeAction.Clear:
                    Tracks.Clear();
                    break;
                case QueueChangeAction.Move:
                    {
                        if (e.Tracks is null || e.Tracks.Count() != 1) throw new Exception();
                        var t = e.Tracks.First();
                        Tracks.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    break;
            }

            ClearCommand.ChangeCanExecute();
            //Tracks = new ObservableCollection<PlayQueueTrackViewModel>(StaticResource.Player.Queue.GetTracks().Select((e) => new PlayQueueTrackViewModel(e)));
        }

        public ObservableCollection<PlayQueueTrackViewModel>? Tracks
        {
            get
            {
                return _tracks;
            }
            set
            {
                _tracks = value;
                if (_tracks is not null)
                {
                    _tracks.CollectionChanged += _tracks_CollectionChanged;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tracks)));
            }
        }

        private void _tracks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _collection_changed_events.Add(e);
        }

        public ObservableCollection<PlayQueueTrackViewModel>? _tracks = null;

        private List<System.Collections.Specialized.NotifyCollectionChangedEventArgs> _collection_changed_events = [];
        public Command ReorderCompletedCommand { get; set; }

        public bool IsMultipleSelectionEnabled
        {
            get
            {
                return _is_multiple_selection_enabled;
            }
            set
            {
                _is_multiple_selection_enabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMultipleSelectionEnabled)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemReorderEnabled)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionMode)));
            }
        }
        private bool _is_multiple_selection_enabled = false;

        public bool ItemReorderEnabled
        {
            get
            {
                return !IsMultipleSelectionEnabled;
            }
        }

        public SelectionMode SelectionMode
        {
            get
            {
                return IsMultipleSelectionEnabled ? SelectionMode.Multiple : SelectionMode.Single;
            }
        }

        public Command AddCommand { get; set; }
        public Command DeleteCommand { get; set; }
        public Command ClearCommand { get; set; }

        public System.Collections.Generic.IList<object>? SelectedTracks
        {
            get
            {
                return _selected_tracks;
            }
            set
            {
                _selected_tracks = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTracks)));
            }
        }
        private System.Collections.Generic.IList<object>? _selected_tracks = null;

        public object? SelectedTrack
        {
            get
            {
                return _selected_track;
            }
            set
            {
                _selected_track = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTrack)));
            }
        }
        private object? _selected_track = null;

        public Command SelectedTracksChangedCommand { get; set; }
    }
}
