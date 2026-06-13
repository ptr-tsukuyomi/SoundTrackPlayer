using Microsoft.UI.Xaml;
using SoundTrackPlayer.Model;
using Windows.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SoundTrackPlayer.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

    public class MediaControl
    {
        public static SystemMediaTransportControls? _smtc { get; set; } = null;

        public static void Initialize(nint hWnd)
        {
            _smtc = Windows.Media.SystemMediaTransportControlsInterop.GetForWindow(hWnd);
            _smtc.IsEnabled = true;
            _smtc.IsStopEnabled = true;
            _smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            _smtc.ButtonPressed += _smtc_ButtonPressed;
            UpdateSMTC();

            StaticResource.Player.PlayerStateChanged += Player_PlayerStateChanged;
            StaticResource.Player.TrackChanged += Player_TrackChanged;
            StaticResource.Player.Queue.QueueChanged += Queue_QueueChanged;
        }

        private static void Queue_QueueChanged(object? sender, QueueChangedEventArgs e)
        {
            UpdateSMTC();
        }

        private static void Player_TrackChanged(object? sender, Track? e)
        {
            UpdateSMTC();
        }

        private static void Player_PlayerStateChanged(object? sender, PlayerState e)
        {
            UpdateSMTC();
        }

        private static void UpdateSMTC()
        {
            if (_smtc is null) throw new Exception();

            _smtc.IsNextEnabled = StaticResource.Player.Queue.CurrentTrackNo != -1 && (StaticResource.Player.Queue.CurrentTrackNo + 1) < StaticResource.Player.Queue.Length;
            _smtc.IsPreviousEnabled = StaticResource.Player.Queue.CurrentTrackNo != -1 && StaticResource.Player.Queue.CurrentTrackNo > 0;
            _smtc.IsPlayEnabled = StaticResource.Player.Queue.Length != 0;
            _smtc.IsPauseEnabled = StaticResource.Player.State == PlayerState.Playing;

            _smtc.PlaybackStatus = StaticResource.Player.State switch
            {
                PlayerState.Playing => MediaPlaybackStatus.Playing,
                PlayerState.Paused => MediaPlaybackStatus.Paused,
                PlayerState.Stopped => MediaPlaybackStatus.Stopped,
                _ => throw new NotImplementedException(),
            };
            _smtc.DisplayUpdater.MusicProperties.Title = StaticResource.Player.Queue.CurrentTrack?.Info.Title ?? "";
            _smtc.DisplayUpdater.Update();
        }

        private static async void _smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(args.Button.ToString());

            if (StaticResource.UIThreadDispatcher is null) throw new Exception();

            await StaticResource.UIThreadDispatcher.DispatchAsync(async () =>
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        await StaticResource.Player.Play();
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        StaticResource.Player.Pause();
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        StaticResource.Player.Queue.Next();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        StaticResource.Player.Queue.Previous();
                        break;
                    default:
                        break;
                }
            });
        }
    }
}
