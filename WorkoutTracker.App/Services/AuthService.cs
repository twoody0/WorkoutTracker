using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

public class AuthService : IAuthService
{
    // In-memory list of users.
    private readonly List<User> _users = new();

    // Property to hold the current logged-in user (nullable for sign-out support).
    public User? CurrentUser { get; private set; }

    public async Task<User?> LoginAsync(string username, string password)
    {
        // Simulated delay (e.g., mimicking a DB call)
        await Task.Delay(500);

        // Basic validation
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        // Attempt to find matching user
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

        // Basic validation
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            return false;

        // Prevent duplicate usernames
        if (_users.Any(u => u.Username == user.Username))
            return false;

        _users.Add(user);
        CurrentUser = user;
        return true;
    }

    public void SignOut()
    {
        CurrentUser = null;
    }
}
