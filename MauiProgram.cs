using WorkoutTracker.Services;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Views;

namespace WorkoutTracker;
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
        builder.Services.AddTransient<WorkoutViewModel>();
        builder.Services.AddTransient<ViewWorkoutViewModel>();

        // Register your Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<WorkoutPage>();
        builder.Services.AddTransient<ViewWorkoutPage>();

        return builder.Build();
    }
}
