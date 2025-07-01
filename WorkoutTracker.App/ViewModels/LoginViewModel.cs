using System.Windows.Input;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for handling user login.
/// </summary>
public class LoginViewModel : BaseViewModel
{
    #region Private Fields

    private readonly IAuthService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;

    #endregion

    #region Constructor

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Username entered by the user.
    /// </summary>
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    /// <summary>
    /// Password entered by the user.
    /// </summary>
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to handle login logic.
    /// </summary>
    public ICommand LoginCommand => new Command(async () =>
    {
        var user = await _authService.LoginAsync(Username, Password);
        if (user != null)
        {
            ((AppShell)Shell.Current).UpdateShellItems(); // Refresh the shell based on login state
            await Shell.Current.GoToAsync("///HomePage"); // Navigate to root home page
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Login Failed", "Invalid username or password", "OK");
        }
    });

    #endregion
}
