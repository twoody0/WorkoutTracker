namespace WorkoutTracker.Views;

public partial class BodyWeightPromptPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _resultSource = new();

    public BodyWeightPromptPage(string title, string message, string initialValue)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        WeightEntry.Text = initialValue;
    }

    public static async Task<string?> ShowAsync(Page parent, string title, string message, string initialValue)
    {
        var promptPage = new BodyWeightPromptPage(title, message, initialValue);
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

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _resultSource.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var value = WeightEntry.Text?.Trim();
        if (!double.TryParse(value, out var weight) || weight <= 0)
        {
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;
        _resultSource.TrySetResult(value);
        await Navigation.PopModalAsync();
    }
}
