using Microsoft.Maui.Storage;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker;

public partial class AppShell : Shell
{
    private readonly IAppModeService _appModeService;
    private bool _isResumingActiveCardioSession;
    private const string ActiveCardioSessionPreferenceKey = "cardio_session.is_tracking";

    public AppShell(IAppModeService appModeService)
    {
        InitializeComponent();

        _appModeService = appModeService;

        BuildSignedInTabs();

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
            Route = "add-workout",
            Title = "Add Workout",
            ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<WorkoutPage>()),
            Icon = "addworkout.png"
        });

        tabBar.Items.Add(new ShellContent
        {
            Route = "heat-map",
            Title = "Heat Map",
            ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<HomePage>()),
            Icon = "heat_map.png"
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

        tabBar.Items.Add(new ShellContent
        {
            Route = "dashboard",
            Title = "Profile",
            ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<DashboardPage>()),
            Icon = "profile.png"
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
            "add-workout",
            "heat-map"
        };

        if (_appModeService.HasLeaderboard)
        {
            routes.Add("leaderboard");
        }

        routes.Add("workout-plans");
        routes.Add("dashboard");
        return routes;
    }

    public async Task ResumeActiveCardioSessionIfNeededAsync()
    {
        if (_isResumingActiveCardioSession || !Preferences.Get(ActiveCardioSessionPreferenceKey, false))
        {
            return;
        }

        if (CurrentPage is CardioSessionPage || Navigation.NavigationStack.OfType<CardioSessionPage>().Any())
        {
            return;
        }

        _isResumingActiveCardioSession = true;

        try
        {
            await GoToAsync("//add-workout");
            await Task.Delay(50);

            if (CurrentPage is CardioSessionPage || Navigation.NavigationStack.OfType<CardioSessionPage>().Any())
            {
                return;
            }

            await Navigation.PushAsync(App.Services.GetRequiredService<CardioSessionPage>());
        }
        finally
        {
            _isResumingActiveCardioSession = false;
        }
    }
}
