using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class HomePage : ContentPage
{
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
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
        {
            vm.UpdateWelcomeMessage();
            await vm.RefreshHeatMapAsync();
        }
    }
}
