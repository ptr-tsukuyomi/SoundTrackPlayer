using SoundTrackPlayer.Model;

namespace SoundTrackPlayer.View;

public partial class PlayListContentView : ContentView
{
	PlayList target_play_list;
	public PlayListContentView(PlayList play_list)
	{
		InitializeComponent();
		target_play_list = play_list;

    }
}