using System.Diagnostics.CodeAnalysis;

namespace WorkoutTracker.Models;

public class User
{
    public required string Name { get; set; } = default!;
    public int Age { get; set; }
    public double Weight { get; set; }
    public required string Username { get; set; } = default!;
    public required string Password { get; set; } = default!;
    public required string Email { get; set; } = default!;

    // Parameterless constructor for data binding / deserialization
    public User() { }

    [SetsRequiredMembers]
    public User(string name, int age, double weight, string username, string password, string email)
    {
        Name = name;
        Age = age;
        Weight = weight;
        Username = username;
        Password = password;
        Email = email;
    }
}
