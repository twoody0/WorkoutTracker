using System.Windows.Input;
using Microsoft.Maui.Controls;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        public HomeViewModel(IAuthService authService)
        {
            _authService = authService;
            // If a user is logged in, set the welcome message.
            if (_authService.CurrentUser != null)
                WelcomeMessage = $"Welcome, {_authService.CurrentUser.Username}";
            else
                WelcomeMessage = string.Empty;
        }

        public string WelcomeMessage { get; set; }

        public ICommand NavigateToLoginCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("//LoginPage");
        });

        public ICommand NavigateToSignupCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("//SignupPage");
        });

        public ICommand NavigateToDashboardCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("//DashboardPage");
        });

        public ICommand NavigateToLeaderboardCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("//LeaderboardPage");
        });
    }
}
