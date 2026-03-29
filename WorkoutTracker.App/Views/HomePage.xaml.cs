using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;

namespace WorkoutTracker.Views;

public partial class HomePage : ContentPage
{
    private bool _hasCheckedForInitialBodyWeight;

    // Use constructor injection
    public HomePage(HomeViewModel viewModel)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            var root = ex.InnerException ?? ex;
            throw new InvalidOperationException(
                $"HomePage InitializeComponent failed: {root.GetType().Name}: {root.Message}",
                root);
        }

        BindingContext = viewModel;
        TabSwipeNavigationHelper.Attach(this, "home");
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
        {
            if (!_hasCheckedForInitialBodyWeight && !vm.HasBodyWeight)
            {
                _hasCheckedForInitialBodyWeight = true;
                await PromptForBodyWeightAsync(
                    vm,
                    "Welcome to Megnor. This is where you can log workouts, follow a plan, and keep your progress moving. If you enter your body weight here, it will still save even if you go straight to Workout Plans.",
                    useCurrentWeightAsInitialValue: false);
            }

            vm.UpdateWelcomeMessage();
            await vm.RefreshHeatMapAsync();
        }
    }

    private async void OnEditBodyWeightClicked(object sender, EventArgs e)
    {
        if (BindingContext is HomeViewModel vm)
        {
            await PromptForBodyWeightAsync(
                vm,
                "Enter your current body weight here and it will save when you close this or go to Workout Plans.",
                useCurrentWeightAsInitialValue: true);
        }
    }

    private async Task PromptForBodyWeightAsync(
        HomeViewModel vm,
        string message,
        bool useCurrentWeightAsInitialValue)
    {
        var result = await BodyWeightPromptPage.ShowAsync(
            this,
            "Body Weight",
            message,
            useCurrentWeightAsInitialValue ? vm.BodyWeightInputValue : string.Empty,
            workoutPlansButtonText: "Go To Workout Plans");

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
}
