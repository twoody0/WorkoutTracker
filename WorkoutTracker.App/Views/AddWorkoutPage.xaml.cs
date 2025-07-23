using System.Collections.ObjectModel;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class AddWorkoutPage : ContentPage
{
    public AddWorkoutViewModel ViewModel { get; }

    public AddWorkoutPage(DayOfWeek day, IWorkoutScheduleService scheduleService, ObservableCollection<Workout> workouts)
    {
        InitializeComponent();
        ViewModel = new AddWorkoutViewModel(day, scheduleService, workouts, Navigation);
        BindingContext = ViewModel;
    }
}
