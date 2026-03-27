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
                    "Enter your body weight in pounds so workout calculations can stay accurate.",
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
                "Update your current body weight in pounds.",
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
            useCurrentWeightAsInitialValue ? vm.BodyWeightInputValue : string.Empty);

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
