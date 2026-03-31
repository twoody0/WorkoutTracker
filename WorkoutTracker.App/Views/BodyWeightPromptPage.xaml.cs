using WorkoutTracker.Helpers;

namespace WorkoutTracker.Views;

public sealed class BodyWeightPromptResult
{
    public string? WeightText { get; init; }

    public bool NavigateToWorkoutPlans { get; init; }
}

public partial class BodyWeightPromptPage : ContentPage
{
    private readonly TaskCompletionSource<BodyWeightPromptResult?> _resultSource = new();

    public BodyWeightPromptPage(string title, string message, string initialValue, string? workoutPlansButtonText)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        WeightEntry.Text = initialValue;
        WorkoutPlansButton.IsVisible = !string.IsNullOrWhiteSpace(workoutPlansButtonText);
        WorkoutPlansButton.Text = workoutPlansButtonText ?? WorkoutPlansButton.Text;
    }

    public static async Task<BodyWeightPromptResult?> ShowAsync(
        Page parent,
        string title,
        string message,
        string initialValue,
        string? workoutPlansButtonText = null)
    {
        var promptPage = new BodyWeightPromptPage(title, message, initialValue, workoutPlansButtonText);
        await parent.Navigation.PushModalAsync(promptPage);
        return await promptPage._resultSource.Task;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100);
            WeightEntry.Focus();
        });
    }

    private void WeightEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
        {
            return;
        }

        var sanitized = InputSanitizer.SanitizePositiveDecimalText(e.NewTextValue, InputSanitizer.MaxBodyWeight);
        if (!string.Equals(entry.Text, sanitized, StringComparison.Ordinal))
        {
            entry.Text = sanitized;
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(CreateResult());
    }

    private async void OnWorkoutPlansClicked(object? sender, EventArgs e)
    {
        await CloseAsync(CreateResult(navigateToWorkoutPlans: true));
    }

    private async Task CloseAsync(BodyWeightPromptResult? result)
    {
        if (Navigation.ModalStack.LastOrDefault() == this)
        {
            await Navigation.PopModalAsync();
        }

        _resultSource.TrySetResult(result);
    }

    private BodyWeightPromptResult? CreateResult(bool navigateToWorkoutPlans = false)
    {
        var value = InputSanitizer.SanitizePositiveDecimalText(WeightEntry.Text, InputSanitizer.MaxBodyWeight);
        var hasValidWeight = InputSanitizer.TryParseBodyWeight(value, out _);
        ErrorLabel.IsVisible = false;

        if (!hasValidWeight && !navigateToWorkoutPlans && string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return new BodyWeightPromptResult
        {
            WeightText = hasValidWeight ? value : null,
            NavigateToWorkoutPlans = navigateToWorkoutPlans
        };
    }
}
