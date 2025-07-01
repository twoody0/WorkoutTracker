using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WeightliftingLibraryViewModel : BaseViewModel
{
    #region Fields

    private readonly IWorkoutLibraryService _libraryService;
    private string _searchText;
    private string _selectedMuscleGroup;

    #endregion

    #region Constructor

    public WeightliftingLibraryViewModel(IWorkoutLibraryService libraryService)
    {
        _libraryService = libraryService;
        Exercises = new ObservableCollection<WeightliftingExercise>();
        MuscleGroups = new List<string>
        {
            "Chest", "Back", "Legs", "Shoulders", "Biceps", "Triceps", "Abs"
        };
    }

    #endregion

    #region Properties

    public ObservableCollection<WeightliftingExercise> Exercises { get; set; }

    public List<string> MuscleGroups { get; set; }

    /// <summary>
    /// The search query entered by the user.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// The currently selected muscle group.
    /// </summary>
    public string SelectedMuscleGroup
    {
        get => _selectedMuscleGroup;
        set
        {
            if (SetProperty(ref _selectedMuscleGroup, value))
            {
                _ = SearchExercises();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SearchCommand => new Command(async () => await SearchExercises());

    #endregion

    #region Private Methods

    private async Task SearchExercises()
    {
        if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            Exercises.Clear();
            return;
        }

        var results = await _libraryService.SearchExercisesByName(
            SelectedMuscleGroup, SearchText ?? string.Empty
        );

        Exercises.Clear();
        foreach (var exercise in results)
        {
            Exercises.Add(exercise);
        }
    }

    #endregion
}
