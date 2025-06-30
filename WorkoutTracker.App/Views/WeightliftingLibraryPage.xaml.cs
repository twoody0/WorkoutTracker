using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class WeightliftingLibraryPage : ContentPage
{
    public WeightliftingLibraryPage(WeightliftingLibraryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
