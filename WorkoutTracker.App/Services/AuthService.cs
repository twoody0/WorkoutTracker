using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class AuthService : IAuthService
{
    // In-memory list of users.
    private readonly List<User> _users = new();

    // Property to hold the current logged-in user.
    public User CurrentUser { get; private set; }

    public async Task<User> LoginAsync(string username, string password)
    {
        await Task.Delay(500);
        var user = _users.FirstOrDefault(u => u.Username == username && u.Password == password);
        if (user != null)
        {
            CurrentUser = user;
        }
        return user;
    }

    public async Task<bool> SignupAsync(User user)
    {
        await Task.Delay(500);
        if (_users.Any(u => u.Username == user.Username))
            return false;

        _users.Add(user);
        // Automatically log the user in.
        CurrentUser = user;
        return true;
    }

    public void SignOut()
    {
        CurrentUser = null!;
    }
}
