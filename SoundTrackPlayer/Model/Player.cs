using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Animations;
using SoundFlow.Backends.MiniAudio.Devices;
using System;
using System.IO;
using System.Reflection;

namespace SoundTrackPlayer.Model
{
    public class PlayableTrackNotFoundException : Exception
    {

    }

    public class CannotDetermineLoopEndException : Exception
    {

    }

    public class TrackDataLoadException : Exception
    {
    }

    public enum PlayerState
    {
        Playing,
        Stopped,
        Paused
    }

    public enum QueueChangeAction
    {
        Add,
        Remove,
        Move,
        Clear
    }

    public enum ShuffleMode
    {
        Off,
        On
    }
    public enum ContinuousPlayMode
    {
        Off,
        Queue
    }


    public class QueueChangedEventArgs
    {
        public QueueChangeAction Action { get; set; } = QueueChangeAction.Clear;
        public IEnumerable<Track>? Tracks { get; set; } = null;
        public int NewStartingIndex { get; set; } = -1;
        public int OldStartingIndex { get; set; } = -1;
    }

    public class TrackQueue
    {
        public TrackQueue() { }

        private List<Track> queue = [];
        private int current_track_no = -1; // -1: no track selected

        public event EventHandler<Track?>? CurrentTrackChanged;
        public event EventHandler<QueueChangedEventArgs>? QueueChanged;

        public Track? CurrentTrack { get { return queue.Count == 0 || current_track_no == -1 ? null : queue[current_track_no]; } }
        public int CurrentTrackNo { get { return current_track_no; } }

        public void Shuffle(Track? head = null)
        {
            int shuffle_begin = 0;
            if(head != null)
            {
                if (!queue.Contains(head)) throw new ArgumentException("head must be in the queue");
                InsertOrMove(head, 0);
                shuffle_begin = 1;
            }

            int[] index = Enumerable.Range(shuffle_begin, queue.Count - shuffle_begin).Shuffle().ToArray();
            for(int i = shuffle_begin; i < index.Length; ++i)
            {
                InsertOrMove(queue[index[i]], i);
            }
        }

        public bool SetCurrentTrack(Track track)
        {
            var index = queue.IndexOf(track);
            return SetCurrentTrack(index);
        }

        public bool SetCurrentTrack(int index)
        {
            if (index < 0 || index >= queue.Count) return false;

            current_track_no = index;
            CurrentTrackChanged?.Invoke(this, CurrentTrack);
            return true;
        }

        public void UnsetCurrentTrack()
        {
            current_track_no = -1;
            CurrentTrackChanged?.Invoke(this, CurrentTrack);
        }

        public bool Next()
        {
            if (current_track_no == -1 && queue.Count == 0 || current_track_no != -1 && current_track_no + 1 >= queue.Count)
            {
                return false;
            }

            if (current_track_no == -1)
            {
                current_track_no = 0;
            }
            else
            {
                current_track_no += 1;
            }
            CurrentTrackChanged?.Invoke(this, CurrentTrack);
            return true;
        }

        public bool Previous()
        {
            if (current_track_no == -1 && queue.Count == 0 || current_track_no == 0)
            {
                return false;
            }

            if (current_track_no == -1)
            {
                current_track_no = queue.Count - 1;
            }
            else
            {
                current_track_no -= 1;
            }
            CurrentTrackChanged?.Invoke(this, CurrentTrack);
            return true;
        }

        public void Clear()
        {
            queue.Clear();
            UnsetCurrentTrack();

            QueueChanged?.Invoke(this, new QueueChangedEventArgs() { Action = QueueChangeAction.Clear });
        }

        public void Append(Track t)
        {
            queue.Add(t);
            QueueChanged?.Invoke(this, new QueueChangedEventArgs() { Action = QueueChangeAction.Add, Tracks = [t], NewStartingIndex = queue.Count - 1 });
        }

        public void Append(IEnumerable<Track> tracks)
        {
            queue.AddRange(tracks);
            QueueChanged?.Invoke(this, new QueueChangedEventArgs() { Action = QueueChangeAction.Add, Tracks = tracks, NewStartingIndex = queue.Count - tracks.Count() });
        }

        public bool Remove(Track t, bool emit_queue_changed = true)
        {
            int index = queue.IndexOf(t);
            if (index == -1) return false;
            queue.RemoveAt(index);
            if (index == current_track_no)
            {
                UnsetCurrentTrack();
            }
            else if (index < current_track_no)
            {
                --current_track_no;
            }
            QueueChanged?.Invoke(this, new QueueChangedEventArgs() { Action = QueueChangeAction.Remove, Tracks = [t], OldStartingIndex = index });
            return true;
        }

        public void InsertOrMove(Track t, int insert_to = 0, bool emit_queue_changed = true)
        {
            if (insert_to < 0 || insert_to > queue.Count) throw new ArgumentOutOfRangeException();

            int old_index = queue.IndexOf(t);
            bool is_current_track_targeted = false;
            if (old_index != -1) // insert only 
            {
                is_current_track_targeted = old_index == current_track_no;
                queue.RemoveAt(old_index);
                if (is_current_track_targeted)
                {
                    current_track_no = -1;
                }
                else if (old_index < current_track_no)
                {
                    --current_track_no;
                }
            }

            queue.Insert(insert_to, t);
            if (is_current_track_targeted)
            {
                current_track_no = insert_to;
            }
            else if (insert_to <= current_track_no)
            {
                ++current_track_no;
            }

            if (old_index == -1) // insert only
            {
                QueueChanged?.Invoke(this, new QueueChangedEventArgs() { Action = QueueChangeAction.Add, Tracks = [t], NewStartingIndex = insert_to });
            }
            else
            {
                QueueChanged?.Invoke(this, new QueueChangedEventArgs() { Action = QueueChangeAction.Move, Tracks = [t], NewStartingIndex = insert_to, OldStartingIndex = old_index });
            }
        }

        public IEnumerable<Track> GetTracks()
        {
            return queue;
        }

        public int Length { get { return queue.Count; } }
    }

    public class PlayerCore // 1 track 再生するだけの役割
    {
        public event EventHandler? TrackChanged;
        public event EventHandler? PlaybackCompleted;
        public event EventHandler? LoopExecuted;
        public event EventHandler? StatusChanged;

        public PlayerCore()
        {
            var d = Dispatcher.GetForCurrentThread();
            if (d is null) throw new Exception();
            _dispatcher = d;
        }

        ~PlayerCore()
        {
            _device_watch_cts?.Cancel();
        }

        private SoundFlow.Abstracts.AudioEngine _engine = new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
        private SoundFlow.Abstracts.Devices.AudioPlaybackDevice? _playback_device = null;
        private SoundFlow.Components.SoundPlayer? _player;
        private SoundFlow.Structs.AudioFormat _audio_format = SoundFlow.Structs.AudioFormat.Cd;
        private SoundFlow.Providers.StreamDataProvider? _src = null;
        private System.IO.MemoryStream? _memory_stream = null;
        private Task _loop_watch_task = Task.CompletedTask;
        private Task _device_watch_task = Task.CompletedTask;

        public Track? Track { get { return _track; } }
        private Track? _track = null;
        public LoopMode DefaultTrackLoopMode
        {
            get
            {
                return _track_loop_mode;
            }
            set
            {
                _track_loop_mode = value;
                StatusChanged?.Invoke(this, new EventArgs());
            }
        }
        private LoopMode _track_loop_mode = LoopMode.None;

        public uint DefaultTrackLoopCount
        {
            get
            {
                return _track_loop_count;
            }
            set
            {
                _track_loop_count = value;
                StatusChanged?.Invoke(this, new EventArgs());
            }
        }
        private uint _track_loop_count = 0;

        private Stream? _stream = null;

        public LoopMode CurrentTrackLoopMode
        {
            get
            {
                return _current_track_loop_mode;
            }
        }
        private LoopMode _current_track_loop_mode = LoopMode.None;

        public async Task SetCurrentLoopMode(LoopMode mode)
        {
            if (_player is not null)
            {
                _current_track_loop_mode = mode;
                await SetLoopForCurrentTrack();
                StatusChanged?.Invoke(this, new EventArgs());
            }
        }


        public TimeSpan? LoopBegin
        {
            get
            {
                return _loop_begin;
            }
        }
        private TimeSpan? _loop_begin = null;

        public async Task SetLoopBegin(TimeSpan? begin)
        {
            _loop_begin = begin;
            await SetLoopForCurrentTrack();
            StatusChanged?.Invoke(this, new EventArgs());
        }

        public TimeSpan? LoopEnd
        {
            get
            {
                return _loop_end;
            }
        }
        private TimeSpan? _loop_end = null;

        public async Task SetLoopEnd(TimeSpan? end)
        {
            _loop_end = end;
            await SetLoopForCurrentTrack();
            StatusChanged?.Invoke(this, new EventArgs());
        }

        public uint LastLoopExecution
        {
            get
            {
                return _last_loop_execute;
            }
            set
            {
                _last_loop_execute = value;
                StatusChanged?.Invoke(this, new EventArgs());
            }
        }
        private uint _last_loop_execute = 0;

        private IDispatcher _dispatcher;

        public TimeSpan Position
        {
            get
            {
                return TimeSpan.FromSeconds(_player is null ? 0 : _player.Time);
            }
        }


        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                if (_player is not null) _player.Volume = _volume;
                StatusChanged?.Invoke(this, new EventArgs());
            }
        }
        private float _volume = 1.0f;

        public bool IsMuted
        {
            get
            {
                return _is_muted;
            }
            set
            {
                _is_muted = value;
                if (_player is not null) _player.Mute = _is_muted;
                StatusChanged?.Invoke(this, new EventArgs());
            }
        }
        private bool _is_muted = false;

        private CancellationTokenSource? _loop_watch_cts;
        private CancellationTokenSource? _device_watch_cts;

        public void Initialize()
        {
            _engine.UpdateAudioDevicesInfo();
            var device_info = _engine.PlaybackDevices.FirstOrDefault((x) => x.IsDefault);
            _playback_device = _engine.InitializePlaybackDevice(device_info, _audio_format);
            _playback_device.Start();

            _device_watch_cts = new CancellationTokenSource();
            _device_watch_task = DeviceWatch(_device_watch_cts.Token);
        }


        private SoundFlow.Providers.StreamDataProvider LoadTrackData(Track t)
        {
            if (t.Source is null) throw new TrackDataLoadException();


            _stream = t.Source.Open();
            if (_stream is null) throw new TrackDataLoadException();

            _memory_stream = new MemoryStream((int)_stream.Length); // MP3 の場合、なぜか new StreamDataProvider() から返ってこなくなる。MemoryStream にコピーすると正常に動作する。なぜ？
            _stream.CopyTo(_memory_stream);

            SoundFlow.Providers.StreamDataProvider p = new SoundFlow.Providers.StreamDataProvider(_engine, _audio_format, _memory_stream);

            if (p.FormatInfo is not null)
            {
                if (p.FormatInfo.Tags is not null)
                {
                    t.Info.Title = p.FormatInfo.Tags.Title;
                }
                t.Info.Length = p.FormatInfo.Duration;
            }
            return p;
        }

        async Task LoopWatch(float initial, CancellationToken ct)
        {
            if (_player is null) return;

            PeriodicTimer loop_watch_timer = new(TimeSpan.FromMilliseconds(100));
            float before = initial;
            while (_player.IsLooping)
            {
                try
                {
                    await loop_watch_timer.WaitForNextTickAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var current = _player.Time;
                if (current - before < 0) // current - before がマイナスになった場合にループ1回実行と判断する
                {
                    if (_current_track_loop_mode == LoopMode.Limited)
                    {
                        --_last_loop_execute;
                        if (_last_loop_execute == 0)
                        {
                            _player.IsLooping = false;
                        }
                        StatusChanged?.Invoke(this, new EventArgs());
                    }
                    LoopExecuted?.Invoke(this, new EventArgs());
                }
                before = current;
            }
        }

        private async Task DeviceWatch(CancellationToken ct)
        {
            PeriodicTimer loop_watch_timer = new(TimeSpan.FromMilliseconds(500));
            while (true)
            {
                try
                {
                    await loop_watch_timer.WaitForNextTickAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _engine.UpdateAudioDevicesInfo();

                if (_playback_device is not null && _playback_device.Info is not null)
                {
                    var device_id = _playback_device.Info.Value.Id;
                    var device_name = _playback_device.Info.Value.Name;
                    try
                    {
                        var device_info = _engine.PlaybackDevices.First((x) => x.Id == device_id);
                        if (!device_info.IsDefault || device_info.Name != device_name)
                        {
                            SwitchToDefaultDevice();
                        }
                    }
                    catch (InvalidOperationException) // current device is not found.
                    {
                        SwitchToDefaultDevice();
                    }
                }
            }
        }

        private void SwitchToDefaultDevice()
        {
            var new_device_info = _engine.PlaybackDevices.FirstOrDefault((x) => x.IsDefault);
            SwitchToDefaultDevice(new_device_info);
        }

        private void SwitchToDefaultDevice(SoundFlow.Structs.DeviceInfo new_device_info)
        {
            if (_playback_device is not null)
            {
                _playback_device = _engine.SwitchDevice(_playback_device, new_device_info);
            }
        }


        public async Task<bool> Seek(TimeSpan to)
        {
            if (_player is null) return false;
            if (_player.State == SoundFlow.Enums.PlaybackState.Stopped) return false;

            var orig_state = _player.State;

            if (orig_state == SoundFlow.Enums.PlaybackState.Playing)
            {
                _player.Pause();
                if (_loop_watch_task is not null)
                {
                    _loop_watch_cts?.Cancel();
                    await _loop_watch_task;
                    _loop_watch_cts = null;
                }
            }

            bool result = _player.Seek(to);

            if (orig_state == SoundFlow.Enums.PlaybackState.Playing)
            {
                if (_player.IsLooping)
                {
                    _loop_watch_cts = new CancellationTokenSource();
                    _loop_watch_task = LoopWatch(_player.Time, _loop_watch_cts.Token);
                }
                _player.Play();
            }

            return result;
        }

        private void PlayBackEnded(object? sender, EventArgs e)
        {
            _dispatcher.Dispatch(async () =>
            {
                await Stop();
                PlaybackCompleted?.Invoke(this, new EventArgs());
            });
        }

        private uint CalculateLastLoopExecute()
        {
            if (Track is null) throw new Exception();

            uint loop_count = Track.Config.LoopCount is not null ? Track.Config.LoopCount.Value : DefaultTrackLoopCount;
            if (loop_count <= 1)
            {
                return 0;
            }
            else
            {
                return loop_count - 1;
            }
        }

        private LoopMode DetermineCurrentLoopMode()
        {
            if (Track is null) throw new Exception();

            return Track.Config.DefaultLoopMode == LoopMode.None ? DefaultTrackLoopMode : Track.Config.DefaultLoopMode;
        }

        private void CalculateLoopBeginAndEnd()
        {
            if (Track is null) throw new Exception();

            _loop_begin = null;
            _loop_end = null;
            switch (Track.Config.LoopPositionSource)
            {
                case LoopPositionSource.Custom:
                    _loop_begin = Track.Config.LoopBegin;
                    _loop_end = Track.Config.LoopEnd;
                    break;
                case LoopPositionSource.File:
                    throw new NotImplementedException();
            }
            StatusChanged?.Invoke(this, new EventArgs());
        }

        private async Task SetLoopForCurrentTrack()
        {
            if (_player is null) throw new Exception();
            if (_src is null) throw new Exception();

            if (_loop_begin is null)
            {
                _loop_begin = TimeSpan.Zero;
            }
            if (_loop_end is null)
            {
                _loop_end = _src.FormatInfo?.Duration;
            }

            switch (_current_track_loop_mode)
            {
                case LoopMode.Unlimited:
                case LoopMode.Limited:
                    if (LoopEnd is null) throw new CannotDetermineLoopEndException();
                    _player.IsLooping = _current_track_loop_mode == LoopMode.Limited && _last_loop_execute == 0 ? false : true;
                    _player.SetLoopPoints((TimeSpan)_loop_begin, _loop_end);

                    if (_player.IsLooping && (_loop_watch_task is null || _loop_watch_task.IsCompleted))
                    {
                        _loop_watch_cts = new CancellationTokenSource();
                        _loop_watch_task = LoopWatch(0.0f, _loop_watch_cts.Token);
                    }
                    break;
                case LoopMode.None:
                case LoopMode.Disabled:
                    _player.IsLooping = false;

                    if (_loop_watch_task is not null)
                    {
                        _loop_watch_cts?.Cancel();
                        await _loop_watch_task;
                        _loop_watch_cts = null;
                    }
                    break;
            }

            StatusChanged?.Invoke(this, new EventArgs());
        }


        public async Task Play()
        {
            if (Track is null) throw new PlayableTrackNotFoundException();
            if (_playback_device is null) Initialize();
            if (_player is null)
            {
                _src = LoadTrackData(Track);

                _player = new SoundFlow.Components.SoundPlayer(_engine, _audio_format, _src);
                _player.Volume = _volume;

                CalculateLoopBeginAndEnd();

                _last_loop_execute = CalculateLastLoopExecute();
                _current_track_loop_mode = DetermineCurrentLoopMode();
                await SetLoopForCurrentTrack();

                _player.PlaybackEnded += PlayBackEnded;

                StatusChanged?.Invoke(this, new EventArgs());
            }
            switch (_player.State)
            {
                case SoundFlow.Enums.PlaybackState.Playing:
                    break;
                case SoundFlow.Enums.PlaybackState.Stopped:
                    if (_playback_device is null) throw new Exception();
                    _playback_device.MasterMixer.AddComponent(_player);
                    _player.Play();
                    break;
                case SoundFlow.Enums.PlaybackState.Paused:
                    _player.Play();
                    break;
            }
        }

        public async Task Stop()
        {
            if (_player is not null)
            {
                if (_loop_watch_task is not null)
                {
                    _loop_watch_cts?.Cancel();
                    await _loop_watch_task;
                    _loop_watch_cts = null;
                }

                switch (_player.State)
                {
                    case SoundFlow.Enums.PlaybackState.Playing:
                    case SoundFlow.Enums.PlaybackState.Paused:
                        _player.Stop();
                        break;
                    default:
                        break;
                }
                _playback_device?.MasterMixer.RemoveComponent(_player);
                _player.Dispose();
                _player = null;
                _src?.Dispose();
                _src = null;
                _memory_stream?.Close();
                _memory_stream = null;
                _stream?.Close();
                _stream = null;
            }
        }

        public void Pause()
        {
            _player?.Pause();
        }

        public async Task SetTrack(Track? t)
        {
            await Stop();
            _track = t;
            TrackChanged?.Invoke(this, new EventArgs());
        }
    }

    public class Player
    {
        public Player()
        {
            Queue.CurrentTrackChanged += Queue_CurrentTrackChanged;

            pc.PlaybackCompleted += Pc_PlaybackCompleted;
            pc.LoopExecuted += Pc_LoopExecuted;
            pc.StatusChanged += Pc_StatusChanged;
            //pc.DefaultTrackLoopMode = DefaultTrackLoopMode;
            //pc.DefaultTrackLoopCount = DefaultTrackLoopCount;
        }

        private void Pc_StatusChanged(object? sender, EventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        private void Pc_LoopExecuted(object? sender, EventArgs e)
        {
            LoopExecuted?.Invoke(this, e);
        }

        private void Pc_PlaybackCompleted(object? sender, EventArgs e)
        {
            Next();
        }

        public event EventHandler<Track?>? TrackSkipped;
        public event EventHandler? TrackQueueCompleted;
        public event EventHandler<PlayerState>? PlayerStateChanged;
        public event EventHandler<Track?>? TrackChanged;

        public event EventHandler? LoopExecuted;
        public event EventHandler? StatusChanged;

        private PlayerCore pc = new PlayerCore();

        public LoopMode DefaultTrackLoopMode
        {
            get
            {
                return pc.DefaultTrackLoopMode;
            }
            set
            {
                pc.DefaultTrackLoopMode = value;
            }
        }

        public uint DefaultTrackLoopCount
        {
            get
            {
                return pc.DefaultTrackLoopCount;
            }
            set
            {
                pc.DefaultTrackLoopCount = value;
            }
        }

        public uint LastLoopExecution
        {
            get
            {
                return pc.LastLoopExecution;
            }
            set
            {
                pc.LastLoopExecution = value;
            }
        }

        public LoopMode CurrentTrackLoopMode
        {
            get
            {
                return pc.CurrentTrackLoopMode;
            }
        }

        public async Task SetCurrentTrackLoopMode(LoopMode mode)
        {
            await pc.SetCurrentLoopMode(mode);
        }

        public TimeSpan? LoopBegin
        {
            get
            {
                return pc.LoopBegin;
            }
        }

        public async Task SetLoopBegin(TimeSpan? begin)
        {
            await pc.SetLoopBegin(begin);
        }

        public TimeSpan? LoopEnd
        {
            get
            {
                return pc.LoopEnd;
            }
        }

        public async Task SetLoopEnd(TimeSpan? end)
        {
            await pc.SetLoopEnd(end);
        }


        public TrackQueue Queue { get; set; } = new TrackQueue();
        public PlayerState State
        {
            get { return _state; }
            set
            {
                var old_state = _state;
                _state = value;
                if (old_state != _state)
                {
                    PlayerStateChanged?.Invoke(this, _state);
                }
            }
        }
        private PlayerState _state = PlayerState.Stopped;

        public float Volume
        {
            get
            {
                return pc.Volume;
            }
            set
            {
                pc.Volume = value;
            }
        }

        public bool IsMuted
        {
            get
            {
                return pc.IsMuted;
            }
            set
            {
                pc.IsMuted = value;
            }
        }

        public TimeSpan Position
        {
            get
            {
                return pc.Position;
            }
        }

        public async Task<bool> Seek(TimeSpan to)
        {
            return await pc.Seek(to);
        }

        private void Next()
        {
            if (!Queue.Next())
            {
                TrackQueueCompleted?.Invoke(this, new EventArgs());
                switch (ContinuousPlayMode)
                {
                    case ContinuousPlayMode.Off:
                        State = PlayerState.Stopped;
                        Queue.UnsetCurrentTrack();
                        break;
                    case ContinuousPlayMode.Queue:
                        switch (ShuffleMode) 
                        {
                            case ShuffleMode.Off:
                                break;
                            case ShuffleMode.On:
                                // shuffle queue
                                Queue.Shuffle();
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        Queue.SetCurrentTrack(0);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private async void Queue_CurrentTrackChanged(object? sender, Track? e)
        {
            switch (State)
            {
                case PlayerState.Playing:
                    if (e is null)
                    {
                        // stop playing
                        await pc.Stop();
                        await pc.SetTrack(null);
                        State = PlayerState.Stopped;
                    }
                    else
                    {
                        // change track and play
                        await pc.Stop();
                        await pc.SetTrack(e);
                        try
                        {
                            await pc.Play();
                        }
                        catch (Exception)
                        {
                            TrackSkipped?.Invoke(this, e);
                            Next();
                            //if (!Queue.Next())
                            //{
                            //    TrackQueueCompleted?.Invoke(this, new EventArgs());
                            //    State = PlayerState.Stopped;
                            //    Queue.SetCurrentTrack(0);
                            //}
                        }
                    }
                    break;
                case PlayerState.Paused:
                    // stop playing
                    await pc.Stop();
                    State = PlayerState.Stopped;
                    if (e is not null)
                    {
                        // change track
                        await pc.SetTrack(e);
                    }
                    else
                    {
                        await pc.SetTrack(null);
                    }
                    break;
                case PlayerState.Stopped:
                    if (e is not null)
                    {
                        // change track
                        await pc.SetTrack(e);
                    }
                    else
                    {
                        await pc.SetTrack(null);
                    }
                    break;
            }
            TrackChanged?.Invoke(this, e);
        }



        public async Task Play(Track? begin = null)
        {
            if (State == PlayerState.Paused)
            {
                State = PlayerState.Playing;
                await pc.Play();
            }
            else
            {
                await pc.Stop();
                if (Queue.CurrentTrack is null && !Queue.Next() || begin != null && !Queue.GetTracks().Contains(begin))
                {
                    State = PlayerState.Stopped;
                    throw new PlayableTrackNotFoundException();
                }
                State = PlayerState.Playing;

                switch (ShuffleMode)
                {
                    case ShuffleMode.Off:
                        break;
                    case ShuffleMode.On:
                        // shuffle queue
                        Queue.Shuffle(begin);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (begin != null)
                {
                    Queue.SetCurrentTrack(begin);
                } else
                {
                    Queue.SetCurrentTrack(0);
                }

                try
                {
                    await pc.Play();
                }
                catch (Exception)
                {
                    TrackSkipped?.Invoke(this, pc.Track);
                    Next();
                    //if (!Queue.Next())
                    //{
                    //    TrackQueueCompleted?.Invoke(this, new EventArgs());
                    //    State = PlayerState.Stopped;
                    //    Queue.SetCurrentTrack(0);
                    //}
                }
            }
        }


        public async Task Stop()
        {
            // stop playing
            await pc.Stop();
            State = PlayerState.Stopped;
        }

        public void Pause()
        {
            // pause
            pc.Pause();
            State = PlayerState.Paused;
        }


        public Model.ContinuousPlayMode ContinuousPlayMode { get; set; } = ContinuousPlayMode.Off;
        public Model.ShuffleMode ShuffleMode { get; set; } = ShuffleMode.Off;
    }
}
