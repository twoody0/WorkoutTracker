using WorkoutTracker.Models;
using WorkoutTracker.Services;

namespace WorkoutTracker.Tests;

[TestClass]
public class AuthServiceTests
{
    [TestMethod]
    public async Task SignupAsync_WithValidUser_SetsCurrentUserAndReturnsTrue()
    {
        var service = new AuthService();
        var user = new User("Tyler", 28, 185, "tyler", "secret", "tyler@example.com");

        var success = await service.SignupAsync(user);

        Assert.IsTrue(success);
        Assert.AreSame(user, service.CurrentUser);
    }

    [TestMethod]
    public async Task SignupAsync_WithDuplicateUsername_ReturnsFalse()
    {
        var service = new AuthService();
        await service.SignupAsync(new User("Tyler", 28, 185, "tyler", "secret", "tyler@example.com"));

        var success = await service.SignupAsync(new User("Other", 30, 190, "tyler", "other", "other@example.com"));

        Assert.IsFalse(success);
        Assert.AreEqual("tyler", service.CurrentUser?.Username);
    }

    [TestMethod]
    public async Task LoginAsync_WithMatchingCredentials_ReturnsUserAndSetsCurrentUser()
    {
        var service = new AuthService();
        var user = new User("Tyler", 28, 185, "tyler", "secret", "tyler@example.com");
        await service.SignupAsync(user);
        service.SignOut();

        var loggedInUser = await service.LoginAsync("tyler", "secret");

        Assert.IsNotNull(loggedInUser);
        Assert.AreEqual("tyler", loggedInUser.Username);
        Assert.AreSame(loggedInUser, service.CurrentUser);
    }

    [TestMethod]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsNull()
    {
        var service = new AuthService();
        await service.SignupAsync(new User("Tyler", 28, 185, "tyler", "secret", "tyler@example.com"));
        service.SignOut();

        var loggedInUser = await service.LoginAsync("tyler", "wrong-password");

        Assert.IsNull(loggedInUser);
        Assert.IsNull(service.CurrentUser);
    }

    [TestMethod]
    public async Task SignOut_ClearsCurrentUser()
    {
        var service = new AuthService();
        await service.SignupAsync(new User("Tyler", 28, 185, "tyler", "secret", "tyler@example.com"));

        service.SignOut();

        Assert.IsNull(service.CurrentUser);
    }
}
