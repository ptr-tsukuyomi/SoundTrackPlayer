namespace SoundTrackPlayer.View;

public partial class FlyoutMenuPage : ContentPage
{
	public FlyoutMenuPage()
	{
		InitializeComponent();

        PlayListCollectionView.SelectionChanged += PlayListCollectionView_SelectionChanged;
        MenuItemsCollectionView.SelectionChanged += MenuItemsCollectionView_SelectionChanged;
	}

    private void MenuItemsCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count != 0)
        {
            PlayListCollectionView.SelectedItem = null;
        }
    }

    private void PlayListCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count != 0)
        {
            MenuItemsCollectionView.SelectedItem = null;
        }
    }
}