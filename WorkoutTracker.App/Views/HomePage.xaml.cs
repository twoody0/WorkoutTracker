using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class HomePage : ContentPage
{
    // Use constructor injection
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
        {
            vm.UpdateWelcomeMessage();
        }
    }
}
