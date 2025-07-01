using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Helpers;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for managing workout creation and interaction with the workout library.
/// </summary>
public class WorkoutViewModel : BaseViewModel
{
    // ─────────────────────────────────────────────────────────────
    // Private Fields
    // ─────────────────────────────────────────────────────────────

    private readonly IWorkoutService _workoutService;
    private readonly IWorkoutLibraryService _workoutLibraryService;

    private string _selectedMuscleGroup;
    private string _exerciseSearchQuery;
    private bool _isNameFieldFocused;
    private bool _hasWorkouts;
    private string _name;
    private string _weight;
    private string _reps;
    private string _sets;

    // ─────────────────────────────────────────────────────────────
    // Public Properties
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// List of muscle groups for the dropdown menu.
    /// </summary>
    public List<string> MuscleGroups { get; set; }

    /// <summary>
    /// Collection of exercises that match the user's search.
    /// </summary>
    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; set; }

    /// <summary>
    /// Indicates whether there are any saved workouts.
    /// </summary>
    public bool HasWorkouts
    {
        get => _hasWorkouts;
        set { _hasWorkouts = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Tracks whether the exercise name field is focused, to control suggestion visibility.
    /// </summary>
    public bool IsNameFieldFocused
    {
        get => _isNameFieldFocused;
        set { _isNameFieldFocused = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The selected muscle group from the dropdown.
    /// </summary>
    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            if (_selectedMuscleGroup != value)
            {
                _selectedMuscleGroup = value;
                OnPropertyChanged();

                // Clear search input and suggestions when the group changes.
                ExerciseSearchQuery = string.Empty;
                ExerciseSuggestions.Clear();
            }
        }
    }

    /// <summary>
    /// Search input from the user for finding exercises.
    /// </summary>
    public string ExerciseSearchQuery
    {
        get => _exerciseSearchQuery;
        set
        {
            if (_exerciseSearchQuery != value)
            {
                _exerciseSearchQuery = value;
                OnPropertyChanged();
                _ = UpdateExerciseSuggestionsAsync(); // Fire-and-forget update
            }
        }
    }

    /// <summary>
    /// Name of the selected or entered exercise.
    /// </summary>
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Entered weight value for the workout.
    /// </summary>
    public string Weight
    {
        get => _weight;
        set { _weight = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Entered reps value for the workout.
    /// </summary>
    public string Reps
    {
        get => _reps;
        set { _reps = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Entered sets value for the workout.
    /// </summary>
    public string Sets
    {
        get => _sets;
        set { _sets = value; OnPropertyChanged(); }
    }

    // ─────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Command for adding a new workout to the user's saved workouts.
    /// </summary>
    public ICommand AddWorkoutCommand => new Command(async () => await AddWorkoutAsync());

    /// <summary>
    /// Command executed when a suggested exercise is selected.
    /// </summary>
    public ICommand SelectExerciseCommand => new Command<WeightliftingExercise>(exercise =>
    {
        if (exercise != null)
        {
            Name = exercise.Name;
            ExerciseSearchQuery = exercise.Name;
            ExerciseSuggestions.Clear();
        }
    });

    /// <summary>
    /// Command to navigate to the page that displays existing workouts.
    /// </summary>
    public ICommand NavigateToViewWorkoutsCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("///ViewWorkoutPage");
    });

    // ─────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────

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

        // If a workout template was previously saved, preload it.
        if (WorkoutTemplateCache.Template != null)
        {
            var workout = WorkoutTemplateCache.Template;
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

    // ─────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks if the user has any saved workouts.
    /// </summary>
    private async Task CheckForExistingWorkouts()
    {
        var all = await _workoutService.GetWorkouts();
        HasWorkouts = all.Any();
    }

    /// <summary>
    /// Attempts to infer a muscle group based on keywords in the exercise name.
    /// </summary>
    private string InferMuscleGroupFromName(string name)
    {
        var lower = name.ToLower();
        if (lower.Contains("chest")) return "Chest";
        if (lower.Contains("leg")) return "Legs";
        if (lower.Contains("back")) return "Back";
        if (lower.Contains("tricep")) return "Triceps";
        if (lower.Contains("bicep")) return "Biceps";
        if (lower.Contains("shoulder")) return "Shoulders";
        if (lower.Contains("abs") || lower.Contains("core")) return "Abs";
        return string.Empty;
    }

    /// <summary>
    /// Adds a workout to the saved list, after validating input.
    /// </summary>
    private async Task AddWorkoutAsync()
    {
        if (SelectedMuscleGroup == "Select Muscle Group")
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please select a muscle group.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter an exercise name.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Reps))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter the number of reps.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Sets))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter the number of sets.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Weight))
        {
            Weight = "0";
        }

        double.TryParse(Weight, out double parsedWeight);
        int.TryParse(Reps, out int parsedReps);
        int.TryParse(Sets, out int parsedSets);

        // Construct the workout using the constructor.
        var workout = new Workout(
            name: Name,
            weight: parsedWeight,
            reps: parsedReps,
            sets: parsedSets,
            muscleGroup: SelectedMuscleGroup,
            startTime: DateTime.Now,
            type: WorkoutType.WeightLifting,
            gymLocation: "Default Gym" // Replace with real location if available
        );

        await _workoutService.AddWorkout(workout);
        HasWorkouts = true;

        // Clear form fields after submission.
        Name = string.Empty;
        ExerciseSearchQuery = string.Empty;
        Weight = string.Empty;
        Reps = string.Empty;
        Sets = string.Empty;
        ExerciseSuggestions.Clear();
    }

    /// <summary>
    /// Updates the list of exercise suggestions based on the search query and selected muscle group.
    /// </summary>
    public async Task UpdateExerciseSuggestionsAsync()
    {
        if (SelectedMuscleGroup != "Select Muscle Group")
        {
            IEnumerable<WeightliftingExercise> exercises = await _workoutLibraryService.SearchExercisesByName(SelectedMuscleGroup, ExerciseSearchQuery);
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
}
