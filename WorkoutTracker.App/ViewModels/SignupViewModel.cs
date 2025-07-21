using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using Microsoft.Maui.Dispatching;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for handling user sign-up.
/// </summary>
public class SignupViewModel : BaseViewModel
{
    #region Private Fields

    private readonly IAuthService _authService;
    private string _name = string.Empty;
    private string _age = string.Empty;
    private string _weight = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _email = string.Empty;

    #endregion

    #region Constructor

    public SignupViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    #endregion

    #region Public Properties

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }

    public string Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    #endregion

    #region Commands

    public ICommand SignupCommand => new Command(async () =>
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;

        try
        {
            if (!ValidateInput(out int parsedAge, out double parsedWeight, out string validationMessage))
            {
                if (page != null)
                    await page.DisplayAlert("Validation Error", validationMessage, "OK");
                return;
            }

            var user = new User(
                name: Name?.Trim() ?? string.Empty,
                age: parsedAge,
                weight: parsedWeight,
                username: Username?.Trim() ?? string.Empty,
                password: Password,
                email: Email?.Trim() ?? string.Empty
            );

            bool signupSuccess = await _authService.SignupAsync(user);
            if (!signupSuccess)
            {
                if (page != null)
                    await page.DisplayAlert("Signup Failed", "Username already exists", "OK");
                return;
            }

            // Auto-login after signup
            var loggedInUser = await _authService.LoginAsync(Username, Password);
            if (loggedInUser == null)
            {
                if (page != null)
                    await page.DisplayAlert("Login Failed", "Could not log in after signup. Please try logging in manually.", "OK");
                return;
            }

            // Switch to AppShell on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new AppShell();
            });
        }
        catch (HttpRequestException ex)
        {
            await page.DisplayAlert("Network Error", "Please check your internet connection.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            await page.DisplayAlert("Navigation Error", "Unable to navigate. Please try again.", "OK");
        }
    });

    #endregion

    #region Private Helpers

    private bool ValidateInput(out int parsedAge, out double parsedWeight, out string message)
    {
        parsedAge = 0;
        parsedWeight = 0;
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            message = "Username and password are required.";
            return false;
        }

        if (!int.TryParse(Age?.Trim(), out parsedAge))
        {
            message = "Please enter a valid numeric age.";
            return false;
        }

        if (!double.TryParse(Weight?.Trim(), out parsedWeight))
        {
            message = "Please enter a valid numeric weight.";
            return false;
        }

        return true;
    }

    #endregion
}
