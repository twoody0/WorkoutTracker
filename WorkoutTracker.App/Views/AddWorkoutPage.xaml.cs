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
        ViewModel = new AddWorkoutViewModel(
            day,
            scheduleService,
            App.Services.GetRequiredService<IWorkoutLibraryService>(),
            workouts,
            Navigation);
        BindingContext = ViewModel;
        ViewModel.InitializeDefaultRecommendation();
    }

    public AddWorkoutPage(DayOfWeek day, WorkoutPlan plan, ObservableCollection<Workout> workouts)
    {
        InitializeComponent();
        var workoutPlanService = App.Services.GetRequiredService<IWorkoutPlanService>();
        ViewModel = new AddWorkoutViewModel(
            day,
            App.Services.GetRequiredService<IWorkoutLibraryService>(),
            workouts,
            Navigation,
            plan.Workouts.Where(workout => workout.Day == day),
            plan.Name,
            workout =>
            {
                plan.Workouts.Add(workout);
                workoutPlanService.SavePlans();
            });
        BindingContext = ViewModel;
        ViewModel.InitializeDefaultRecommendation();
    }
}
