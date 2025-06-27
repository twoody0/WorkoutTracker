using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WeightliftingLibraryViewModel : BaseViewModel
{
    public List<string> MuscleGroups { get; set; }

    private readonly IWorkoutLibraryService _libraryService;
    public WeightliftingLibraryViewModel(IWorkoutLibraryService libraryService)
    {
        _libraryService = libraryService;
        Exercises = new ObservableCollection<WeightliftingExercise>();
        MuscleGroups = new List<string> { "Chest", "Back", "Legs", "Shoulders", "Biceps", "Triceps", "Abs" };
    }

    public ObservableCollection<WeightliftingExercise> Exercises { get; set; }

    // The text entered by the user for searching.
    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); }
    }

    // The selected muscle group. This can be bound to a Picker.
    private string _selectedMuscleGroup;
    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            _selectedMuscleGroup = value;
            OnPropertyChanged();
            _ = SearchExercises();
        }
    }

    // Command to perform the search.
    public ICommand SearchCommand => new Command(async () => await SearchExercises());

    private async Task SearchExercises()
    {
        // If no muscle group is selected, clear and exit
        if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            Exercises.Clear();
            return;
        }

        // Always fetch exercises for the selected muscle group
        IEnumerable<WeightliftingExercise> results =
            await _libraryService.SearchExercisesByName(SelectedMuscleGroup, SearchText ?? string.Empty);

        Exercises.Clear();
        foreach (var exercise in results)
        {
            Exercises.Add(exercise);
        }
    }
}
