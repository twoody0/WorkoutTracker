using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
        BindingContext = new HomeViewModel();
    }
}