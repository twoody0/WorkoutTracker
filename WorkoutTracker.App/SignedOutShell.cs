using WorkoutTracker.Views;

namespace WorkoutTracker;

public class SignedOutShell : Shell
{
    public SignedOutShell()
    {
        Items.Add(new ShellContent
        {
            Title = "Welcome",
            ContentTemplate = new DataTemplate(() =>
                App.Services.GetRequiredService<AuthPage>())
        });
    }
}
