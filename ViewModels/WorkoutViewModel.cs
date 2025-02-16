using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
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

        // Initialize the ExerciseSuggestions collection before setting SelectedMuscleGroup.
        ExerciseSuggestions = new ObservableCollection<WeightliftingExercise>();

        // Define muscle groups with a default prompt.
        MuscleGroups = new List<string> { "Select Muscle Group", "Back", "Biceps", "Chest", "Legs", "Shoulders", "Triceps", "Abs" };

        // Now set the SelectedMuscleGroup, which will use the already-initialized ExerciseSuggestions.
        SelectedMuscleGroup = MuscleGroups.First(); // "Select Muscle Group" by default

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
                // When the muscle group changes, clear the exercise search.
                ExerciseSearchQuery = string.Empty;
                ExerciseSuggestions.Clear();
            }
        }
    }

    // This property holds what the user types in to search for an exercise.
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
                // Update suggestions only if a real muscle group is selected.
                UpdateExerciseSuggestions();
            }
        }
    }

    // Suggestions to show based on the muscle group and search query.
    public ObservableCollection<WeightliftingExercise> ExerciseSuggestions { get; set; }

    // The final selected exercise name.
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    // Use strings for these fields so that the Entry boxes start empty.
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
        // Try parsing the numeric fields; if parsing fails, default to 0.
        double parsedWeight = 0;
        int parsedReps = 0;
        int parsedSets = 0;
        double.TryParse(Weight, out parsedWeight);
        int.TryParse(Reps, out parsedReps);
        int.TryParse(Sets, out parsedSets);

        var workout = new Workout
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

    // Update the suggestions based on the current muscle group and search query.
    private async void UpdateExerciseSuggestions()
    {
        if (SelectedMuscleGroup != "Select Muscle Group")
        {
            // If the query is empty, get all exercises for the group; otherwise, filter by the query.
            var exercises = await _workoutLibraryService.SearchExercisesByName(SelectedMuscleGroup, ExerciseSearchQuery);
            // Sort the results alphabetically by exercise name.
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

    // Command to handle when an exercise suggestion is selected.
    public ICommand SelectExerciseCommand => new Command<WeightliftingExercise>(exercise =>
    {
        if (exercise != null)
        {
            Name = exercise.Name;
            // Optionally, set the search query to the full exercise name and clear suggestions.
            ExerciseSearchQuery = exercise.Name;
            ExerciseSuggestions.Clear();
        }
    });
}
