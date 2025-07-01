namespace WorkoutTracker.Models;

public class User
{
    public required string Name { get; set; }
    public int Age { get; set; }
    public double Weight { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }

    // Required for data binding / deserialization
    public User() { }

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
