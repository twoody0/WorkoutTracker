using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public sealed class GuestAuthService : IAuthService
{
    private static readonly User GuestUser = new()
    {
        Name = "Local User",
        Age = 0,
        Weight = 0,
        Username = "guest",
        Password = string.Empty,
        Email = string.Empty
    };

    public User? CurrentUser => GuestUser;

    public Task<User?> LoginAsync(string username, string password) =>
        Task.FromResult<User?>(GuestUser);

    public Task<bool> SignupAsync(User user) =>
        Task.FromResult(false);

    public void SignOut()
    {
        // Free mode stays in local guest mode, so there is nothing to sign out.
    }
}
