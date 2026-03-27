using System.ComponentModel;
using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views
{
    public partial class WorkoutPlanPage : ContentPage
    {
        private readonly WorkoutPlanViewModel _viewModel;

        public WorkoutPlanPage(WorkoutPlanViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(WorkoutPlanViewModel.IsCreatePlanVisible) || !_viewModel.IsCreatePlanVisible)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(120);
                await PlanScrollView.ScrollToAsync(CreatePlanSection, ScrollToPosition.Start, true);
                PlanNameEntry.Focus();
            });
        }
    }
}
