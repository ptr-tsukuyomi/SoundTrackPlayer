//using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using SoundTrackPlayer.Model;
using SoundTrackPlayer.View;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.ViewModel
{
    public partial class FlyoutPageItem : ObservableObject
    {
        public FlyoutPageItem()
        {
            if (Application.Current is null) throw new Exception();

            Application.Current.RequestedThemeChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IconSource));
            };
        }

        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;
        [ObservableProperty]
        public partial string LightIconSource { get; set; } = string.Empty;
        [ObservableProperty]
        public partial string DarkIconSource { get; set; } = string.Empty;
        public string IconSource
        {
            get
            {
                if (Application.Current is null) throw new Exception();

                if (Application.Current.RequestedTheme == AppTheme.Dark)
                {
                    return DarkIconSource;
                }
                else
                {
                    return LightIconSource;
                }
            }
        }
        [ObservableProperty]
        public partial Type TargetType { get; set; } = typeof(MainPage);
    }

    public partial class FlyoutMenuPageViewModel : ObservableObject
    {
        public Command PlayListUpdateCommand { get; set; } = new Command(() =>
        {
            //StaticResource.PlayLists.Clear();

            var content_dir = Config.ContentDirectories;
            foreach (var dir in content_dir)
            {
                var bg_task = PlayList.CreateFindPlayListTask(dir);
                StaticResource.BackgroundTaskRunner.Enqueue(bg_task);
            }
        });

        public Command PlayListAddCommand { get; set; } = new Command(() =>
        {
            var new_playlist = new PlayList()
            {
                Source = null,
                Name = "新しいプレイリスト",
                Tracks = new System.Collections.ObjectModel.ObservableCollection<Track>()
            };
            StaticResource.PlayLists.Add(new_playlist);
        });

        [ObservableProperty]
        public partial List<FlyoutPageItem> MenuItems { get; set; } = [
            new FlyoutPageItem()
            {
                Title = "再生キュー",
                LightIconSource = "list_bullet_light.png",
                DarkIconSource = "list_bullet_dark.png",
                TargetType = typeof(PlayQueueContentView)
            },
            new FlyoutPageItem()
            {
                Title = "タスク",
                LightIconSource = "check_square_light.png",
                DarkIconSource = "check_square_dark.png",
                TargetType = typeof(BackgroundTaskContentView)
            },
            new FlyoutPageItem()
            {
                Title = "設定",
                LightIconSource = "gear_light.png",
                DarkIconSource = "gear_dark.png",
                TargetType = typeof(SettingsContentView)
            }
        ];
    }
}
