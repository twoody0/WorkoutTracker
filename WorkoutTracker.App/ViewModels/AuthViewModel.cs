using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using Microsoft.Maui.Dispatching;

namespace WorkoutTracker.ViewModels;

public class AuthViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    // Backing fields for properties
    private string _username;
    private string _password;
    private string _name;
    private string _email;
    private string _age;
    private string _weight;

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

    private bool _isFormValid;
    public bool IsFormValid
    {
        get => _isFormValid;
        set => SetProperty(ref _isFormValid, value);
    }

    public bool IsRegisterMode => !IsLoginMode;

    public string SubmitButtonText => IsLoginMode ? "Login" : "Register";

    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value);
            ValidateForm();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            ValidateForm();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            SetProperty(ref _name, value);
            ValidateForm();
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            SetProperty(ref _email, value);
            ValidateForm();
        }
    }

    public string Age
    {
        get => _age;
        set
        {
            SetProperty(ref _age, value);
            ValidateForm();
        }
    }

    public string Weight
    {
        get => _weight;
        set
        {
            SetProperty(ref _weight, value);
            ValidateForm();
        }
    }

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

    private void ValidateForm()
    {
        if (IsLoginMode)
        {
            IsFormValid = !string.IsNullOrWhiteSpace(Username) &&
                          !string.IsNullOrWhiteSpace(Password);
        }
        else // Register mode
        {
            IsFormValid = !string.IsNullOrWhiteSpace(Username) &&
                          !string.IsNullOrWhiteSpace(Password) &&
                          !string.IsNullOrWhiteSpace(Name) &&
                          !string.IsNullOrWhiteSpace(Email) &&
                          !string.IsNullOrWhiteSpace(Age) &&
                          !string.IsNullOrWhiteSpace(Weight);
        }
    }

    #endregion
}
