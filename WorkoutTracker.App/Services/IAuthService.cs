using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task<bool> SignupAsync(User user);

    User? CurrentUser { get; }
    void SignOut();
}
