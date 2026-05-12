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
                //StaticResource.Player.Queue.SetCurrentTrack(Track);
                await StaticResource.Player.Play(Track);
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

        private void CalculateTrackNoInPlayList(int begin = 0, int end = -1)
        {
            if (Tracks is null) throw new Exception();
            for (int i = begin; i < (end < 0 ? Tracks.Count : Math.Min(end + 1, Tracks.Count)); ++i)
            {
                Tracks[i].TrackNoInPlayList = i + 1;
            }
        }

        public PlayListViewModel(PlayList play_list)
        {
            _play_list = play_list;
            StaticResource.Player.Queue.CurrentTrackChanged += Queue_CurrentTrackChanged;
            Tracks = new ObservableCollection<PlayListTrackViewModel>(_play_list.Tracks.Select((e, i) => new PlayListTrackViewModel(e, _play_list, i + 1)));

            //StaticResource.Player.Queue.QueueChanged += Queue_QueueChanged;
            //StaticResource.Player.Queue.CurrentTrackChanged += Queue_CurrentTrackChanged;
            //Tracks = new ObservableCollection<PlayQueueTrackViewModel>(StaticResource.Player.Queue.GetTracks().Select((e, i) => new PlayQueueTrackViewModel(e, i + 1)));

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

                                        var target_item = e.NewItems[0] as PlayListTrackViewModel;
                                        if (target_item is null) throw new Exception();

                                        //StaticResource.Player.Queue.InsertOrMove(target_item.Track, e.NewStartingIndex);
                                        _play_list.Tracks.Insert(e.NewStartingIndex, target_item.Track);
                                        CalculateTrackNoInPlayList(begin: e.NewStartingIndex);
                                    }
                                    break;
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                                    throw new NotImplementedException();
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                                    {
                                        if (e.OldItems is null) throw new Exception();
                                        if (e.OldItems.Count != 1) throw new NotImplementedException();

                                        var target_item = e.OldItems[0] as PlayListTrackViewModel;
                                        if (target_item is null) throw new Exception();

                                        //StaticResource.Player.Queue.Remove(target_item.Track);
                                        _play_list.Tracks.Remove(target_item.Track);
                                        CalculateTrackNoInPlayList(begin: target_item.TrackNoInPlayList - 1);
                                    }
                                    break;
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                                    throw new NotImplementedException();
                                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                                    //StaticResource.Player.Queue.Clear();
                                    _play_list.Tracks.Clear();
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

                                var target_item_0 = e0.OldItems[0] as PlayListTrackViewModel;
                                var target_item_1 = e1.NewItems[0] as PlayListTrackViewModel;

                                if (target_item_0 is null) throw new Exception();
                                if (target_item_1 is null) throw new Exception();

                                if (!target_item_0.Equals(target_item_1)) throw new Exception();

                                //StaticResource.Player.Queue.InsertOrMove(target_item_0.Track, e1.NewStartingIndex);
                                _play_list.Tracks.Remove(target_item_0.Track);
                                _play_list.Tracks.Insert(e1.NewStartingIndex, target_item_0.Track);
                                CalculateTrackNoInPlayList(begin: Math.Min(target_item_0.TrackNoInPlayList - 1, e1.NewStartingIndex));
                            }
                            else
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

                int num_of_tracks_before = Tracks.Count;
                foreach (var result in results)
                {
                    if (result is null) continue;
                    var filepath = result.FullPath;
                    var track = TrackFactory.LoadFromFile(filepath, true);
                    //StaticResource.Player.Queue.Append(track);
                    //Tracks = new ObservableCollection<PlayListTrackViewModel>(_play_list.Tracks.Select((e, i) => new PlayListTrackViewModel(e, _play_list, i + 1)));
                    _play_list.Tracks.Add(track);
                    Tracks.Add(new PlayListTrackViewModel(track, _play_list, 0));
                }
                CalculateTrackNoInPlayList(begin: num_of_tracks_before);
                _collection_changed_events.Clear();
            });
            DeleteCommand = new Command(() =>
            {
                if (IsMultipleSelectionEnabled)
                {
                    if (SelectedTracks is null) return;

                    var targets = SelectedTracks.ToList();

                    foreach (PlayListTrackViewModel e in targets)
                    {
                        //StaticResource.Player.Queue.Remove(e.Track);
                        Tracks.Remove(e);
                        _play_list.Tracks.Remove(e.Track);
                    }
                }
                else
                {
                    if (SelectedTrack is null) return;
                    //StaticResource.Player.Queue.Remove(((PlayListTrackViewModel)SelectedTrack).Track);
                    _play_list.Tracks.Remove(((PlayListTrackViewModel)SelectedTrack).Track);
                    Tracks.Remove((PlayListTrackViewModel)SelectedTrack);
                }
                _collection_changed_events.Clear();
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
            ClearCommand = new Command(() =>
            {
                //StaticResource.Player.Queue.Clear();
                Tracks.Clear();
                _play_list.Tracks.Clear();
                _collection_changed_events.Clear();
            }, () => Tracks.Count > 0);

            FindLoopBeginCommand = new Command(() =>
            {
                if (IsMultipleSelectionEnabled)
                {
                    if (SelectedTracks is null) return;

                    var targets = SelectedTracks.ToList();

                    foreach (PlayListTrackViewModel e in targets)
                    {
                        var bg_task = LoopPoint.CreateFindLoopBeginTask(e.Track);
                        StaticResource.BackgroundTaskRunner.Enqueue(bg_task);
                    }
                }
                else
                {
                    if (SelectedTrack is null) return;
                    var bg_task = LoopPoint.CreateFindLoopBeginTask(((PlayListTrackViewModel)SelectedTrack).Track);
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

        //private void Queue_CurrentTrackChanged(object? sender, Track? e)
        //{
        //    foreach (var track in Tracks)
        //    {
        //        var is_playing = track.Track == StaticResource.Player.Queue.CurrentTrack;
        //        track.IsPlaying = is_playing;
        //    }
        //    OnPropertyChanged(nameof(Tracks));
        //}

        //[ObservableProperty]
        //public partial ObservableCollection<PlayListTrackViewModel> Tracks { get; set; }

        public string Name
        {
            get
            {
                return _play_list.Name;
            }
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



        //private void Queue_QueueChanged(object? sender, QueueChangedEventArgs e)
        //{
        //    if (Tracks is null) throw new Exception();

        //    var tracks_in_vm = Tracks.Select((t) => t.Track).ToList() ?? [];
        //    var tracks_in_queue = StaticResource.Player.Queue.GetTracks();
        //    if (tracks_in_vm.SequenceEqual(tracks_in_queue))
        //    {
        //        return;
        //    }

        //    switch (e.Action)
        //    {
        //        case QueueChangeAction.Add:
        //            {
        //                if (e.Tracks is null) throw new Exception();

        //                for (int i = 0; i < e.Tracks.Count(); ++i)
        //                {
        //                    Tracks.Insert(e.NewStartingIndex + i, new PlayListTrackViewModel(e.Tracks.ElementAt(i), e.NewStartingIndex + i + 1));
        //                }
        //                CalculateTrackNoInPlayQueue(begin: e.NewStartingIndex + e.Tracks.Count());
        //            }
        //            break;
        //        case QueueChangeAction.Remove:
        //            {
        //                if (e.Tracks is null) throw new Exception();
        //                var min_track_no_in_play_queue = -1;
        //                foreach (Track t in e.Tracks)
        //                {
        //                    var target = Tracks!.First((vm) => vm.Track == t);
        //                    min_track_no_in_play_queue = min_track_no_in_play_queue == -1 ? target.TrackNoInPlayQueue : Math.Min(min_track_no_in_play_queue, target.TrackNoInPlayQueue);
        //                    Tracks?.Remove(target);
        //                }

        //                if (min_track_no_in_play_queue != -1)
        //                {
        //                    CalculateTrackNoInPlayQueue(begin: min_track_no_in_play_queue - 1);
        //                }
        //            }
        //            break;
        //        case QueueChangeAction.Clear:
        //            Tracks.Clear();
        //            break;
        //        case QueueChangeAction.Move:
        //            {
        //                if (e.Tracks is null || e.Tracks.Count() != 1) throw new Exception();
        //                var t = e.Tracks.First();
        //                Tracks.Move(e.OldStartingIndex, e.NewStartingIndex);
        //                CalculateTrackNoInPlayQueue(begin: Math.Min(e.OldStartingIndex, e.NewStartingIndex));
        //            }
        //            break;
        //    }

        //    _collection_changed_events.Clear();
        //    ClearCommand.ChangeCanExecute();
        //}

        public ObservableCollection<PlayListTrackViewModel>? Tracks
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

        public ObservableCollection<PlayListTrackViewModel>? _tracks = null;

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
                SelectedTrack = null;
                SelectedTracks = null;
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
