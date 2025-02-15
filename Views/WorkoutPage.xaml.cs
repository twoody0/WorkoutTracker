using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class WorkoutPage : ContentPage
{
    public WorkoutPage(WorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
