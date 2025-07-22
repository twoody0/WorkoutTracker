using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;

public class EditDayViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;

    public DayOfWeek Day { get; }
    public ObservableCollection<Workout> Workouts { get; }

    public ICommand MoveWorkoutCommand { get; }
    public ICommand RemoveWorkoutCommand { get; }
    public ICommand AddWorkoutCommand { get; }

    public EditDayViewModel(DayOfWeek day, IWorkoutScheduleService scheduleService)
    {
        Day = day;
        _scheduleService = scheduleService;
        Workouts = new ObservableCollection<Workout>(_scheduleService.GetWeeklySchedule()[day]);

        MoveWorkoutCommand = new Command<Workout>(MoveWorkout);
        RemoveWorkoutCommand = new Command<Workout>(RemoveWorkout);
        AddWorkoutCommand = new Command(AddWorkout);
    }

    private async void MoveWorkout(Workout workout)
    {
        var days = Enum.GetNames(typeof(DayOfWeek));
        string selectedDay = await Application.Current.MainPage.DisplayActionSheet("Move To:", "Cancel", null, days);

        if (!string.IsNullOrWhiteSpace(selectedDay) && Enum.TryParse(selectedDay, out DayOfWeek newDay))
        {
            workout.Day = newDay;
            _scheduleService.AddWorkoutToDay(newDay, workout);
            Workouts.Remove(workout);
        }
    }

    private void RemoveWorkout(Workout workout)
    {
        _scheduleService.RemoveWorkoutFromDay(Day, workout);
        Workouts.Remove(workout);
    }

    private async void AddWorkout()
    {
        // TODO: Open a picker for user to choose from all exercises
        await Application.Current.MainPage.DisplayAlert("Add Workout", "This feature is coming soon!", "OK");
    }
}
