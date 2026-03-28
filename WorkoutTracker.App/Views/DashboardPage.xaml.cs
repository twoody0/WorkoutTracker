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
        if (BindingContext is DashboardViewModel vm)
        {
            vm.LoadWorkoutsCommand.Execute(null);
        }
        RefreshSwipeTargets();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () => TabSwipeNavigationHelper.Refresh(this));
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () => TabSwipeNavigationHelper.Refresh(this));
    }

    private void RefreshSwipeTargets()
    {
        TabSwipeNavigationHelper.Attach(this, "dashboard", DashboardRootLayout, DashboardDatePicker, WorkoutHistoryList);
        TabSwipeNavigationHelper.Refresh(this);
    }

    private async void OnEditBodyWeightClicked(object sender, EventArgs e)
    {
        if (BindingContext is not DashboardViewModel vm)
        {
            return;
        }

        var result = await BodyWeightPromptPage.ShowAsync(
            this,
            "Body Weight",
            "Update your current body weight in pounds.",
            vm.BodyWeightInputValue);

        if (result == null)
        {
            return;
        }

        var success = await vm.UpdateBodyWeightAsync(result);
        if (!success)
        {
            await DisplayAlert("Invalid Weight", "Enter a valid body weight greater than 0.", "OK");
        }
    }
}
