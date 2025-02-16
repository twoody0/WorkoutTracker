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
}
