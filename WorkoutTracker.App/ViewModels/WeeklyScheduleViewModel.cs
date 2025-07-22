using System.Collections.ObjectModel;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WeeklyScheduleViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;

    public ObservableCollection<KeyValuePair<DayOfWeek, List<Workout>>> WeeklySchedule { get; } = new();

    public WeeklyScheduleViewModel(IWorkoutScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
        LoadSchedule();
    }

    private void LoadSchedule()
    {
        WeeklySchedule.Clear();
        foreach (var day in _scheduleService.GetWeeklySchedule())
        {
            WeeklySchedule.Add(day);
        }
    }
}
