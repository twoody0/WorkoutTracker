using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;

namespace WorkoutTracker.Views;

public partial class LeaderboardPage : ContentPage
{
    public LeaderboardPage(LeaderboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        TabSwipeNavigationHelper.Attach(this, "leaderboard");
    }
}
