using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using Microsoft.Maui.Dispatching;

namespace WorkoutTracker.ViewModels;

public class AuthViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _services;

    // Backing fields for properties
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _age = string.Empty;
    private string _weight = string.Empty;

    public AuthViewModel(IAuthService authService, IServiceProvider services)
    {
        _authService = authService;
        _services = services;
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
                App.SetRootPage(_services.GetRequiredService<AppShell>());
            });
        }
        else
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Login Failed", "Invalid username or password.", "OK");
        }
    }

    private async Task HandleRegister()
    {
        if (!int.TryParse(Age?.Trim(), out int parsedAge) || !double.TryParse(Weight?.Trim(), out double parsedWeight))
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Validation Error", "Please enter a valid age and weight.", "OK");
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
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Signup Failed", "Username already exists.", "OK");
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
