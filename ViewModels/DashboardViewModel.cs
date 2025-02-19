using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IWorkoutService _workoutService;
    public DashboardViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        Workouts = new ObservableCollection<Workout>();
        LoadWorkoutsCommand = new Command(async () => await LoadWorkoutsAsync());
        SelectedDate = DateTime.Today;
    }

    public ObservableCollection<Workout> Workouts { get; set; }

    private DateTime _selectedDate;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged();
                LoadWorkoutsCommand.Execute(null);
            }
        }
    }

    public ICommand LoadWorkoutsCommand { get; set; }

    private async Task LoadWorkoutsAsync()
    {
        IEnumerable<Workout> allWorkouts = await _workoutService.GetWorkouts();
        IEnumerable<Workout> filtered = allWorkouts.Where(w => w.StartTime.Date == SelectedDate.Date);
        Workouts.Clear();
        foreach (var w in filtered)
        {
            Workouts.Add(w);
        }
    }
}
