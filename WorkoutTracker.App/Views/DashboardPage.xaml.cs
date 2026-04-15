using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;
using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace WorkoutTracker.Views;

public partial class DashboardPage : ContentPage
{
    private DashboardViewModel? _viewModel;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        AttachViewModel(vm);
        RefreshSwipeTargets();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DashboardViewModel vm)
        {
            vm.LoadWorkoutsCommand.Execute(null);
            UpdateCalendarBackgroundBlur(vm.IsCalendarExpanded);
        }
        RefreshSwipeTargets();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () => TabSwipeNavigationHelper.Refresh(this));
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () => TabSwipeNavigationHelper.Refresh(this));
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (ReferenceEquals(_viewModel, BindingContext))
        {
            return;
        }

        DetachViewModel();

        if (BindingContext is DashboardViewModel vm)
        {
            AttachViewModel(vm);
            UpdateCalendarBackgroundBlur(vm.IsCalendarExpanded);
        }
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

    private async void OnWorkoutReminderToggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is not DashboardViewModel vm)
        {
            return;
        }

        if (sender is not Switch reminderSwitch)
        {
            return;
        }

        if (e.Value)
        {
            var enable = await DisplayAlert(
                "Workout Reminders",
                "Turn on friendly workout reminders? We'll use your usual workout time and only remind you on planned workout days if you still have not logged a workout.",
                "Turn On",
                "Cancel");

            if (!enable)
            {
                reminderSwitch.Toggled -= OnWorkoutReminderToggled;
                reminderSwitch.IsToggled = false;
                reminderSwitch.Toggled += OnWorkoutReminderToggled;
                return;
            }
        }

        await vm.SetWorkoutReminderEnabledAsync(e.Value);

        if (e.Value && !vm.IsWorkoutReminderEnabled)
        {
            await DisplayAlert(
                "Notifications Off",
                "Workout reminders stayed off because notification permission was not granted.",
                "OK");

            reminderSwitch.Toggled -= OnWorkoutReminderToggled;
            reminderSwitch.IsToggled = false;
            reminderSwitch.Toggled += OnWorkoutReminderToggled;
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

    private void AttachViewModel(DashboardViewModel vm)
    {
        _viewModel = vm;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void DetachViewModel()
    {
        if (_viewModel == null)
        {
            return;
        }

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.IsCalendarExpanded) &&
            sender is DashboardViewModel vm)
        {
            UpdateCalendarBackgroundBlur(vm.IsCalendarExpanded);
        }
    }

    partial void UpdateCalendarBackgroundBlur(bool isEnabled);
}
