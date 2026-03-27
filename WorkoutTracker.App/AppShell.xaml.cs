using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker;

public partial class AppShell : Shell
{
    private readonly IAppModeService _appModeService;

    public AppShell(IAppModeService appModeService)
    {
        InitializeComponent();

        _appModeService = appModeService;

        // Initial navigation setup
        BuildSignedInTabs();

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

    private void BuildSignedInTabs()
    {
        Items.Clear();

        var tabBar = new TabBar();

        tabBar.Items.Add(new ShellContent
        {
            Route = "home",
            Title = "Home",
            ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<HomePage>()),
            Icon = "home.png"
        });

        tabBar.Items.Add(new ShellContent
        {
            Route = "dashboard",
            Title = "Dashboard",
            ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<DashboardPage>()),
            Icon = "dashboard.png"
        });

        tabBar.Items.Add(new ShellContent
        {
            Route = "add-workout",
            Title = "Add Workout",
            ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<WorkoutPage>()),
            Icon = "addworkout.png"
        });

        if (_appModeService.HasLeaderboard)
        {
            tabBar.Items.Add(new ShellContent
            {
                Route = "leaderboard",
                Title = "Leaderboard",
                ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<LeaderboardPage>()),
                Icon = "leaderboard.png"
            });
        }

        tabBar.Items.Add(new ShellContent
        {
            Route = "workout-plans",
            Title = "Workout Plans",
            ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<WorkoutPlanPage>()),
            Icon = "workoutplans.png"
        });

        Items.Add(tabBar);
    }

    public async Task NavigateToAdjacentPrimaryTabAsync(string currentRoute, int step)
    {
        var routes = GetPrimaryTabRoutes();
        var currentIndex = routes.FindIndex(route => string.Equals(route, currentRoute, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = currentIndex + step;
        if (targetIndex < 0 || targetIndex >= routes.Count)
        {
            return;
        }

        await GoToAsync($"//{routes[targetIndex]}");
    }

    private List<string> GetPrimaryTabRoutes()
    {
        var routes = new List<string>
        {
            "home",
            "dashboard",
            "add-workout"
        };

        if (_appModeService.HasLeaderboard)
        {
            routes.Add("leaderboard");
        }

        routes.Add("workout-plans");
        return routes;
    }
}
