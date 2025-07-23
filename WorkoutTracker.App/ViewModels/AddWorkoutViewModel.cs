using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class AddWorkoutViewModel : BaseViewModel
{
    private readonly IWorkoutScheduleService _scheduleService;
    private readonly ObservableCollection<Workout> _workouts;
    private readonly INavigation _navigation;

    public DayOfWeek Day { get; }

    public string Name { get; set; }
    public string MuscleGroup { get; set; }
    public WorkoutType SelectedType { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int Steps { get; set; }

    public List<WorkoutType> WorkoutTypes { get; } = Enum.GetValues(typeof(WorkoutType)).Cast<WorkoutType>().ToList();

    public bool IsWeightLifting => SelectedType == WorkoutType.WeightLifting;
    public bool IsCardio => SelectedType == WorkoutType.Cardio;

    public ICommand SaveCommand { get; }

    public AddWorkoutViewModel(DayOfWeek day, IWorkoutScheduleService scheduleService, ObservableCollection<Workout> workouts, INavigation navigation)
    {
        Day = day;
        _scheduleService = scheduleService;
        _workouts = workouts;
        _navigation = navigation;

        SelectedType = WorkoutType.WeightLifting; // Default
        SaveCommand = new Command(SaveWorkout);
    }

    private async void SaveWorkout()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(MuscleGroup))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please fill in all required fields.", "OK");
            return;
        }

        var newWorkout = new Workout(
            name: Name,
            weight: 0, // User can edit later in EditDayPage
            reps: Reps,
            sets: Sets,
            muscleGroup: MuscleGroup,
            day: Day,
            startTime: DateTime.Now,
            type: SelectedType,
            gymLocation: string.Empty // We don't care about GymLocation
        );

        if (SelectedType == WorkoutType.Cardio)
        {
            newWorkout.Steps = Steps;
        }

        // Add to WeeklySchedule service
        _scheduleService.AddWorkoutToDay(Day, newWorkout);

        // Add to EditDayPage ObservableCollection so UI updates live
        _workouts.Add(newWorkout);

        // Go back to EditDayPage
        await _navigation.PopAsync();
    }

}

