using System.Windows.Input;
using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.ViewModels;

public class SignupViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    public SignupViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    // Bound properties for the sign-up form.
    public string Name { get; set; }
    public string Age { get; set; }
    public string Weight { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }

    public ICommand SignupCommand => new Command(async () =>
    {
        if (!int.TryParse(Age?.Trim(), out int age) || !double.TryParse(Weight?.Trim(), out double weight))
        {
            await Application.Current.MainPage.DisplayAlert("Invalid Input", "Please enter a valid age and weight", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await Application.Current.MainPage.DisplayAlert("Missing Fields", "Username and password are required", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Signup Failed", "Username already exists", "OK");
        }
    });
}
