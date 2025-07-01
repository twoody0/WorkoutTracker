using CommunityToolkit.Maui.Core.Platform;
using System.Linq;
using WorkoutTracker.Models;
using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class WorkoutPage : ContentPage
{
    public WorkoutPage(WorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void ExerciseEntry_Focused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.IsNameFieldFocused = true;
            await vm.UpdateExerciseSuggestionsAsync();
        }
    }

    private void ExerciseEntry_Unfocused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.IsNameFieldFocused = false;
        }
    }

    private void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is WeightliftingExercise exercise)
        {
            if (BindingContext is WorkoutViewModel vm)
            {
                vm.SelectExerciseCommand.Execute(exercise);
            }
        }
        // Clear the selection so that the same item can be selected again if needed.
        ((CollectionView)sender).SelectedItem = null;
    }
    private async void OnAddWorkoutClicked(object sender, EventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.Name = vm.ExerciseSearchQuery;

            if (vm.AddWorkoutCommand.CanExecute(null))
                vm.AddWorkoutCommand.Execute(null);
        }

        // Hide keyboard
        await ExerciseEntry.HideKeyboardAsync();
        await WeightEntry.HideKeyboardAsync();
        await RepsEntry.HideKeyboardAsync();
        await SetsEntry.HideKeyboardAsync();
    }
}
