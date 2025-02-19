using System.Windows.Input;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    private string _username;
    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    private string _password;
    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public ICommand LoginCommand => new Command(async () =>
    {
        var user = await _authService.LoginAsync(Username, Password);
        if (user != null)
        {
            // Navigate to Dashboard (the calendar view)
            ((AppShell)Shell.Current).UpdateShellItems();
            await Shell.Current.GoToAsync("///HomePage");
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Login Failed", "Invalid username or password", "OK");
        }
    });
}
