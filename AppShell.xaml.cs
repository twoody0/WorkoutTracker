using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell()
    {
        InitializeComponent();

        // Resolve IAuthService from DI.
        _authService = App.Services.GetRequiredService<IAuthService>();
        UpdateShellItems();

        // Register routes explicitly.
        Routing.RegisterRoute("SignupPage", typeof(SignupPage));
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
        Routing.RegisterRoute("LeaderboardPage", typeof(LeaderboardPage));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateShellItems();
    }

    public void UpdateShellItems()
    {
        // Clear existing items.
        this.Items.Clear();

        // Always add the Home page.
        FlyoutItem homeFlyout = new FlyoutItem
        {
            Title = "Home",
            Route = "HomePage",
            Icon = "home.png" // (optional)
        };
        homeFlyout.Items.Add(new ShellContent
        {
            ContentTemplate = new DataTemplate(typeof(HomePage))
        });
        this.Items.Add(homeFlyout);

        // If the user is signed in, add additional items.
        if (_authService.CurrentUser != null)
        {
            FlyoutItem dashboardFlyout = new FlyoutItem
            {
                Title = "Dashboard",
                Route = "DashboardPage",
                Icon = "dashboard.png" // (optional)
            };
            dashboardFlyout.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(DashboardPage))
            });
            this.Items.Add(dashboardFlyout);

            FlyoutItem leaderboardFlyout = new FlyoutItem
            {
                Title = "Leaderboard",
                Route = "LeaderboardPage",
                Icon = "leaderboard.png"
            };
            leaderboardFlyout.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(LeaderboardPage))
            });
            this.Items.Add(leaderboardFlyout);

            FlyoutItem addWorkoutFlyout = new FlyoutItem
            {
                Title = "Add Workout",
                Route = "WorkoutPage",
                Icon = "addworkout.png"
            };
            addWorkoutFlyout.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(WorkoutPage))
            });
            this.Items.Add(addWorkoutFlyout);

            FlyoutItem viewWorkoutsFlyout = new FlyoutItem
            {
                Title = "View Workouts",
                Route = "ViewWorkoutPage",
                Icon = "viewworkouts.png"
            };
            viewWorkoutsFlyout.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(ViewWorkoutPage))
            });
            this.Items.Add(viewWorkoutsFlyout);

            FlyoutItem cardioSessionFlyout = new FlyoutItem
            {
                Title = "Cardio Session",
                Route = "CardioSessionPage",
                Icon = "cardio.png"
            };
            cardioSessionFlyout.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(CardioSessionPage))
            });
            this.Items.Add(cardioSessionFlyout);

            FlyoutItem libraryFlyout = new FlyoutItem
            {
                Title = "Workout Library",
                Route = "WeightliftingLibraryPage",
                Icon = "library.png"
            };
            libraryFlyout.Items.Add(new ShellContent
            {
                ContentTemplate = new DataTemplate(typeof(WeightliftingLibraryPage))
            });
            this.Items.Add(libraryFlyout);
        }
    }
}
