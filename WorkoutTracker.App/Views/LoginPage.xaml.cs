using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(SignupViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}