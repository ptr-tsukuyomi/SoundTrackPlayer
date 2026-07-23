using CommunityToolkit.Mvvm.ComponentModel;
using SoundTrackPlayer.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Maui.Storage;
using System.Diagnostics;
using System.Text;

namespace SoundTrackPlayer.ViewModel
{
    public partial class PlayListTrackViewModel : ObservableObject
    {
        private bool _is_playing;

        private PlayList _play_list;
        public PlayListTrackViewModel(Track t, PlayList p, int trackNoInPlayList)
        {
            if (Application.Current is null) throw new Exception();

            Track = t;
            _play_list = p;

            Track.PropertyChanged += (o, e) =>
            {
                RefreshTrackView();
            };

            Application.Current.RequestedThemeChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(DetailButtonImageSource));
            };


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

            TrackLoopBeginEntryUnforcusedCommand = new Command(async () =>
            {
                if (Track is not null)
                {
                    if (_track_loop_begin_string == "")
                    {
                        Track.Config.LoopBegin = null;
                    }
                    else if (TimeSpan.TryParse(_track_loop_begin_string, out TimeSpan result))
                    {
                        Track.Config.LoopBegin = result;
                    }
                }
                OnPropertyChanged(nameof(TrackLoopBeginString));
            });

            TrackLoopEndEntryUnforcusedCommand = new Command(async () =>
            {
                if (Track is not null)
                {
                    if (_track_loop_end_string == "")
                    {
                        Track.Config.LoopEnd = null;
                    }
                    else if (TimeSpan.TryParse(_track_loop_end_string, out TimeSpan result))
                    {
                        Track.Config.LoopEnd = result;
                    }
                }
                OnPropertyChanged(nameof(TrackLoopEndString));
            });

            LoopBeginFindCommand = new Command(() =>
            {
                if (Track is not null)
                {
                    var bg_task = LoopPoint.CreateFindLoopBeginTask(Track);
                    StaticResource.BackgroundTaskRunner.Enqueue(bg_task);
                }
            });

            TrackDefaultLoopCountEntryUnforcusedCommand = new Command(() =>
            {
                if (Track is not null)
                {
                    if (string.IsNullOrEmpty(_default_loop_count_string))
                    {
                        Track.Config.LoopCount = null;
                    }
                    else if (uint.TryParse(_default_loop_count_string, out uint l))
                    {
                        Track.Config.LoopCount = l;
                    }
                }
                OnPropertyChanged(nameof(TrackDefaultLoopCountString));
            });

            TrackConfigSaveCommand = new Command(async () =>
            {
                if (Track is not null && Track.Source is not null)
                {
                    try
                    {
                        Track.Source.SaveTrackConfig(Track.Config);
                    }
                    catch (Exception)
                    {
                        await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync(Track.Info.Title ?? "", "トラック設定の保存に失敗しました。", "OK");
                    }
                }
            });

            DetailButtonCommand = new Command(() =>
            {
                IsDetailExpaned = !IsDetailExpaned;
            });
        }

        public void RefreshTrackView()
        {
            OnPropertyChanged(nameof(TrackDefaultLoopModeItem));
            OnPropertyChanged(nameof(TrackLoopBeginString));
            OnPropertyChanged(nameof(TrackLoopEndString));
            OnPropertyChanged(nameof(TrackDefaultLoopCountString));
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

        public IList<LoopModePickerItem> DefaultLoopModePickerItems { get; } = Common.DefaultLoopModePickerItems;

        public LoopModePickerItem TrackDefaultLoopModeItem
        {
            get
            {
                if (Track is not null)
                {
                    var mode = Track.Config.DefaultLoopMode;
                    var item = Common.DefaultLoopModePickerItems.First(e => e.Mode == mode);
                    return item;
                }
                else
                {
                    return Common.DefaultLoopModePickerItems.First(e => e.Mode == LoopMode.Disabled);
                }
            }
            set
            {
                if (Track is not null && value is not null)
                {
                    Track.Config.DefaultLoopMode = value.Mode;
                }
                OnPropertyChanged(nameof(TrackDefaultLoopModeItem));
            }
        }

        public string TrackLoopBeginString
        {
            get
            {
                if (Track?.Config.LoopBegin is null)
                {
                    return "未設定";
                }
                else
                {
                    return Track.Config.LoopBegin.Value.ToString(@"hh\:mm\:ss\.ffffff");
                }
            }
            set
            {
                _track_loop_begin_string = value;
            }
        }
        private string _track_loop_begin_string = string.Empty;

        public string TrackLoopEndString
        {
            get
            {
                if (Track?.Config.LoopEnd is null)
                {
                    return "未設定";
                }
                else
                {
                    return Track.Config.LoopEnd.Value.ToString(@"hh\:mm\:ss\.ffffff");
                }
            }
            set
            {
                _track_loop_end_string = value;
            }
        }
        private string _track_loop_end_string = string.Empty;

        public Command TrackLoopBeginEntryUnforcusedCommand { get; set; }

        public Command TrackLoopEndEntryUnforcusedCommand { get; set; }

        public Command LoopBeginFindCommand { get; set; }

        public string TrackDefaultLoopCountString
        {
            get
            {
                if (Track is not null)
                {
                    if (Track.Config.LoopCount is null)
                    {
                        return "未設定";
                    }
                    else
                    {
                        return Track.Config.LoopCount.Value.ToString();
                    }
                }
                else
                {
                    return "-";
                }
            }
            set
            {
                _default_loop_count_string = value;
            }
        }
        private string _default_loop_count_string = string.Empty;

        public Command TrackDefaultLoopCountEntryUnforcusedCommand { get; set; }

        public Command TrackConfigSaveCommand { get; set; }

        public bool IsDetailExpaned
        {
            get
            {
                return _is_detail_expanded;
            }
            set
            {
                _is_detail_expanded = value;
                OnPropertyChanged(nameof(IsDetailExpaned));
                OnPropertyChanged(nameof(DetailButtonImageSource));
            }
        }
        bool _is_detail_expanded = false;

        public ImageSource DetailButtonImageSource
        {
            get
            {
                if (Application.Current is null) throw new Exception();
                string theme = Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";

                return IsDetailExpaned switch
                {
                    true => ImageSource.FromFile($"double_arrow_up_{theme}.png"),
                    false => ImageSource.FromFile($"double_arrow_down_{theme}.png")
                };
            }
        }

        public Command DetailButtonCommand { get; set; }
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
            _play_list.PropertyChanged += PlayList_Changed;
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
                int min_track_no = Tracks.Count;
                if (IsMultipleSelectionEnabled)
                {
                    if (SelectedTracks is null) return;

                    var targets = SelectedTracks.ToList();

                    foreach (PlayListTrackViewModel e in targets)
                    {
                        //StaticResource.Player.Queue.Remove(e.Track);
                        min_track_no = Math.Min(min_track_no, e.TrackNoInPlayList);
                        Tracks.Remove(e);
                        _play_list.Tracks.Remove(e.Track);
                    }
                }
                else
                {
                    if (SelectedTrack is null) return;
                    //StaticResource.Player.Queue.Remove(((PlayListTrackViewModel)SelectedTrack).Track);
                    min_track_no = ((PlayListTrackViewModel)SelectedTrack).TrackNoInPlayList;
                    _play_list.Tracks.Remove(((PlayListTrackViewModel)SelectedTrack).Track);
                    Tracks.Remove((PlayListTrackViewModel)SelectedTrack);
                }
                CalculateTrackNoInPlayList(begin: min_track_no - 1);
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

            PlayListSaveCommand = new Command(async () =>
            {
                if (_play_list.Source == null)
                {
                    var filesave_result = await FileSaver.SaveAsync($"{_play_list.Name}.m3u8", new MemoryStream(0));
                    if (filesave_result.IsSuccessful)
                    {
                        var src = new FileOriginPlayListSource()
                        {
                            FilePath = filesave_result.FilePath,
                            Format = new M3UPlayListFormat()
                        };
                        _play_list.Source = src;
                        _play_list.Name = Path.GetFileNameWithoutExtension(src.FilePath);
                        OnPropertyChanged(nameof(Name));
                    } else
                    {
                        return;
                    }
                }
                try
                {
                    _play_list.Source.SavePlayList(_play_list);

                    foreach (var e in _tracks!)
                    {
                        e.TrackConfigSaveCommand.Execute(null);
                    }
                } catch (Exception e)
                {
                    await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("", $"プレイリストの保存に失敗しました。\r\n\r\n[例外]\r\n{e.GetType().Name}\r\n\r\n[メッセージ]\r\n{e.Message}", "OK");
                }
            });

            PlayListDeleteCommand = new Command(async () =>
            {
                var result = await Application.Current!.Windows[0]!.Page!.DisplayActionSheetAsync("プレイリストを削除しますか？", "キャンセル", null, ["プレイヤーから削除", "ファイルも削除"]);
                if (result is null || result == "キャンセル") return;

                StaticResource.PlayLists.Remove(_play_list);

                if (result == "ファイルも削除" && _play_list.Source != null)
                {
                    try
                    {
                        _play_list.Source.DeletePlayList();
                        _play_list.Tracks.Clear();
                    }
                    catch (Exception e)
                    {
                        await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("", $"プレイリストの削除に失敗しました。\r\n\r\n[例外]\r\n{e.GetType().Name}\r\n\r\n[メッセージ]\r\n{e.Message}", "OK");
                    }
                }
            });

            TrackConfigSaveCommand = new Command(async () =>
            {
                var csv = Misc.GenerateTrackConfigCsv(_play_list.Tracks);

                var filesave_result = await FileSaver.SaveAsync($"{_play_list.Name}.csv", new MemoryStream(0));
                if (!filesave_result.IsSuccessful) return;

                try
                {
                    using (var writer = new StreamWriter(filesave_result.FilePath, false, new UTF8Encoding(false)))
                    {
                        writer.Write(csv);
                    }
                }
                catch (Exception e)
                {
                    await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("", $"トラック設定一覧の保存に失敗しました。\r\n\r\n[例外]\r\n{e.GetType().Name}\r\n\r\n[メッセージ]\r\n{e.Message}", "OK");
                }
            });

            TrackConfigLoadCommand = new Command(async () =>
            {
                var r1 = await FilePicker.Default.PickAsync();
                if (r1 is null) return;

                try
                {
                    var csv = await File.ReadAllTextAsync(r1.FullPath, new UTF8Encoding(false));
                    var track_configs = Misc.CreateTrackConfigFromCsv(csv).ToList();

                    if (_play_list.Tracks.Count != track_configs.Count)
                    {
                        var r2 = await Application.Current!.Windows[0]!.Page!.DisplayActionSheetAsync("設定反映対象のトラック数と読み込んだトラック設定の数が一致しません。上から可能な限り設定を反映しますか？", "キャンセル", null, ["はい", "いいえ"]);
                        if (r2 != "はい") return;
                    }

                    var num_of_tracks_to_apply = Math.Min(_play_list.Tracks.Count, track_configs.Count);
                    for (int i = 0;i < num_of_tracks_to_apply; ++i)
                    {
                        var track = _play_list.Tracks[i];
                        var config = track_configs[i];
                        track.Config.LoopBegin = config.LoopBegin;
                        track.Config.LoopEnd = config.LoopEnd;
                        track.Config.DefaultLoopMode = config.DefaultLoopMode;
                        track.Config.LoopCount = config.LoopCount;
                        _tracks![i].RefreshTrackView();
                    }
                }
                catch (Exception e)
                {
                    await Application.Current!.Windows[0]!.Page!.DisplayAlertAsync("", $"トラック設定一覧の読み込みに失敗しました。\r\n\r\n[例外]\r\n{e.GetType().Name}\r\n\r\n[メッセージ]\r\n{e.Message}", "OK");
                }
            });
        }


        private void PlayList_Changed(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayList.Name):
                    OnPropertyChanged(nameof(Name));
                    break;
                case nameof(PlayList.Tracks):
                    Tracks = null;
                    Tracks = new ObservableCollection<PlayListTrackViewModel>(_play_list.Tracks.Select((e, i) => new PlayListTrackViewModel(e, _play_list, i + 1)));
                    break;
                case nameof(PlayList.Source):
                    break;
                default:
                    throw new NotImplementedException();
            }
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

        public Command PlayListSaveCommand { get; set; }

        public Command PlayListDeleteCommand { get; set; }

        public Command TrackConfigLoadCommand { get; set; }

        public Command TrackConfigSaveCommand { get; set; }
    }
}
