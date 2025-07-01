using WorkoutTracker.Models;

namespace WorkoutTracker.Services;

/// <summary>
/// Defines authentication functionality including login, signup, and session management.
/// </summary>
public interface IAuthService
{
    #region Properties

    /// <summary>
    /// Gets the currently authenticated user, or null if not logged in.
    /// </summary>
    User? CurrentUser { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Attempts to log in with the specified username and password.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <param name="password">The password of the user.</param>
    /// <returns>The authenticated user, or null if authentication fails.</returns>
    Task<User?> LoginAsync(string username, string password);

    /// <summary>
    /// Attempts to register a new user.
    /// </summary>
    /// <param name="user">The user to register.</param>
    /// <returns>True if signup is successful, otherwise false.</returns>
    Task<bool> SignupAsync(User user);

    /// <summary>
    /// Logs out the currently authenticated user.
    /// </summary>
    void SignOut();

    #endregion
}
