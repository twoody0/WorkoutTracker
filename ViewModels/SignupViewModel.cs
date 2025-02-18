using System.Windows.Input;
using Microsoft.Maui.Controls;
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
        if (int.TryParse(Age, out int age) && double.TryParse(Weight, out double weight))
        {
            var user = new User
            {
                Name = Name,
                Age = age,
                Weight = weight,
                Username = Username,
                Password = Password,
                Email = Email
            };
            var success = await _authService.SignupAsync(user);
            if (success)
            {
                // Automatically logged in because AuthService.CurrentUser is set.
                ((AppShell)Shell.Current).UpdateShellItems();
                await Shell.Current.GoToAsync("///HomePage");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Signup Failed", "Username already exists", "OK");
            }
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Invalid Input", "Please enter a valid age and weight", "OK");
        }
    });
}
