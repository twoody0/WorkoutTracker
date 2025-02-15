namespace WorkoutTracker.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private string _title;
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public HomeViewModel()
    {
        Title = "Welcome to Workout Tracker!";
    }
}
