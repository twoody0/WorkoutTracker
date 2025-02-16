using System.Linq;
using WorkoutTracker.Models;
using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views
{
    public partial class WorkoutPage : ContentPage
    {
        public WorkoutPage(WorkoutViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private void ExerciseEntry_Focused(object sender, FocusEventArgs e)
        {
            if (BindingContext is WorkoutViewModel vm)
            {
                vm.IsNameFieldFocused = true;
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
    }
}
