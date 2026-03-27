using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class EditDayPage : ContentPage
{
    public EditDayViewModel ViewModel { get; }

    public EditDayPage(DayOfWeek day, IWorkoutScheduleService scheduleService)
    {
        InitializeComponent();
        ViewModel = new EditDayViewModel(day, scheduleService);
        BindingContext = ViewModel;
    }

    public EditDayPage(DayOfWeek day, WorkoutPlan plan, IWorkoutScheduleService scheduleService)
    {
        InitializeComponent();
        ViewModel = new EditDayViewModel(day, plan, scheduleService);
        BindingContext = ViewModel;
    }
}
