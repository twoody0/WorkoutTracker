using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class WeeklySchedulePage : ContentPage
{
    public WeeklySchedulePage(WeeklyScheduleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
