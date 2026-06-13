using Microsoft.Extensions.DependencyInjection;
using SoundTrackPlayer.Model;

namespace SoundTrackPlayer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var w = new Window(new SoundTrackPlayer.View.PlayerMainPage()) { Height = 800, Width = 1280 };
            w.Destroying += Window_Destroying;
            StaticResource.UIThreadDispatcher = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread();
            return w;
        }

        private async void Window_Destroying(object? sender, EventArgs e)
        {
            await StaticResource.Player.Stop();
        }
    }
}