using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views
{
    public partial class WorkoutPlanPage : ContentPage
    {
        public WorkoutPlanPage(WorkoutPlanViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
