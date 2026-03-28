using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class WeightliftingLibraryViewModel : BaseViewModel
{
    #region Fields

    private readonly IWorkoutLibraryService _libraryService;
    private string _searchText = string.Empty;
    private string _selectedMuscleGroup = string.Empty;
    private CancellationTokenSource? _searchDebounceCts;
    private int _searchRequestVersion;

    #endregion

    #region Constructor

    public WeightliftingLibraryViewModel(IWorkoutLibraryService libraryService)
    {
        _libraryService = libraryService;
        Exercises = new ObservableCollection<WeightliftingExercise>();
        MuscleGroups = new List<string>
        {
            "Chest", "Back", "Legs", "Shoulders", "Arms", "Biceps", "Triceps", "Core", "Abs"
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
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = SearchExercises();
            }
        }
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
        _searchDebounceCts?.Cancel();

        if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
        {
            Exercises.Clear();
            return;
        }

        var requestVersion = Interlocked.Increment(ref _searchRequestVersion);
        var debounceCts = new CancellationTokenSource();
        _searchDebounceCts = debounceCts;

        try
        {
            await Task.Delay(175, debounceCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var results = await _libraryService.SearchExercisesByName(
            SelectedMuscleGroup, SearchText ?? string.Empty
        );

        if (debounceCts.IsCancellationRequested || requestVersion != _searchRequestVersion)
        {
            return;
        }

        Exercises.Clear();
        foreach (var exercise in results)
        {
            Exercises.Add(exercise);
        }
    }

    #endregion
}
