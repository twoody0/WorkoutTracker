using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class SignupPage : ContentPage
{
    public SignupPage(SignupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}