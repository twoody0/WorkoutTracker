using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

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
        var page = Application.Current?.Windows[0].Page;

        if (!int.TryParse(Age?.Trim(), out int age) || !double.TryParse(Weight?.Trim(), out double weight))
        {
            if (page != null)
                await page.DisplayAlert("Invalid Input", "Please enter a valid age and weight", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            if (page != null)
                await page.DisplayAlert("Missing Fields", "Username and password are required", "OK");
            return;
        }

        var user = new User(
            name: Name?.Trim() ?? string.Empty,
            age: age,
            weight: weight,
            username: Username?.Trim() ?? string.Empty,
            password: Password,
            email: Email?.Trim() ?? string.Empty
        );

        bool success = await _authService.SignupAsync(user);
        if (success)
        {
            ((AppShell)Shell.Current).UpdateShellItems();
            await Shell.Current.GoToAsync("///HomePage");
        }
        else
        {
            if (page != null)
                await page.DisplayAlert("Signup Failed", "Username already exists", "OK");
        }
    });

    #endregion
}
