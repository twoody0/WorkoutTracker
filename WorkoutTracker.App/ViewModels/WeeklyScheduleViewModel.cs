using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WeeklyScheduleViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;

    public ICommand ChangeWorkoutDayCommand { get; }
    public ICommand EditDayCommand { get; }
    public ObservableCollection<KeyValuePair<DayOfWeek, List<Workout>>> WeeklySchedule { get; } = new();

    public WeeklyScheduleViewModel(IWorkoutScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
        EditDayCommand = new Command<DayOfWeek>(EditDay);
        LoadSchedule();
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
        string selectedDay = await Application.Current.MainPage.DisplayActionSheet(
            "Move Workout To:",
            "Cancel",
            null,
            days);

        if (!string.IsNullOrWhiteSpace(selectedDay) && Enum.TryParse(selectedDay, out DayOfWeek newDay))
        {
            workout.Day = newDay;
            LoadSchedule(); // Refresh the schedule view
            await Application.Current.MainPage.DisplayAlert("Workout Moved", $"{workout.Name} is now scheduled for {newDay}.", "OK");
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
    }
}
