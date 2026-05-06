using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using SoundTrackPlayer.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.ViewModel
{
    public class LoopModePickerItem
    {
        public string Name { get; set; } = string.Empty;
        public LoopMode Mode { get; set; } = LoopMode.Disabled;
    }

    public partial class PlayerMainPageViewModel : ObservableObject
    {
        #region constructor
        // constructor
        public PlayerMainPageViewModel()
        {
            if (Application.Current is null) throw new Exception();

            Application.Current.RequestedThemeChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(PlayPauseButtonImageSource));
            };

            StaticResource.Player.PlayerStateChanged += Player_PlayerStateChanged;
            StaticResource.Player.TrackChanged += Player_TrackChanged;
            StaticResource.Player.Queue.QueueChanged += Queue_QueueChanged;
            StaticResource.Player.StatusChanged += Player_StatusChanged;
            StaticResource.Player.TrackSkipped += Player_TrackSkipped;

            SliderDragStartedCommand = new Command(() =>
            {
                System.Diagnostics.Debug.WriteLine("SliderDragStarted");
                _is_slider_dragging = true;
            });

            SliderDragCompletedCommand = new Command(() =>
            {
                System.Diagnostics.Debug.WriteLine("SliderDragCompleted");
                Seek(TimeSpan.FromSeconds(CurrentPositionInSeconds));
                _is_slider_dragging = false;
            });

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

            LastLoopExecutionEntryForcusedCommand = new Command(() =>
            {
                _is_last_loop_execution_entry_focused = true;
            });

            LastLoopExecutionEntryUnforcusedCommand = new Command(async () =>
            {
                if (StaticResource.Player.State != PlayerState.Stopped && uint.TryParse(_last_loop_execution_string, out uint l))
                {
                    StaticResource.Player.LastLoopExecution = l;
                    if (l > 0)
                    {
                        await StaticResource.Player.SetCurrentTrackLoopMode(LoopMode.Limited);
                    } else
                    {
                        await StaticResource.Player.SetCurrentTrackLoopMode(LoopMode.Disabled);
                    }
                }
                _is_last_loop_execution_entry_focused = false;
                OnPropertyChanged(nameof(LastLoopExecutionString));
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

            CurrentLoopModePickerSelectedIndexChangedCommand = new Command(async () =>
            {
                if (_current_loop_mode_item is null) return;

                await StaticResource.Player.SetCurrentTrackLoopMode(_current_loop_mode_item.Mode);
            });

            CurrentLoopBeginEntryUnforcusedCommand = new Command(async () =>
            {
                if (StaticResource.Player.State != PlayerState.Stopped)
                {
                    if (_current_loop_begin_string == "")
                    {
                        await StaticResource.Player.SetLoopBegin(null);
                    }
                    else if (TimeSpan.TryParse(_current_loop_begin_string, out TimeSpan result))
                    {
                        await StaticResource.Player.SetLoopBegin(result);
                    }
                }
                OnPropertyChanged(nameof(CurrentLoopBeginString));
            });

            CurrentLoopEndEntryUnforcusedCommand = new Command(async () =>
            {
                if (StaticResource.Player.State != PlayerState.Stopped)
                {
                    if (_current_loop_end_string == "")
                    {
                        await StaticResource.Player.SetLoopEnd(null);
                    }
                    else if (TimeSpan.TryParse(_current_loop_end_string, out TimeSpan result))
                    {
                        await StaticResource.Player.SetLoopEnd(result);
                    }
                }
                OnPropertyChanged(nameof(CurrentLoopEndString));
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
                        if (Page is not null)
                        {
                            await Page.DisplayAlertAsync(Track.Info.Title ?? "", "トラック設定の保存に失敗しました。", "OK");
                        }
                    }
                }
            });

            LoopBeginFindCommand = new Command(() =>
            {
                if (Track is not null)
                {
                    var bg_task = LoopPoint.CreateFindLoopBeginTask(Track);
                    StaticResource.BackgroundTaskRunner.Enqueue(bg_task);
                }
            });

            VolumeButtonClickCommand = new Command(() =>
            {
                IsMuted = !IsMuted;
            });

            ShuffleModeButtonClickCommand = new Command(() =>
            {
                StaticResource.Player.ShuffleMode = StaticResource.Player.ShuffleMode == ShuffleMode.On ? ShuffleMode.Off : ShuffleMode.On;
                Config.ShuffleMode = StaticResource.Player.ShuffleMode;
                OnPropertyChanged(nameof(ShuffleModeButtonImageSource));
            });

            ContinuousPlayModeButtonClickCommand = new Command(() =>
            {
                StaticResource.Player.ContinuousPlayMode = StaticResource.Player.ContinuousPlayMode == ContinuousPlayMode.Queue ? ContinuousPlayMode.Off : ContinuousPlayMode.Queue;
                Config.ContinuousPlayMode = StaticResource.Player.ContinuousPlayMode;
                OnPropertyChanged(nameof(ContinuousPlayModeButtonImageSource));
            });

            var content_dir = Config.ContentDirectories;
            foreach (var dir in content_dir)
            {
                var bg_task = PlayList.CreateFindPlayListTask(dir);
                StaticResource.BackgroundTaskRunner.Enqueue(bg_task);
            }
        }
        #endregion

        #region event handler
        // event handler

        private void Player_TrackSkipped(object? sender, Track? e)
        {
            RefreshTrackView();
        }

        private void Player_StatusChanged(object? sender, EventArgs e)
        {
            RefreshCurrentPlayerView();
        }

        private void Queue_QueueChanged(object? sender, QueueChangedEventArgs e)
        {
            NextButtonClickCommand.ChangeCanExecute();
            PreviousButtonClickCommand.ChangeCanExecute();
            PlayPauseButtonClickCommand.ChangeCanExecute();
        }

        private void Player_TrackChanged(object? sender, Track? e)
        {
            Track = StaticResource.Player.Queue.CurrentTrack;
            if(Track is not null)
            {
                Track.PropertyChanged += Track_PropertyChanged;
            }
            CurrentPositionInSeconds = 0.0;

            NextButtonClickCommand.ChangeCanExecute();
            PreviousButtonClickCommand.ChangeCanExecute();

            RefreshTrackView();
            RefreshCurrentPlayerView();
        }

        private void Track_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RefreshTrackView();
        }

        private void RefreshTrackView()
        {
            OnPropertyChanged(nameof(TrackLoopBeginString));
            OnPropertyChanged(nameof(TrackLoopEndString));
            OnPropertyChanged(nameof(TrackDefaultLoopCountString));
            OnPropertyChanged(nameof(TrackDefaultLoopModeItem));

            if (Track is not null && Track.Info.Length is TimeSpan t && StaticResource.Player.State != PlayerState.Stopped)
            {
                TrackLengthInSeconds = t.TotalSeconds;
            } else
            {
                TrackLengthInSeconds = 0.0;
            }
        }

        private void RefreshCurrentPlayerView()
        {
            OnPropertyChanged(nameof(PlayPauseButtonImageSource));
            OnPropertyChanged(nameof(CurrentLoopModeItem));
            OnPropertyChanged(nameof(CurrentLoopBeginString));
            OnPropertyChanged(nameof(CurrentLoopEndString));
            OnPropertyChanged(nameof(LastLoopExecutionString));
            OnPropertyChanged(nameof(VolumeButtonImageSource));
        }

        private async void Player_PlayerStateChanged(object? sender, PlayerState e)
        {
            RefreshTrackView();
            RefreshCurrentPlayerView();

            switch (e)
            {
                case PlayerState.Playing:
                    refresh_state_cts = new CancellationTokenSource();
                    refresh_state_task = RefreshState(refresh_state_cts.Token);
                    break;
                case PlayerState.Paused:
                    refresh_state_cts?.Cancel();
                    await refresh_state_task;
                    refresh_state_cts = null;
                    break;
                case PlayerState.Stopped:
                    refresh_state_cts?.Cancel();
                    await refresh_state_task;
                    refresh_state_cts = null;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region refresh state
        private Task refresh_state_task = Task.CompletedTask;
        private CancellationTokenSource? refresh_state_cts;
        async Task RefreshState(CancellationToken ct)
        {
            PeriodicTimer refresh_state_timer = new(TimeSpan.FromMilliseconds(100));
            while (true)
            {
                try
                {
                    await refresh_state_timer.WaitForNextTickAsync(ct);
                    if (!_is_slider_dragging)
                    {
                        CurrentPositionInSeconds = StaticResource.Player.Position.TotalSeconds;
                    }
                    if (!_is_last_loop_execution_entry_focused)
                    {
                        OnPropertyChanged(nameof(LastLoopExecutionString));
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        #endregion

        #region property
        // property

        [ObservableProperty]
        public partial Track? Track { get; set; }

        public ImageSource PlayPauseButtonImageSource
        {
            get
            {
                if (Application.Current is null) throw new Exception();
                string theme = Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";

                return StaticResource.Player.State switch
                {
                    PlayerState.Playing => ImageSource.FromFile($"pause_{theme}.png"),
                    PlayerState.Paused => ImageSource.FromFile($"play_{theme}.png"),
                    PlayerState.Stopped => ImageSource.FromFile($"play_{theme}.png"),
                    _ => throw new NotImplementedException()
                };
            }
        }

        public double Volume
        { 
            get 
            {
                return IsMuted ? 0.0 : _volume;
            }
            set
            {
                _volume = value;
                if (IsMuted)
                {
                    IsMuted = false;
                }
                StaticResource.Player.Volume = (float)value;
                Config.Volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }
        private double _volume = Config.Volume;

        public bool IsMuted
        {
            get
            {
                return _is_muted;
            }
            set
            {
                _is_muted = value;
                StaticResource.Player.Volume = _is_muted ? 0.0f : (float)_volume;
                Config.IsMuted = _is_muted;
                OnPropertyChanged(nameof(IsMuted));
                OnPropertyChanged(nameof(VolumeButtonImageSource));
            }
        }
        private bool _is_muted = Config.IsMuted;

        public ImageSource ShuffleModeButtonImageSource
        {
            get
            {
                if (Application.Current is null) throw new Exception();
                string theme = Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";

                return StaticResource.Player.ShuffleMode switch
                {
                    ShuffleMode.Off => ImageSource.FromFile($"shuffle_off_{theme}.png"),
                    ShuffleMode.On => ImageSource.FromFile($"shuffle_on_{theme}.png"),
                    _ => throw new NotImplementedException()
                };
            }
        }

        public ImageSource ContinuousPlayModeButtonImageSource
        {
            get
            {
                if (Application.Current is null) throw new Exception();
                string theme = Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";

                return StaticResource.Player.ContinuousPlayMode switch
                {
                    ContinuousPlayMode.Off => ImageSource.FromFile($"continuous_play_off_{theme}.png"),
                    ContinuousPlayMode.Queue => ImageSource.FromFile($"continuous_play_queue_{theme}.png"),
                    _ => throw new NotImplementedException()
                };
            }
        }


        public TimeSpan CurrentPosition
        {
            get
            {
                return TimeSpan.FromSeconds(CurrentPositionInSeconds);
            }
        }

        public double CurrentPositionInSeconds
        {
            get
            {
                return _current_position_in_seconds;
            }
            set
            {
                _current_position_in_seconds = value;
                OnPropertyChanged(nameof(CurrentPositionInSeconds));
                OnPropertyChanged(nameof(CurrentPosition));
            }
        }
        private double _current_position_in_seconds = 0.0;

        [ObservableProperty]
        public partial double TrackLengthInSeconds { get; set; } = 0;


        public string TrackLoopBeginString
        {
            get
            {
                if (Track?.Config.LoopBegin is null)
                {
                    return "未設定";
                } else
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

        public string LastLoopExecutionString
        {
            get
            {
                switch (StaticResource.Player.CurrentTrackLoopMode)
                {
                    case LoopMode.None:
                    case LoopMode.Disabled:
                        return "0";
                    case LoopMode.Limited:
                        return StaticResource.Player.LastLoopExecution.ToString();
                    case LoopMode.Unlimited:
                        return "無限";
                    default:
                        throw new NotImplementedException();
                }
            } set
            {
                _last_loop_execution_string = value;
            }
        }
        private string _last_loop_execution_string = string.Empty;

        public string TrackDefaultLoopCountString
        {
            get
            {
                if (Track is not null)
                {
                    if (Track.Config.LoopCount is null)
                    {
                        return "未設定";
                    } else
                    {
                        return Track.Config.LoopCount.Value.ToString();
                    }
                } else
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

        public IList<LoopModePickerItem> DefaultLoopModePickerItems { get; set; } = new List<LoopModePickerItem>()
        {
            new() { Name = "未設定", Mode = LoopMode.None },
            new() { Name = "有限ループ", Mode = LoopMode.Limited },
            new() { Name = "無限ループ", Mode = LoopMode.Unlimited },
            new() { Name = "ループ無効", Mode = LoopMode.Disabled }
        };

        public IList<LoopModePickerItem> CurrentLoopModePickerItems { get; set; } = new List<LoopModePickerItem>()
        {
            new() { Name = "有限ループ", Mode = LoopMode.Limited },
            new() { Name = "無限ループ", Mode = LoopMode.Unlimited },
            new() { Name = "ループ無効", Mode = LoopMode.Disabled }
        };


        public LoopModePickerItem TrackDefaultLoopModeItem
        {
            get
            {
                if (Track is not null)
                {
                    var mode = Track.Config.DefaultLoopMode;
                    var item = DefaultLoopModePickerItems.First(e => e.Mode == mode);
                    return item;
                } else
                {
                    return DefaultLoopModePickerItems.First(e => e.Mode == LoopMode.Disabled);
                }
            }
            set
            {
                if (Track is not null)
                {
                    Track.Config.DefaultLoopMode = value.Mode;
                }
                OnPropertyChanged(nameof(TrackDefaultLoopModeItem));
            }
        }

        public LoopModePickerItem CurrentLoopModeItem
        {
            get
            {
                var mode = StaticResource.Player.CurrentTrackLoopMode;
                if (mode == LoopMode.None)
                {
                    mode = LoopMode.Disabled;
                }
                var item = CurrentLoopModePickerItems.First(e => e.Mode == mode);
                return item;
            }
            set
            {
                _current_loop_mode_item = value;
            }
        }
        LoopModePickerItem? _current_loop_mode_item = null;

        public string CurrentLoopBeginString
        {
            get
            {
                if (StaticResource.Player.LoopBegin is null)
                {
                    return "未設定";
                }
                else
                {
                    return StaticResource.Player.LoopBegin.Value.ToString(@"hh\:mm\:ss\.ffffff");
                }
            }
            set
            {
                _current_loop_begin_string = value;
            }
        }
        private string _current_loop_begin_string = string.Empty;

        public string CurrentLoopEndString
        {
            get
            {
                if (StaticResource.Player.LoopEnd is null)
                {
                    return "未設定";
                }
                else
                {
                    return StaticResource.Player.LoopEnd.Value.ToString(@"hh\:mm\:ss\.ffffff");
                }
            }
            set
            {
                _current_loop_end_string = value;
            }
        }
        private string _current_loop_end_string = string.Empty;

        public ImageSource VolumeButtonImageSource
        {
            get
            {
                if (Application.Current is null) throw new Exception();
                string theme = Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";

                return IsMuted switch
                {
                    true => ImageSource.FromFile($"speaker_off_{theme}.png"),
                    false => ImageSource.FromFile($"speaker_loud_{theme}.png")
                };
            }
        }

        public Microsoft.Maui.Controls.Page? Page { get; set; }

        #endregion

        #region command
        // command

        public Command PlayPauseButtonClickCommand { get; set; } = new Command(async () =>
        {
            switch (StaticResource.Player.State)
            {
                case PlayerState.Playing:
                    StaticResource.Player.Pause();
                    break;
                case PlayerState.Paused:
                    await StaticResource.Player.Play();
                    break;
                case PlayerState.Stopped:
                    await StaticResource.Player.Play();
                    break;
                default:
                    throw new NotImplementedException();
            }
        },() => StaticResource.Player.Queue.Length != 0);

        public Command NextButtonClickCommand { get; set; } = new Command(() =>
        {
            StaticResource.Player.Queue.Next();
        }, () => {
            return StaticResource.Player.Queue.CurrentTrackNo != -1 && (StaticResource.Player.Queue.CurrentTrackNo + 1) < StaticResource.Player.Queue.Length;
        });

        public Command PreviousButtonClickCommand { get; set; } = new Command(() =>
        {
            StaticResource.Player.Queue.Previous();
        }, () => {
            return StaticResource.Player.Queue.CurrentTrackNo != -1 && StaticResource.Player.Queue.CurrentTrackNo > 0;
        });

        private bool _is_slider_dragging = false;
        public Command SliderDragStartedCommand { get; set; } 

        public Command SliderDragCompletedCommand { get; set; }

        public Command TrackLoopBeginEntryUnforcusedCommand { get; set; }

        public Command TrackLoopEndEntryUnforcusedCommand { get; set; }

        public Command LastLoopExecutionEntryForcusedCommand { get; set; }
        public Command LastLoopExecutionEntryUnforcusedCommand { get; set; }

        private bool _is_last_loop_execution_entry_focused = false;

        public Command TrackDefaultLoopCountEntryUnforcusedCommand { get; set; }

        public Command CurrentLoopModePickerSelectedIndexChangedCommand { get; set; }


        public Command CurrentLoopBeginEntryUnforcusedCommand { get; set; }

        public Command CurrentLoopEndEntryUnforcusedCommand { get; set; }

        public Command TrackConfigSaveCommand { get; set; }

        public Command LoopBeginFindCommand { get; set; }

        public Command VolumeButtonClickCommand { get; set; }

        public Command ShuffleModeButtonClickCommand { get; set; }

        public Command ContinuousPlayModeButtonClickCommand { get; set; }
        #endregion


        #region misc
        private static async void Seek(TimeSpan to)
        {
            await StaticResource.Player.Seek(to);
        }
        #endregion
    }
}
