using System.Diagnostics;
using WorkoutTracker.Services;

namespace WorkoutTracker;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        Services = services;
        services.GetRequiredService<IThemeService>().ApplySavedTheme();
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            var rootPage = CreateRootPage();
            var window = new Window(rootPage);

            if (rootPage is AppShell shell)
            {
                window.Created += (_, _) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () => await shell.ResumeActiveCardioSessionIfNeededAsync());
                };

                window.Resumed += (_, _) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () => await shell.ResumeActiveCardioSessionIfNeededAsync());
                };
            }

            return window;
        }
        catch (Exception ex)
        {
            return new Window(CreateErrorPage(ex));
        }
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
        var appMode = Services.GetRequiredService<IAppModeService>();
        var auth = Services.GetRequiredService<IAuthService>();

        return !appMode.RequiresLoginOnLaunch || auth.CurrentUser != null
            ? Services.GetRequiredService<AppShell>()
            : Services.GetRequiredService<SignedOutShell>();
    }

    private static Page CreateErrorPage(Exception ex)
    {
        Debug.WriteLine(ex);

        return new ContentPage
        {
            Title = "Startup Error",
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 24,
                    Spacing = 12,
                    Children =
                    {
                        new Label
                        {
                            Text = "The app hit an error while starting.",
                            FontSize = 22,
                            FontAttributes = FontAttributes.Bold
                        },
                        new Label
                        {
                            Text = ex.Message
                        }
                    }
                }
            }
        };
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine(e.Exception);
        e.SetObserved();
    }
}
