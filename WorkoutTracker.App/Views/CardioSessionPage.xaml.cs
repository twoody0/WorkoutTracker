using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class CardioSessionPage : ContentPage
{
    public CardioSessionPage(CardioWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CardioWorkoutViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.SessionName))
            {
                vm.IsSessionNameFocused = true;
                vm.ShowAllCardioNameSuggestions();
            }

            await vm.PrepareStepTrackingAsync();
        }
    }

    private void SessionNameEntry_Focused(object sender, FocusEventArgs e)
    {
        if (BindingContext is CardioWorkoutViewModel vm)
        {
            vm.IsSessionNameFocused = true;
            vm.ShowAllCardioNameSuggestions();
        }
    }

    private void SessionNameEntry_Unfocused(object sender, FocusEventArgs e)
    {
        if (BindingContext is CardioWorkoutViewModel vm)
        {
            vm.IsSessionNameFocused = false;
        }
    }

    private void OnCardioNameSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is string name &&
            BindingContext is CardioWorkoutViewModel vm)
        {
            vm.SelectCardioName(name);
        }

        ((CollectionView)sender).SelectedItem = null;
    }
}
