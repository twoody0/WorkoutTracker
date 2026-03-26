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

    public int WorkoutCount => Workouts.Count;

    public int StrengthWorkoutCount => Workouts.Count(workout => workout.Type == WorkoutType.WeightLifting);

    public int CardioWorkoutCount => Workouts.Count(workout => workout.Type == WorkoutType.Cardio);

    public string DaySummary => WorkoutCount == 0
        ? "No sessions planned yet. Add a workout to build out this day."
        : $"{WorkoutCount} session{(WorkoutCount == 1 ? string.Empty : "s")} planned for {Day}.";

    public string EmptyStateTitle => $"{Day} is open";

    public string EmptyStateMessage => "Start with a suggested lift or cardio session and shape the day from there.";

    public EditDayViewModel(DayOfWeek day, IWorkoutScheduleService scheduleService)
    {
        Day = day;
        _scheduleService = scheduleService;
        Workouts = new ObservableCollection<Workout>(_scheduleService.GetWeeklySchedule()[day]);
        Workouts.CollectionChanged += (_, _) => NotifyOverviewChanged();

        MoveWorkoutCommand = new Command<Workout>(MoveWorkout);
        RemoveWorkoutCommand = new Command<Workout>(RemoveWorkout);
        AddWorkoutCommand = new Command(AddWorkout);
    }

    private async void MoveWorkout(Workout workout)
    {
        var days = Enum.GetNames(typeof(DayOfWeek));
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
            return;

        string selectedDay = await page.DisplayActionSheet("Move To:", "Cancel", null, days);

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
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page != null)
            await page.Navigation.PushAsync(addPage);
    }

    public void NotifyOverviewChanged()
    {
        OnPropertyChanged(nameof(WorkoutCount));
        OnPropertyChanged(nameof(StrengthWorkoutCount));
        OnPropertyChanged(nameof(CardioWorkoutCount));
        OnPropertyChanged(nameof(DaySummary));
    }
}
