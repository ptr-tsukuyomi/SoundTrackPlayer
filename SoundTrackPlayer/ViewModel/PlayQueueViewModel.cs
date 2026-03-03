using CommunityToolkit.Mvvm.ComponentModel;
using SoundTrackPlayer.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SoundTrackPlayer.ViewModel
{
    public partial class PlayQueueTrackViewModel : ObservableObject
    {
        private bool _is_playing;
        public PlayQueueTrackViewModel(Track t, int trackNoInPlayQueue)
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
                return true;
            });
            IsPlaying = StaticResource.Player.Queue.CurrentTrack == Track;
            TrackNoInPlayQueue = trackNoInPlayQueue;
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
        public partial int TrackNoInPlayQueue { get; set; }
    }

    public partial class PlayQueueViewModel : ObservableObject
    {
        private void CalculateTrackNoInPlayQueue(int begin = 0, int end = -1)
        {
            if (Tracks is null) throw new Exception();
            for (int i = begin; i < (end < 0 ? Tracks.Count : Math.Min(end + 1, Tracks.Count)); ++i)
            {
                Tracks[i].TrackNoInPlayQueue = i + 1;
            }
        }

        public PlayQueueViewModel()
        {
            StaticResource.Player.Queue.QueueChanged += Queue_QueueChanged;
            StaticResource.Player.Queue.CurrentTrackChanged += Queue_CurrentTrackChanged;
            Tracks = new ObservableCollection<PlayQueueTrackViewModel>(StaticResource.Player.Queue.GetTracks().Select((e, i) => new PlayQueueTrackViewModel(e, i + 1)));

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
                                        CalculateTrackNoInPlayQueue(begin: e.NewStartingIndex);
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
                                        CalculateTrackNoInPlayQueue(begin: target_item.TrackNoInPlayQueue - 1);
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
                                CalculateTrackNoInPlayQueue(begin: Math.Min(target_item_0.TrackNoInPlayQueue - 1, e1.NewStartingIndex));
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

            FindLoopBeginCommand = new Command(() =>
            {
                if (IsMultipleSelectionEnabled)
                {
                    if (SelectedTracks is null) return;

                    var targets = SelectedTracks.ToList();

                    foreach (PlayQueueTrackViewModel e in targets)
                    {
                        var bg_task = LoopPoint.CreateFindLoopBeginTask(e.Track);
                        StaticResource.BackgroundTaskRunner.Enqueue(bg_task);
                    }
                }
                else
                {
                    if (SelectedTrack is null) return;
                    var bg_task = LoopPoint.CreateFindLoopBeginTask(((PlayQueueTrackViewModel)SelectedTrack).Track);
                    StaticResource.BackgroundTaskRunner.Enqueue(bg_task);
                }
            }, () =>
            {
                if (IsMultipleSelectionEnabled)
                {
                    return SelectedTracks is not null && SelectedTracks.Count > 0;
                }
                else
                {
                    return SelectedTrack is not null;
                }
            });

            SelectedTracksChangedCommand = new Command(() =>
            {
                DeleteCommand.ChangeCanExecute();
                FindLoopBeginCommand.ChangeCanExecute();
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
            OnPropertyChanged(nameof(Tracks));
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

                        for (int i = 0; i < e.Tracks.Count(); ++i)
                        {
                            Tracks.Insert(e.NewStartingIndex + i, new PlayQueueTrackViewModel(e.Tracks.ElementAt(i), e.NewStartingIndex + i + 1));
                        }
                        CalculateTrackNoInPlayQueue(begin: e.NewStartingIndex + e.Tracks.Count());
                    }
                    break;
                case QueueChangeAction.Remove:
                    {
                        if (e.Tracks is null) throw new Exception();
                        var min_track_no_in_play_queue = -1;
                        foreach (Track t in e.Tracks)
                        {
                            var target = Tracks!.First((vm) => vm.Track == t);
                            min_track_no_in_play_queue = min_track_no_in_play_queue == -1 ? target.TrackNoInPlayQueue : Math.Min(min_track_no_in_play_queue, target.TrackNoInPlayQueue);
                            Tracks?.Remove(target);
                        }

                        if (min_track_no_in_play_queue != -1)
                        {
                            CalculateTrackNoInPlayQueue(begin: min_track_no_in_play_queue - 1);
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
                        CalculateTrackNoInPlayQueue(begin: Math.Min(e.OldStartingIndex, e.NewStartingIndex));
                    }
                    break;
            }

            _collection_changed_events.Clear();
            ClearCommand.ChangeCanExecute();
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
                OnPropertyChanged(nameof(Tracks));
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
                OnPropertyChanged(nameof(IsMultipleSelectionEnabled));
                OnPropertyChanged(nameof(ItemReorderEnabled));
                OnPropertyChanged(nameof(SelectionMode));
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

        public Command FindLoopBeginCommand { get; set; }

        [ObservableProperty]
        public partial System.Collections.Generic.IList<object>? SelectedTracks { get; set; } = null;

        [ObservableProperty]
        public partial object? SelectedTrack { get; set; } = null;

        public Command SelectedTracksChangedCommand { get; set; }
    }
}
