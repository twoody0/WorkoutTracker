using System.Windows.Input;
using Microsoft.Maui.Controls;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    public HomeViewModel(IAuthService authService)
    {
        _authService = authService;
        UpdateWelcomeMessage();
    }

    public void UpdateWelcomeMessage()
    {
        if (_authService.CurrentUser != null)
            WelcomeMessage = $"Welcome, {_authService.CurrentUser.Username}";
        else
            WelcomeMessage = string.Empty;
    }

    private string _welcomeMessage;
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set
        {
            _welcomeMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUserLoggedIn));
        }
    }

    public bool IsUserLoggedIn => !string.IsNullOrWhiteSpace(WelcomeMessage);

    public ICommand NavigateToLoginCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("LoginPage");
    });

    public ICommand NavigateToSignupCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("SignupPage");
    });

    public ICommand NavigateToDashboardCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("DashboardPage");
    });

    public ICommand NavigateToLeaderboardCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("LeaderboardPage");
    });

    public ICommand SignOutCommand => new Command(async () =>
    {
        _authService.SignOut();
        UpdateWelcomeMessage();
        // Optionally, navigate to HomePage.
        await Shell.Current.GoToAsync("///HomePage");
    });
}
