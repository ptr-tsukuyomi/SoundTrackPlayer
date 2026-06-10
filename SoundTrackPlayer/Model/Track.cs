using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace SoundTrackPlayer.Model
{
    public enum LoopPositionSource
    {
        Custom,
        File
    }
    public enum LoopMode
    {
        None,
        Limited,
        Unlimited,
        Disabled
    }

    public partial class TrackConfig : ObservableObject
    {
        public TrackConfig()
        {
        }

        [ObservableProperty]
        public partial LoopPositionSource LoopPositionSource { get; set; } = LoopPositionSource.Custom;
        [ObservableProperty]
        public partial TimeSpan? LoopBegin { get; set; } = null;
        [ObservableProperty]
        public partial TimeSpan? LoopEnd { get; set; } = null;
        [ObservableProperty]
        public partial LoopMode DefaultLoopMode { get; set; } = LoopMode.None;
        [ObservableProperty]
        public partial uint? LoopCount { get; set; }
    }

    public partial class TrackInfo : ObservableObject
    {
        public TrackInfo() {}

        [ObservableProperty]
        public partial uint? No { get; set; } = null;
        [ObservableProperty]
        public partial string? Title { get; set; } = null;
        [ObservableProperty]
        public partial TimeSpan? Length { get; set; } = null;
    }

    public partial class Track : ObservableObject
    {
        public Track()
        {
            Info = new TrackInfo();
            Config = new TrackConfig();
        }

        [ObservableProperty]
        public partial ITrackSource? Source { get; set; } = null;

        public TrackConfig Config
        {
            get
            {
                return _config!;
            }
            set
            {
                _config = value;
                _config.PropertyChanged += _config_PropertyChanged;
                OnPropertyChanged(nameof(Config));
            }
        }
        private TrackConfig? _config;

        private void _config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Config));
        }

        public TrackInfo Info
        {
            get
            {
                return _info!;
            }
            set
            {
                _info = value;
                _info.PropertyChanged += _info_PropertyChanged;
                OnPropertyChanged(nameof(Info));
            }
        }
        private TrackInfo? _info;

        private void _info_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Info));
        }

        public void LoadMetadata()
        {
            if (Source is null) throw new TrackDataLoadException();


            var stream = Source.Open();
            if (stream is null) throw new TrackDataLoadException();

            var memory_stream = new MemoryStream((int)stream.Length); // MP3 の場合、なぜか new StreamDataProvider() から返ってこなくなる。MemoryStream にコピーすると正常に動作する。なぜ？
            stream.CopyTo(memory_stream);
            stream.Close();
            stream = null;

            var result = TagLibSharp2.Core.MediaFile.ReadFromData(memory_stream.ToArray());

            Info = new TrackInfo
            {
                Title = Path.GetFileName(Source.Name)
            };

            if (result.IsSuccess && result.Tag is not null)
            {
                if (result.Tag.Title is string s)
                {
                    Info.Title = s;
                }
                if (result.Tag.Track is uint t)
                {
                    Info.No = t;
                }
            }


            var engine = new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
            var audio_format = SoundFlow.Structs.AudioFormat.Cd;

            SoundFlow.Providers.StreamDataProvider p = new SoundFlow.Providers.StreamDataProvider(engine, audio_format, memory_stream);

            if (p.FormatInfo is not null)
            {
                Info.Length = p.FormatInfo.Duration;
            } else
            {
                throw new TrackDataLoadException();
            }

            p.Dispose();
            memory_stream.Close();
        }
    }
}
