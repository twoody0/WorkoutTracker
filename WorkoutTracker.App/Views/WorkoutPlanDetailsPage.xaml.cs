using WorkoutTracker.ViewModels;
using WorkoutTracker.Models;

namespace WorkoutTracker.Views;

public partial class WorkoutPlanDetailsPage : ContentPage
{
    public WorkoutPlanDetailsViewModel ViewModel { get; }

    public WorkoutPlanDetailsPage(WorkoutPlanDetailsViewModel viewModel, WorkoutPlan plan)
    {
        InitializeComponent();

        // Load plan before BindingContext is set
        viewModel.LoadPlan(plan);

        ViewModel = viewModel;
        BindingContext = ViewModel;
    }
}
