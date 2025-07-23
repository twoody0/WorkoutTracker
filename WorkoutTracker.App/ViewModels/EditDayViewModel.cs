using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Views;

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
            _scheduleService.RemoveWorkoutFromDay(Day, workout);
            workout.Day = newDay;
            _scheduleService.AddWorkoutToDay(newDay, workout);

            Workouts.Remove(workout);
        }
    }

    private void RemoveWorkout(Workout workout)
    {
        if (workout == null)
            return;

        // Remove from service
        _scheduleService.RemoveWorkoutFromDay(Day, workout);

        // Remove from ObservableCollection so UI updates
        Workouts.Remove(workout);
    }

    private async void AddWorkout()
    {
        var addPage = new AddWorkoutPage(Day, _scheduleService, Workouts);
        await Application.Current.MainPage.Navigation.PushAsync(addPage);
    }
}
