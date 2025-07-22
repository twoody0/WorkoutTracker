using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell()
    {
        InitializeComponent();

        _authService = App.Services.GetRequiredService<IAuthService>();

        // Initial navigation setup
        UpdateShellItems();

        // Register routes (optional since we’re using DI below)
        Routing.RegisterRoute(nameof(SignupPage), typeof(SignupPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
        Routing.RegisterRoute(nameof(LeaderboardPage), typeof(LeaderboardPage));
        Routing.RegisterRoute(nameof(ViewWorkoutPage), typeof(ViewWorkoutPage));
        Routing.RegisterRoute(nameof(WorkoutPage), typeof(WorkoutPage));
        Routing.RegisterRoute(nameof(CardioSessionPage), typeof(CardioSessionPage));
        Routing.RegisterRoute(nameof(WeightliftingLibraryPage), typeof(WeightliftingLibraryPage));
        Routing.RegisterRoute(nameof(WorkoutPlanDetailsPage), typeof(WorkoutPlanDetailsPage));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateShellItems();
    }

    public void UpdateShellItems()
    {
        this.Items.Clear();

        if (_authService.CurrentUser != null)
        {
            var tabBar = new TabBar();

            tabBar.Items.Add(new ShellContent
            {
                Title = "Home",
                ContentTemplate = new DataTemplate(() =>
                    App.Services.GetRequiredService<HomePage>()),
                Icon = "home.png"
            });

            tabBar.Items.Add(new ShellContent
            {
                Title = "Dashboard",
                ContentTemplate = new DataTemplate(() =>
                    App.Services.GetRequiredService<DashboardPage>()),
                Icon = "dashboard.png"
            });

            tabBar.Items.Add(new ShellContent
            {
                Title = "Add Workout",
                ContentTemplate = new DataTemplate(() =>
                    App.Services.GetRequiredService<WorkoutPage>()),
                Icon = "addworkout.png"
            });

            tabBar.Items.Add(new ShellContent
            {
                Title = "Leaderboard",
                ContentTemplate = new DataTemplate(() =>
                    App.Services.GetRequiredService<LeaderboardPage>()),
                Icon = "leaderboard.png"
            });

            tabBar.Items.Add(new ShellContent
            {
                Title = "Workout Plans",
                ContentTemplate = new DataTemplate(() =>
                    App.Services.GetRequiredService<WorkoutPlanPage>()),
                Icon = "workoutplans.png"
            });

            this.Items.Add(tabBar);
        }
        else
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null)
            {
                window.Page = App.Services.GetRequiredService<SignedOutShell>();
            }
        }
    }
}
