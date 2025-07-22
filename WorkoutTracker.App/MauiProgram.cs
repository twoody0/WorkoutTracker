using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Views;
using WorkoutTracker;
using CommunityToolkit.Maui;


#if ANDROID
using WorkoutTracker.Platforms.Android;
#endif

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Existing services
        builder.Services.AddSingleton<IWorkoutService, WorkoutService>();

        // Register platform-specific service for step counting on Android:
        #if ANDROID
        builder.Services.AddSingleton<IStepCounterService, StepCounterServiceAndroid>();
        #endif

        // Register the Workout Library service
        builder.Services.AddSingleton<IWorkoutLibraryService, WorkoutLibraryService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IWorkoutPlanService, WorkoutPlanService>();
        builder.Services.AddSingleton<IWorkoutScheduleService, WorkoutScheduleService>();

        // Register ViewModels
        builder.Services.AddTransient<AuthViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<SignupViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<LeaderboardViewModel>();
        builder.Services.AddTransient<WorkoutViewModel>();
        builder.Services.AddTransient<ViewWorkoutViewModel>();
        builder.Services.AddTransient<CardioWorkoutViewModel>();
        builder.Services.AddTransient<WeightliftingLibraryViewModel>();
        builder.Services.AddSingleton<WeeklyScheduleViewModel>();
        builder.Services.AddTransient<WorkoutPlanDetailsViewModel>();
        builder.Services.AddSingleton<WorkoutPlanViewModel>();
        builder.Services.AddTransient<EditDayViewModel>();

        // Register Pages
        builder.Services.AddTransient<AuthPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<SignupPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<LeaderboardPage>();
        builder.Services.AddTransient<WorkoutPage>();
        builder.Services.AddTransient<ViewWorkoutPage>();
        builder.Services.AddTransient<CardioSessionPage>();
        builder.Services.AddTransient<WeightliftingLibraryPage>();
        builder.Services.AddTransient<WorkoutPlanDetailsPage>();
        builder.Services.AddSingleton<WorkoutPlanPage>();
        builder.Services.AddSingleton<WeeklySchedulePage>();
        builder.Services.AddTransient<EditDayPage>();

        // Register shell
        builder.Services.AddSingleton<SignedOutShell>();

        return builder.Build();
    }
}
