using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WorkoutViewModel : BaseViewModel
{
    private readonly IWorkoutService _workoutService;
    private readonly IWorkoutLibraryService _workoutLibraryService;

    public WorkoutViewModel(IWorkoutService workoutService, IWorkoutLibraryService workoutLibraryService)
    {
        _workoutService = workoutService;
        _workoutLibraryService = workoutLibraryService;

        // Initialize the suggestions collection first.
        ExerciseSuggestions = new ObservableCollection<WeightliftingExercise>();

        // Define muscle groups with a default prompt.
        MuscleGroups = new List<string> { "Back", "Biceps", "Chest", "Legs", "Shoulders", "Triceps", "Abs" };

        // Initialize numeric fields as empty strings.
        Weight = string.Empty;
        Reps = string.Empty;
        Sets = string.Empty;
    }

    // List of muscle groups.
    public List<string> MuscleGroups { get; set; }

    private string _selectedMuscleGroup;
    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            if (_selectedMuscleGroup != value)
            {
                _selectedMuscleGroup = value;
                OnPropertyChanged();
                // When the muscle group changes, clear the search.
                ExerciseSearchQuery = string.Empty;
                ExerciseSuggestions.Clear();
            }
        }
    }

    // Holds what the user types to search for an exercise.
    private string _exerciseSearchQuery;
    public string ExerciseSearchQuery
    {
        get => _exerciseSearchQuery;
        set
        {
            if (_exerciseSearchQuery != value)
            {
                _exerciseSearchQuery = value;
                OnPropertyChanged();
                // Fire and forget (or await if you make the setter async) update.
                _ = UpdateExerciseSuggestionsAsync();
            }
        }
    }

    // Controls the visibility of the suggestions list.
    private bool _isNameFieldFocused;
    public bool IsNameFieldFocused
    {
        get => _isNameFieldFocused;
        set { _isNameFieldFocused = value; OnPropertyChanged(); }
    }

    // Collection for suggestions.
    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; set; }

    // Final selected exercise name.
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    // Numeric fields as strings.
    private string _weight;
    public string Weight
    {
        get => _weight;
        set { _weight = value; OnPropertyChanged(); }
    }

    private string _reps;
    public string Reps
    {
        get => _reps;
        set { _reps = value; OnPropertyChanged(); }
    }

    private string _sets;
    public string Sets
    {
        get => _sets;
        set { _sets = value; OnPropertyChanged(); }
    }

    // Command to add the workout.
    public ICommand AddWorkoutCommand => new Command(async () => await AddWorkoutAsync());

    private async Task AddWorkoutAsync()
    {
        // Validate that a muscle group is selected.
        if (SelectedMuscleGroup == "Select Muscle Group")
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please select a muscle group.", "OK");
            return;
        }

        // Validate that an exercise name is entered.
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter an exercise name.", "OK");
            return;
        }

        // Validate that reps are entered.
        if (string.IsNullOrWhiteSpace(Reps))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter the number of reps.", "OK");
            return;
        }

        // Validate that sets are entered.
        if (string.IsNullOrWhiteSpace(Sets))
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please enter the number of sets.", "OK");
            return;
        }

        // If no weight is entered, default it to "0"
        if (string.IsNullOrWhiteSpace(Weight))
        {
            Weight = "0";
        }

        // Parse the numeric fields (if parsing fails, TryParse leaves the values as 0).
        double parsedWeight = 0;
        int parsedReps = 0;
        int parsedSets = 0;
        double.TryParse(Weight, out parsedWeight);
        int.TryParse(Reps, out parsedReps);
        int.TryParse(Sets, out parsedSets);

        // Create the workout.
        Workout workout = new Workout
        {
            Name = Name,
            Weight = parsedWeight,
            Reps = parsedReps,
            Sets = parsedSets,
            StartTime = DateTime.Now,
            Type = WorkoutType.WeightLifting
        };

        await _workoutService.AddWorkout(workout);

        // Clear fields after adding.
        Name = string.Empty;
        ExerciseSearchQuery = string.Empty;
        Weight = string.Empty;
        Reps = string.Empty;
        Sets = string.Empty;
        ExerciseSuggestions.Clear();
    }


    // Update suggestions: if the search query is empty, show all exercises in alphabetical order.
    public async Task UpdateExerciseSuggestionsAsync()
    {
        if (SelectedMuscleGroup != "Select Muscle Group")
        {
            IEnumerable<WeightliftingExercise> exercises = await _workoutLibraryService.SearchExercisesByName(SelectedMuscleGroup, ExerciseSearchQuery);
            IOrderedEnumerable<WeightliftingExercise> sorted = exercises.OrderBy(e => e.Name);

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

    // Command to handle when a suggestion is selected.
    public ICommand SelectExerciseCommand => new Command<WeightliftingExercise>(exercise =>
    {
        if (exercise != null)
        {
            Name = exercise.Name;
            ExerciseSearchQuery = exercise.Name;
            ExerciseSuggestions.Clear();
        }
    });
}
