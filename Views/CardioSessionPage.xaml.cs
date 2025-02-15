using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class CardioSessionPage : ContentPage
{
    public CardioSessionPage(CardioWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
