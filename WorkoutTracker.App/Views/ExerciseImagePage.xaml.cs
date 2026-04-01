namespace WorkoutTracker.Views;

public partial class ExerciseImagePage : ContentPage
{
    public string ExerciseName { get; }
    public string Summary { get; }
    public IReadOnlyList<string> Cues { get; }
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageSource);
    public string ImageSource { get; }
    public string CardLabel => HasImage ? "Exercise Coaching Card" : "Exercise Coaching Notes";
    public string Subheading => HasImage
        ? "Use the demo and cues together so the movement feels easier to understand."
        : "Use these notes as a quick coaching guide while you move.";
    public string CoachingIntro => "Keep these cues simple. Pick one or two to focus on for your next set.";

    public ExerciseImagePage(string exerciseName)
    {
        InitializeComponent();
        ExerciseName = exerciseName.Trim();
        var info = Helpers.ExerciseInfoCatalog.GetInfo(ExerciseName);
        Summary = info.Summary;
        Cues = info.Cues;
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
