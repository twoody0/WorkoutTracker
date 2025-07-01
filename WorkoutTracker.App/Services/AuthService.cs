using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Simple in-memory authentication service for handling login, signup, and sign-out operations.
/// </summary>
public class AuthService : IAuthService
{
    #region Fields

    // In-memory user store (replace with DB in production)
    private readonly List<User> _users = new();

    #endregion

    #region Properties

    /// <summary>
    /// Gets the currently logged-in user, or null if not logged in.
    /// </summary>
    public User? CurrentUser { get; private set; }

    #endregion

    #region Methods

    /// <summary>
    /// Attempts to log in a user with the provided credentials.
    /// </summary>
    /// <param name="username">The username to match.</param>
    /// <param name="password">The password to match.</param>
    /// <returns>The logged-in user or null if login failed.</returns>
    public async Task<User?> LoginAsync(string username, string password)
    {
        await Task.Delay(500); // Simulated delay

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = _users.FirstOrDefault(u => u.Username == username && u.Password == password);
        if (user != null)
        {
            CurrentUser = user;
        }

        return user;
    }

    /// <summary>
    /// Registers a new user if the username is not already taken.
    /// </summary>
    /// <param name="user">The user to register.</param>
    /// <returns>True if registration succeeded, otherwise false.</returns>
    public async Task<bool> SignupAsync(User user)
    {
        await Task.Delay(500); // Simulated delay

        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            return false;

        if (_users.Any(u => u.Username == user.Username))
            return false;

        _users.Add(user);
        CurrentUser = user;
        return true;
    }

    /// <summary>
    /// Logs out the currently logged-in user.
    /// </summary>
    public void SignOut()
    {
        CurrentUser = null;
    }

    #endregion
}
