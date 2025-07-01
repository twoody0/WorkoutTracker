using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Helpers;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WorkoutViewModel : BaseViewModel
{
    #region Fields

    private readonly IWorkoutService _workoutService;
    private readonly IWorkoutLibraryService _workoutLibraryService;

    private string _selectedMuscleGroup = string.Empty;
    private string _exerciseSearchQuery = string.Empty;
    private bool _isNameFieldFocused;
    private bool _hasWorkouts;
    private string _name = string.Empty;
    private string _weight = string.Empty;
    private string _reps = string.Empty;
    private string _sets = string.Empty;

    #endregion

    #region Constructor

    public WorkoutViewModel(IWorkoutService workoutService, IWorkoutLibraryService workoutLibraryService)
    {
        _workoutService = workoutService;
        _workoutLibraryService = workoutLibraryService;

        MuscleGroups = new List<string> { "Back", "Biceps", "Chest", "Legs", "Shoulders", "Triceps", "Abs" };
        ExerciseSuggestions = new ObservableCollection<WeightliftingExercise>();

        Weight = string.Empty;
        Reps = string.Empty;
        Sets = string.Empty;

        _ = CheckForExistingWorkouts();

        // Preload template if one exists
        if (WorkoutTemplateCache.Template is Workout workout)
        {
            Name = workout.Name;
            ExerciseSearchQuery = workout.Name;
            Weight = workout.Weight.ToString();
            Reps = workout.Reps.ToString();
            Sets = workout.Sets.ToString();
            SelectedMuscleGroup = workout.MuscleGroup;

            _ = UpdateExerciseSuggestionsAsync();
            WorkoutTemplateCache.Template = null;
        }
    }

    #endregion

    #region Properties

    public List<string> MuscleGroups { get; }

    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; }

    public bool HasWorkouts
    {
        get => _hasWorkouts;
        set => SetProperty(ref _hasWorkouts, value);
    }

    public bool IsNameFieldFocused
    {
        get => _isNameFieldFocused;
        set => SetProperty(ref _isNameFieldFocused, value);
    }

    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            if (SetProperty(ref _selectedMuscleGroup, value))
            {
                ExerciseSearchQuery = string.Empty;
                ExerciseSuggestions.Clear();
            }
        }
    }

    public string ExerciseSearchQuery
    {
        get => _exerciseSearchQuery;
        set
        {
            if (SetProperty(ref _exerciseSearchQuery, value))
                _ = UpdateExerciseSuggestionsAsync();
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public string Reps
    {
        get => _reps;
        set => SetProperty(ref _reps, value);
    }

    public string Sets
    {
        get => _sets;
        set => SetProperty(ref _sets, value);
    }

    #endregion

    #region Commands

    public ICommand AddWorkoutCommand => new Command(async () => await AddWorkoutAsync());

    public ICommand SelectExerciseCommand => new Command<WeightliftingExercise>(exercise =>
    {
        if (exercise != null)
        {
            Name = exercise.Name;
            ExerciseSearchQuery = exercise.Name;
            ExerciseSuggestions.Clear();
        }
    });

    public ICommand NavigateToViewWorkoutsCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("///ViewWorkoutPage");
    });

    #endregion

    #region Private Methods

    private async Task CheckForExistingWorkouts()
    {
        var all = await _workoutService.GetWorkouts();
        HasWorkouts = all.Any();
    }

    private async Task AddWorkoutAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            await ShowError("Please select a muscle group.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            await ShowError("Please enter an exercise name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Reps))
        {
            await ShowError("Please enter the number of reps.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Sets))
        {
            await ShowError("Please enter the number of sets.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Weight))
            Weight = "0";

        double.TryParse(Weight, out double parsedWeight);
        int.TryParse(Reps, out int parsedReps);
        int.TryParse(Sets, out int parsedSets);

        var workout = new Workout(
            name: Name,
            weight: parsedWeight,
            reps: parsedReps,
            sets: parsedSets,
            muscleGroup: SelectedMuscleGroup,
            startTime: DateTime.Now,
            type: WorkoutType.WeightLifting,
            gymLocation: "Default Gym"
        );

        await _workoutService.AddWorkout(workout);
        HasWorkouts = true;

        Name = ExerciseSearchQuery = Weight = Reps = Sets = string.Empty;
        ExerciseSuggestions.Clear();
    }

    private async Task ShowError(string message)
    {
        var currentWindow = Application.Current?.Windows.FirstOrDefault();
        if (currentWindow?.Page is Page currentPage)
        {
            await currentPage.DisplayAlert("Error", message, "OK");
        }
    }

    #endregion

    #region Public Methods

    public async Task UpdateExerciseSuggestionsAsync()
    {
        if (!string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            var exercises = await _workoutLibraryService.SearchExercisesByName(
                SelectedMuscleGroup, ExerciseSearchQuery
            );

            var sorted = exercises.OrderBy(e => e.Name);
            ExerciseSuggestions.Clear();
            foreach (var ex in sorted)
            {
                ExerciseSuggestions.Add(ex);
            }
        }
        else
        {
            ExerciseSuggestions.Clear();
        }
    }

    #endregion
}
