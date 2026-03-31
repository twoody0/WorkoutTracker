using System.Collections.ObjectModel;
using WorkoutTracker.Helpers;
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

    private void RepsEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        ClampEntryText(sender, e.NewTextValue, InputSanitizer.MaxReps, isDecimal: false);
    }

    private void SetsEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        ClampEntryText(sender, e.NewTextValue, InputSanitizer.MaxSets, isDecimal: false);
    }

    private void DurationEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        ClampEntryText(sender, e.NewTextValue, InputSanitizer.MaxDurationMinutes, isDecimal: false);
    }

    private void DistanceEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        ClampEntryText(sender, e.NewTextValue, InputSanitizer.MaxDistanceMiles, isDecimal: true);
    }

    private void StepsEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        ClampEntryText(sender, e.NewTextValue, InputSanitizer.MaxSteps, isDecimal: false);
    }

    private static void ClampEntryText(object sender, string? newTextValue, double maxValue, bool isDecimal)
    {
        if (sender is not Entry entry)
        {
            return;
        }

        var sanitized = isDecimal
            ? InputSanitizer.SanitizePositiveDecimalText(newTextValue, maxValue)
            : InputSanitizer.SanitizePositiveIntegerText(newTextValue, (int)maxValue);

        if (!string.Equals(entry.Text, sanitized, StringComparison.Ordinal))
        {
            entry.Text = sanitized;
        }
    }
}
