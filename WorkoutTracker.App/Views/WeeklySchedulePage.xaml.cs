using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class WeeklySchedulePage : ContentPage
{
    private readonly WeeklyScheduleViewModel _viewModel;

    public WeeklySchedulePage(WeeklyScheduleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
