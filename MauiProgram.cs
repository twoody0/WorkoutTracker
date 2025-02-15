namespace WorkoutTracker;

using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Views;

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

        // Register your services
        builder.Services.AddSingleton<IWorkoutService, WorkoutService>();
        // Register your ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        // Register your Views
        builder.Services.AddTransient<HomePage>();

        return builder.Build();
    }
}

