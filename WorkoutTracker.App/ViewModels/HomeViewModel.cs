using System.Windows.Input;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for the Home page that handles welcome message, navigation, and session management.
/// </summary>
public class HomeViewModel : BaseViewModel
{
    #region Private Fields

    private readonly IAuthService _authService;
    private string _welcomeMessage = string.Empty;

    #endregion

    #region Constructor

    public HomeViewModel(IAuthService authService)
    {
        _authService = authService;
        UpdateWelcomeMessage();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Welcome message displayed to the user.
    /// </summary>
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set
        {
            if (SetProperty(ref _welcomeMessage, value))
            {
                OnPropertyChanged(nameof(IsUserLoggedIn));
            }
        }
    }

    /// <summary>
    /// Indicates whether a user is currently logged in.
    /// </summary>
    public bool IsUserLoggedIn => !string.IsNullOrWhiteSpace(WelcomeMessage);

    #endregion

    #region Commands

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
        await Shell.Current.GoToAsync("///HomePage");
    });

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the welcome message based on the currently logged-in user.
    /// </summary>
    public void UpdateWelcomeMessage()
    {
        var user = _authService.CurrentUser;
        WelcomeMessage = user != null ? $"Welcome, {user.Username}" : string.Empty;
    }

    #endregion
}
