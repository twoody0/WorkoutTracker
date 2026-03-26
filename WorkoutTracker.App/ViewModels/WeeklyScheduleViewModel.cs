using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WeeklyScheduleViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;
    private string? _lastCompletedPlanPromptKey;

    public ICommand ChangeWorkoutDayCommand { get; }
    public ICommand EditDayCommand { get; }
    public ObservableCollection<KeyValuePair<DayOfWeek, List<Workout>>> WeeklySchedule { get; } = new();
    public string ActivePlanName => _scheduleService.ActivePlan?.Name ?? "No active plan";
    public string ActivePlanTimelineSummary => _scheduleService.GetActivePlanTimelineSummary();
    public bool HasActivePlan => _scheduleService.ActivePlan != null;

    public WeeklyScheduleViewModel(IWorkoutScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
        ChangeWorkoutDayCommand = new Command<Workout>(ChangeWorkoutDay);
        EditDayCommand = new Command<DayOfWeek>(EditDay);
        LoadSchedule();
    }

    public async Task OnAppearingAsync()
    {
        LoadSchedule();
        await PromptForCompletedPlanAsync();
    }

    private async void EditDay(DayOfWeek day)
    {
        var editPage = new EditDayPage(day, _scheduleService);
        await Shell.Current.Navigation.PushAsync(editPage);
    }

    private async void ChangeWorkoutDay(Workout workout)
    {
        if (workout == null) return;

        var days = Enum.GetNames(typeof(DayOfWeek));
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
            return;

        string selectedDay = await page.DisplayActionSheet(
            "Move Workout To:",
            "Cancel",
            null,
            days);

        if (!string.IsNullOrWhiteSpace(selectedDay) && Enum.TryParse(selectedDay, out DayOfWeek newDay))
        {
            workout.Day = newDay;
            LoadSchedule(); // Refresh the schedule view
            await page.DisplayAlert("Workout Moved", $"{workout.Name} is now scheduled for {newDay}.", "OK");
        }
    }

    private void LoadSchedule()
    {
        WeeklySchedule.Clear();

        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            var workouts = _scheduleService.GetWeeklySchedule().ContainsKey(day)
                ? _scheduleService.GetWeeklySchedule()[day]
                : new List<Workout>();

            WeeklySchedule.Add(new KeyValuePair<DayOfWeek, List<Workout>>(day, workouts));
        }

        OnPropertyChanged(nameof(ActivePlanName));
        OnPropertyChanged(nameof(ActivePlanTimelineSummary));
        OnPropertyChanged(nameof(HasActivePlan));
    }

    private async Task PromptForCompletedPlanAsync()
    {
        if (!_scheduleService.HasCompletedActivePlan || _scheduleService.ActivePlan == null)
        {
            return;
        }

        var promptKey = $"{_scheduleService.ActivePlan.Name}:{_scheduleService.ActivePlanStartedOn:O}";
        if (string.Equals(_lastCompletedPlanPromptKey, promptKey, StringComparison.Ordinal))
        {
            return;
        }

        _lastCompletedPlanPromptKey = promptKey;

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return;
        }

        var suggestedPlan = _scheduleService.GetSuggestedNextPlan();
        var suggestionOption = suggestedPlan == null ? null : $"Try Suggested Plan: {suggestedPlan.Name}";

        var action = await page.DisplayActionSheet(
            "Workout Plan Complete",
            "Maybe Later",
            null,
            "Restart Current Plan",
            "Choose a New Plan",
            suggestionOption);

        if (action == "Restart Current Plan")
        {
            _scheduleService.RestartActivePlan();
            LoadSchedule();
            await page.DisplayAlert("Plan Restarted", $"'{_scheduleService.ActivePlan?.Name}' has been restarted.", "OK");
            return;
        }

        if (action == "Choose a New Plan")
        {
            var workoutPlanPage = App.Services.GetRequiredService<WorkoutPlanPage>();
            await Shell.Current.Navigation.PushAsync(workoutPlanPage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(suggestionOption) && action == suggestionOption && suggestedPlan != null)
        {
            _scheduleService.AddPlanToWeeklySchedule(suggestedPlan);
            LoadSchedule();
            await page.DisplayAlert(
                "Suggested Plan Started",
                $"You're now on '{suggestedPlan.Name}', a follow-up to the plan you completed.",
                "OK");
        }
    }
}
