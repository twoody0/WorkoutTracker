using System.ComponentModel;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;

namespace WorkoutTracker.Views
{
    public partial class WorkoutPlanPage : ContentPage
    {
        private readonly WorkoutPlanViewModel _viewModel;
        private bool _isSubscribedToViewModel;

        public WorkoutPlanPage(WorkoutPlanViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
            SubscribeToViewModel();
            TabSwipeNavigationHelper.Attach(this, "workout-plans");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SubscribeToViewModel();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            UnsubscribeFromViewModel();
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

        private void SubscribeToViewModel()
        {
            if (_isSubscribedToViewModel)
            {
                return;
            }

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _isSubscribedToViewModel = true;
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribedToViewModel)
            {
                return;
            }

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _isSubscribedToViewModel = false;
        }
    }
}
