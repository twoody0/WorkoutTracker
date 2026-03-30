using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;
using Microsoft.Maui.Controls;

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
        TabSwipeNavigationHelper.Attach(this, "dashboard", DashboardRootLayout);
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
            "Enter your current body weight here and it will save when you close this.",
            vm.BodyWeightInputValue);

        if (result == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.WeightText))
        {
            var success = await vm.UpdateBodyWeightAsync(result.WeightText);
            if (!success)
            {
                await DisplayAlert("Invalid Weight", "Enter a valid body weight greater than 0.", "OK");
                return;
            }
        }

        if (result.NavigateToWorkoutPlans)
        {
            await Shell.Current.GoToAsync("//workout-plans");
            return;
        }
    }

    private void OnCalendarSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BindingContext is not DashboardViewModel vm)
        {
            return;
        }

        vm.SelectCalendarDay(e.CurrentSelection.FirstOrDefault() as DashboardCalendarDayItem);

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }
}
