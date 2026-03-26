using WorkoutTracker.Services;

namespace WorkoutTracker;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        Services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(CreateRootPage());
    }

    public static void SetRootPage(Page page)
    {
        var window = Current?.Windows.FirstOrDefault();
        if (window != null)
        {
            window.Page = page;
        }
    }

    private static Page CreateRootPage()
    {
        var auth = Services.GetRequiredService<IAuthService>();

        return auth.CurrentUser != null
            ? Services.GetRequiredService<AppShell>()
            : Services.GetRequiredService<SignedOutShell>();
    }
}
