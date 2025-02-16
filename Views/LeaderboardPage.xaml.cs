using WorkoutTracker.ViewModels;

namespace WorkoutTracker.Views
{
    public partial class LeaderboardPage : ContentPage
    {
        public LeaderboardPage(LeaderboardViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
