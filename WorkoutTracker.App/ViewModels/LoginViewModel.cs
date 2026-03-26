using System.Windows.Input;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// ViewModel for handling user login.
/// </summary>
public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _services;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isBusy;

    public LoginViewModel(IAuthService authService, IServiceProvider services)
    {
        _authService = authService;
        _services = services;

        LoginCommand = new Command(async () => await ExecuteLoginAsync(), () => !IsBusy);
        NavigateToRegisterCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(SignupPage)));
    }

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

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                (LoginCommand as Command)?.ChangeCanExecute();
        }
    }

    public ICommand LoginCommand { get; }
    public ICommand NavigateToRegisterCommand { get; }

    private async Task ExecuteLoginAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var user = await _authService.LoginAsync(Username, Password);
            if (user != null)
            {
                // Replace MainPage entirely with the signed-in shell
                App.SetRootPage(_services.GetRequiredService<AppShell>());
                return;
            }

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Login Failed", "Invalid username or password", "OK");
        }
        catch (Exception ex)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
                await page.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
