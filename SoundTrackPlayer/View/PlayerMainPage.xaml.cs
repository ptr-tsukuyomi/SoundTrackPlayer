using CommunityToolkit.Maui.Behaviors;
using SoundTrackPlayer.Model;
using SoundTrackPlayer.ViewModel;

namespace SoundTrackPlayer.View;

public partial class PlayerMainPage : FlyoutPage
{
    public PlayerMainPage()
	{
		InitializeComponent();

		MenuPage.MenuItemsCollectionView.SelectionChanged += OnMenuSelectionChanged;
        MenuPage.PlayListCollectionView.SelectionChanged += PlayListCollectionView_SelectionChanged;

        MenuPage.PlayListCollectionView.ItemsSource = StaticResource.PlayLists;

        ((PlayerMainPageViewModel)ContentPage.BindingContext).Page = this;
    }

    private void PlayListCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection.FirstOrDefault() as PlayList;
        if (item is not null)
        {
            var obj = new PlayListContentView(item);
            if (obj is not null)
            {
                obj.BindingContext = new PlayListViewModel(item);

                Content.Children.Clear();
                Content.Children.Add((ContentView)obj);

                if (!((IFlyoutPageController)this).ShouldShowSplitMode)
                    IsPresented = false;
            }
        } else
        {
            Content.Children.Clear();
        }
    }

    private void OnMenuSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection.FirstOrDefault() as FlyoutPageItem;
        if (item is not null)
        {
            var obj = Activator.CreateInstance(item.TargetType);
            if (obj is not null)
            {
                Content.Children.Clear();
                Content.Children.Add((ContentView)obj);
                if (!((IFlyoutPageController)this).ShouldShowSplitMode)
                    IsPresented = false;
            }
        }
    }
}