namespace WorkoutTracker.Views;

public partial class ExerciseImagePage : ContentPage
{
    public string ExerciseName { get; }
    public string Summary { get; }
    public IReadOnlyList<string> Instructions { get; }
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageSource);
    public string ImageSource { get; }
    public string CardLabel => HasImage ? "Exercise How-To Card" : "Exercise How-To Notes";
    public string Subheading => HasImage
        ? "Use the demo and the steps together so the movement is easier to follow."
        : "Use these notes as a quick step-by-step guide while you move.";
    public string CoachingIntro => "These steps are meant to be followed in order. Slow, controlled reps are usually the right default.";

    public ExerciseImagePage(string exerciseName)
    {
        InitializeComponent();
        ExerciseName = exerciseName.Trim();
        var info = Helpers.ExerciseInfoCatalog.GetInfo(ExerciseName);
        Summary = info.Summary;
        Instructions = info.Steps
            .Select((step, index) => $"{index + 1}. {step}")
            .ToArray();
        ImageSource = Helpers.ExerciseImageCatalog.GetImageSource(ExerciseName);
        BindingContext = this;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async Task CloseAsync()
    {
        if (Navigation.ModalStack.LastOrDefault() == this)
        {
            await Navigation.PopModalAsync();
        }
    }
}
