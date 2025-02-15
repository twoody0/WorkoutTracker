using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Views;
using WorkoutTracker;

#if ANDROID
using WorkoutTracker.Platforms.Android;
#endif

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
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

        // Register ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<WorkoutViewModel>();
        builder.Services.AddTransient<ViewWorkoutViewModel>();
        builder.Services.AddTransient<CardioWorkoutViewModel>();
        builder.Services.AddTransient<WeightliftingLibraryViewModel>();

        // Register Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<WorkoutPage>();
        builder.Services.AddTransient<ViewWorkoutPage>();
        builder.Services.AddTransient<CardioSessionPage>();
        builder.Services.AddTransient<WeightliftingLibraryPage>();

        return builder.Build();
    }
}
