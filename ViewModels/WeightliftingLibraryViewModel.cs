using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WeightliftingLibraryViewModel : BaseViewModel
{
    private readonly IWorkoutLibraryService _libraryService;
    public WeightliftingLibraryViewModel(IWorkoutLibraryService libraryService)
    {
        _libraryService = libraryService;
        Exercises = new ObservableCollection<WeightliftingExercise>();
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
        set { _selectedMuscleGroup = value; OnPropertyChanged(); }
    }

    // Command to perform the search.
    public ICommand SearchCommand => new Command(async () => await SearchExercises());

    private async Task SearchExercises()
    {
        // If no muscle group is selected, optionally clear the list and exit.
        if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            Exercises.Clear();
            return;
        }

        // Call the new service method that requires both parameters.
        IEnumerable<WeightliftingExercise> results = await _libraryService.SearchExercisesByName(SelectedMuscleGroup, SearchText);
        Exercises.Clear();
        foreach (var exercise in results)
        {
            Exercises.Add(exercise);
        }
    }
}
