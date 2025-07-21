using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using Microsoft.Maui.Dispatching;

namespace WorkoutTracker.ViewModels;

public class AuthViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    public AuthViewModel()
    {
        _authService = App.Services.GetRequiredService<IAuthService>();
    }

    #region Properties

    private bool _isLoginMode = true;
    public bool IsLoginMode
    {
        get => _isLoginMode;
        set
        {
            SetProperty(ref _isLoginMode, value);
            OnPropertyChanged(nameof(IsRegisterMode));
            OnPropertyChanged(nameof(SubmitButtonText));
        }
    }

    public bool IsRegisterMode => !IsLoginMode;

    public string SubmitButtonText => IsLoginMode ? "Login" : "Register";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;

    #endregion

    #region Commands

    public ICommand SwitchToLoginCommand => new Command(() => IsLoginMode = true);

    public ICommand SwitchToRegisterCommand => new Command(() => IsLoginMode = false);

    public ICommand SubmitCommand => new Command(async () =>
    {
        if (IsLoginMode)
        {
            await HandleLogin();
        }
        else
        {
            await HandleRegister();
        }
    });

    #endregion

    #region Private Methods

    private async Task HandleLogin()
    {
        var user = await _authService.LoginAsync(Username, Password);
        if (user != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new AppShell();
            });
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Login Failed", "Invalid username or password.", "OK");
        }
    }

    private async Task HandleRegister()
    {
        if (!int.TryParse(Age?.Trim(), out int parsedAge) || !double.TryParse(Weight?.Trim(), out double parsedWeight))
        {
            await Application.Current.MainPage.DisplayAlert("Validation Error", "Please enter a valid age and weight.", "OK");
            return;
        }

        var newUser = new User(Name?.Trim() ?? "", parsedAge, parsedWeight,
                               Username?.Trim() ?? "", Password, Email?.Trim() ?? "");

        bool success = await _authService.SignupAsync(newUser);
        if (success)
        {
            await HandleLogin(); // Auto-login after signup
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Signup Failed", "Username already exists.", "OK");
        }
    }

    #endregion
}
