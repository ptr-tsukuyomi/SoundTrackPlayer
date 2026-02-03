using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
