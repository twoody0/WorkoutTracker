using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;

namespace WorkoutTracker.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        RefreshSwipeTargets();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshSwipeTargets();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () => TabSwipeNavigationHelper.Refresh(this));
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () => TabSwipeNavigationHelper.Refresh(this));
    }

    private void RefreshSwipeTargets()
    {
        TabSwipeNavigationHelper.Attach(this, "dashboard", DashboardRootLayout, DashboardDatePicker, WorkoutHistoryList);
        TabSwipeNavigationHelper.Refresh(this);
    }
}
