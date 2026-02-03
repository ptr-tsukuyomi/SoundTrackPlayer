using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SoundTrackPlayer.Model;
#if WINDOWS
using Windows.ApplicationModel.Activation;
#endif

namespace SoundTrackPlayer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureLifecycleEvents(events =>
                {
#if WINDOWS
                    events.AddWindows(windows => windows.OnLaunched(async (window, buggy_args) =>
                    {
                        var args = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

                        switch (args.Kind)
                        {
                            case Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File:
                                {
                                    var data = args.Data as IFileActivatedEventArgs;
                                    var tracks = data?.Files.Select(file => TrackFactory.LoadFromFile(file.Path, true));
                                    if (tracks is not null)
                                    {
                                        StaticResource.Player.Queue.Append(tracks);
                                    }
                                    await StaticResource.Player.Play();
                                    break;
                                }
                            case Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Launch:
                                {
                                    var data = args.Data as ILaunchActivatedEventArgs;
                                    var arg = data?.Arguments;
                                    if (arg is not null)
                                    {
                                        var args_list = Misc.ParseArguments(arg);
                                        if (args_list.Count >= 2)
                                        {
                                            var filenames = args_list[1..];
                                            var tracks = filenames.Select(path => TrackFactory.LoadFromFile(path, true));
                                            if (tracks is not null)
                                            {
                                                StaticResource.Player.Queue.Append(tracks);
                                            }
                                            await StaticResource.Player.Play();
                                        }
                                    }
                                    break;
                                }
                        }
                    }));
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
