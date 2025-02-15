using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class ViewWorkoutPage : ContentPage
{
    public ViewWorkoutPage(ViewWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
