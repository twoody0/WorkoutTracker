using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels
{
    public class WeightliftingLibraryViewModel : BaseViewModel
    {
        private readonly IWorkoutLibraryService _libraryService;
        public WeightliftingLibraryViewModel(IWorkoutLibraryService libraryService)
        {
            _libraryService = libraryService;
            Exercises = new ObservableCollection<WeightliftingExercise>();
        }

        public ObservableCollection<WeightliftingExercise> Exercises { get; set; }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public ICommand SearchCommand => new Command(async () => await SearchExercises());

        private async Task SearchExercises()
        {
            var results = await _libraryService.SearchExercises(SearchText);
            Exercises.Clear();
            foreach (var exercise in results)
                Exercises.Add(exercise);
        }
    }
}
