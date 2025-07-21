using System.ComponentModel;
using WorkoutTracker.Helpers;
using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views;

public partial class AuthPage : ContentPage
{
    public AuthPage(AuthViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Listen for IsFormValid changes
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AuthViewModel.IsFormValid))
        {
            var viewModel = (AuthViewModel)BindingContext;

            // Get target colors from VisualStateManager
            Color targetBackground = viewModel.IsFormValid ? Colors.LightBlue : Colors.LightGray;
            Color targetText = viewModel.IsFormValid ? Colors.White : Colors.DarkGray;

            // Animate background color
            await SubmitButton.ColorTo(
                SubmitButton.BackgroundColor,
                targetBackground,
                color => SubmitButton.BackgroundColor = color,
                250);

            // Animate text color
            await SubmitButton.ColorTo(
                SubmitButton.TextColor,
                targetText,
                color => SubmitButton.TextColor = color,
                250);
        }
    }
}
