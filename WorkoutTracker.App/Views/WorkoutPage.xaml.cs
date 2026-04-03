using CommunityToolkit.Maui.Core.Platform;
using System.Linq;
using System.ComponentModel;
using WorkoutTracker.Models;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;
using WorkoutTracker.Services;
#if ANDROID
using Android.Views;
using PlatformView = Android.Views.View;
#endif

namespace WorkoutTracker.Views;

public partial class WorkoutPage : ContentPage
{
    private CancellationTokenSource? _resistanceAdjustCancellationTokenSource;
    private bool _hasRepeatedResistanceAdjustment;
    private CancellationTokenSource? _bodyweightWeightAdjustCancellationTokenSource;
    private bool _hasRepeatedBodyweightWeightAdjustment;
    private CancellationTokenSource? _standardWeightAdjustCancellationTokenSource;
    private bool _hasRepeatedStandardWeightAdjustment;
    private readonly IBodyWeightService _bodyWeightService;
    private bool _hasCheckedForInitialBodyWeight;

    public WorkoutPage(WorkoutViewModel vm, IBodyWeightService bodyWeightService)
    {
        InitializeComponent();
        BindingContext = vm;
        _bodyWeightService = bodyWeightService;
        vm.PropertyChanged += OnViewModelPropertyChanged;
        TabSwipeNavigationHelper.Attach(this, "add-workout");
#if ANDROID
        RecommendationsList.HandlerChanged += OnRecommendationsListHandlerChanged;
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is WorkoutViewModel vm)
        {
            if (!_hasCheckedForInitialBodyWeight && !_bodyWeightService.HasBodyWeight())
            {
                _hasCheckedForInitialBodyWeight = true;
                var navigatedToWorkoutPlans = await PromptForBodyWeightAsync(
                    vm,
                    "Welcome to Megnor. This is where you can log workouts and start following your plan. If you enter your body weight here, it will save even if you go straight to Workout Plans.",
                    useCurrentWeightAsInitialValue: false);

                if (navigatedToWorkoutPlans)
                {
                    return;
                }
            }

            await vm.EnsureWorkoutHistoryFreshAsync();
            vm.EnsurePlanRecommendationsFresh();
            if (vm.SelectedRecommendationItem == null && !vm.IsManualWorkoutEntryActive)
            {
                EnsureDefaultRecommendationSelected();
            }
        }

        AttachEntryCompletedHandlers(this);
        AttachNumericEntryFocusHandlers(this);
        UpdateRecommendationsHeight();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateRecommendationsHeight();
    }

    private async void ExerciseEntry_Focused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.IsNameFieldFocused = true;
            var clearedExistingSelection = vm.BeginExerciseNameEdit();
            await vm.UpdateExerciseSuggestionsAsync(showAllForCurrentGroup: true);
            await ScrollExerciseSuggestionsIntoViewAsync();

            if (clearedExistingSelection &&
                sender is Entry entry &&
                !OperatingSystem.IsMacCatalyst())
            {
                await Task.Delay(50);

                if (entry.IsFocused)
                {
                    await entry.HideKeyboardAsync();
                }
            }
        }
    }

    private void ExerciseEntry_Unfocused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.IsNameFieldFocused = false;
            vm.CommitExerciseSelection();
        }
    }

    private void WeightEntry_Unfocused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.CommitStandardWeightInput();
            vm.CommitBodyweightWeightInput();
        }
    }

    private void WeightEntry_Focused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.BeginStandardWeightEdit();
        }
    }

    private void NumericEntry_Focused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            SelectEntryText(entry);
        }
    }

    private void OnAnyEntryCompleted(object? sender, EventArgs e)
    {
        if (sender is Entry entry)
        {
            entry.Unfocus();
        }
    }

    private void Entry_Completed(object sender, EventArgs e)
    {
        OnAnyEntryCompleted(sender, e);
    }

    private void WeightEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry || BindingContext is not WorkoutViewModel vm)
        {
            return;
        }

        var maxWeight = vm.ShowResistanceAdjustment
            ? InputSanitizer.MaxBodyWeight
            : InputSanitizer.MaxWorkoutWeight;

        var sanitized = InputSanitizer.SanitizePositiveDecimalText(e.NewTextValue, maxWeight);
        if (!string.Equals(entry.Text, sanitized, StringComparison.Ordinal))
        {
            entry.Text = sanitized;
        }
    }

    private void RepsEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        var maxValue = BindingContext is WorkoutViewModel vm && vm.UsesTimedStrengthTarget
            ? InputSanitizer.MaxTimedStrengthSeconds
            : InputSanitizer.MaxReps;
        ClampEntryText(sender, e.NewTextValue, maxValue, isDecimal: false);
    }

    private void SetsEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        ClampEntryText(sender, e.NewTextValue, InputSanitizer.MaxSets, isDecimal: false);
    }

    private async void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is WeightliftingExercise exercise &&
            BindingContext is WorkoutViewModel vm)
        {
            vm.SelectExerciseCommand.Execute(exercise);
            ExerciseEntry?.Unfocus();

            if (!OperatingSystem.IsMacCatalyst() &&
                ExerciseEntry != null &&
                ExerciseEntry.IsVisible)
            {
                await ExerciseEntry.HideKeyboardAsync();
            }
        }

        ((CollectionView)sender).SelectedItem = null;
    }
    private async void OnAddWorkoutClicked(object sender, EventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.CommitBodyweightWeightInput();

            if (vm.AddWorkoutCommand.CanExecute(null))
                vm.AddWorkoutCommand.Execute(null);
        }

        // Hide keyboard
        if (!OperatingSystem.IsMacCatalyst())
        {
            if (ExerciseEntry.IsVisible)
            {
                await ExerciseEntry.HideKeyboardAsync();
            }
            if (WeightEntry.IsVisible)
            {
                await WeightEntry.HideKeyboardAsync();
            }
            if (RepsEntry.IsVisible)
            {
                await RepsEntry.HideKeyboardAsync();
            }
            if (SetsEntry.IsVisible)
            {
                await SetsEntry.HideKeyboardAsync();
            }
        }
    }

    private async void OnExerciseImageClicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton button ||
            button.BindingContext is not WorkoutRecommendation recommendation)
        {
            return;
        }

        await ShowExerciseImageAsync(recommendation.Workout.Name);
    }

    private async void OnSelectedExerciseImageClicked(object sender, EventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            var selectedRecommendation = vm.SelectedRecommendationItem;
            await ShowExerciseImageAsync(vm.QuickEditExerciseName);

            if (vm.SelectedRecommendationItem == null && !vm.IsManualWorkoutEntryActive)
            {
                vm.RestoreSelectedRecommendation(selectedRecommendation);
            }
        }
    }

    private void ResistanceAdjust_Pressed(object sender, EventArgs e)
    {
        if (TryGetResistanceDelta(sender, out var delta))
        {
            StartResistanceAdjustment(delta);
        }
    }

    private void ResistanceAdjust_Clicked(object sender, EventArgs e)
    {
        if (_hasRepeatedResistanceAdjustment)
        {
            return;
        }

        if (BindingContext is WorkoutViewModel vm && TryGetResistanceDelta(sender, out var delta))
        {
            vm.AdjustResistanceAdjustment(delta);
        }
    }

    private void ResistanceAdjust_Released(object sender, EventArgs e)
    {
        _resistanceAdjustCancellationTokenSource?.Cancel();
        _resistanceAdjustCancellationTokenSource?.Dispose();
        _resistanceAdjustCancellationTokenSource = null;
    }

    private void UpdateRecommendationsHeight()
    {
        if (RecommendationsList == null || Height <= 0)
        {
            return;
        }

        var targetHeight = Math.Max(220, Height - 420);
        RecommendationsList.HeightRequest = targetHeight;
    }

#if ANDROID
    private void OnRecommendationsListHandlerChanged(object? sender, EventArgs e)
    {
        if (RecommendationsList.Handler?.PlatformView is PlatformView platformView)
        {
            platformView.Touch -= OnRecommendationsListTouch;
            platformView.Touch += OnRecommendationsListTouch;
        }
    }

    private void OnRecommendationsListTouch(object? sender, PlatformView.TouchEventArgs e)
    {
        if (sender is not PlatformView platformView || e.Event == null)
        {
            return;
        }

        switch (e.Event.ActionMasked)
        {
            case MotionEventActions.Down:
            case MotionEventActions.Move:
                platformView.Parent?.RequestDisallowInterceptTouchEvent(true);
                break;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                platformView.Parent?.RequestDisallowInterceptTouchEvent(false);
                break;
        }

        e.Handled = false;
    }
#endif

    private void AttachEntryCompletedHandlers(IVisualTreeElement element)
    {
        if (element is Entry entry)
        {
            entry.Completed -= OnAnyEntryCompleted;
            entry.Completed += OnAnyEntryCompleted;
        }

        foreach (var child in element.GetVisualChildren())
        {
            AttachEntryCompletedHandlers(child);
        }
    }

    private void AttachNumericEntryFocusHandlers(IVisualTreeElement element)
    {
        if (element is Entry entry && entry.Keyboard == Keyboard.Numeric)
        {
            entry.Focused -= NumericEntry_Focused;
            entry.Focused += NumericEntry_Focused;
        }

        foreach (var child in element.GetVisualChildren())
        {
            AttachNumericEntryFocusHandlers(child);
        }
    }

    private static void SelectEntryText(Entry entry)
    {
        if (string.IsNullOrEmpty(entry.Text))
        {
            return;
        }

        entry.Dispatcher.Dispatch(() =>
        {
            if (!entry.IsFocused || string.IsNullOrEmpty(entry.Text))
            {
                return;
            }

            entry.CursorPosition = 0;
            entry.SelectionLength = entry.Text.Length;
        });
    }

    private async Task ScrollExerciseSuggestionsIntoViewAsync()
    {
        if (PageScrollView == null ||
            ExerciseSuggestionsPanel == null ||
            !ExerciseSuggestionsPanel.IsVisible)
        {
            return;
        }

        await Task.Delay(150);

        if (PageScrollView == null ||
            ExerciseSuggestionsPanel == null ||
            !ExerciseSuggestionsPanel.IsVisible)
        {
            return;
        }

        await PageScrollView.ScrollToAsync(ExerciseSuggestionsPanel, ScrollToPosition.Center, true);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not WorkoutViewModel vm)
        {
            return;
        }

        if (e.PropertyName == nameof(WorkoutViewModel.SelectedRecommendationItem) &&
            vm.SelectedRecommendationItem == null)
        {
            if (!vm.IsManualWorkoutEntryActive)
            {
                EnsureDefaultRecommendationSelected();
            }
            return;
        }

        if (e.PropertyName == nameof(WorkoutViewModel.SelectedRecommendationItem) &&
            vm.SelectedRecommendationItem != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecommendationsList?.ScrollTo(vm.SelectedRecommendationItem, position: ScrollToPosition.Center, animate: true);
            });
            return;
        }

        if (e.PropertyName == nameof(WorkoutViewModel.IsManualWorkoutEntryActive) &&
            vm.IsManualWorkoutEntryActive)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (PageScrollView != null && ManualWorkoutEditorSection != null)
                {
                    await Task.Delay(50);
                    await PageScrollView.ScrollToAsync(ManualWorkoutEditorSection, ScrollToPosition.Start, true);
                }
            });
            return;
        }

        if (e.PropertyName == nameof(WorkoutViewModel.SelectedMuscleGroup) &&
            vm.IsManualWorkoutEntryActive &&
            vm.CanEditExerciseName)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(125);

                if (ExerciseEntry == null || !ExerciseEntry.IsEnabled || !ExerciseEntry.IsVisible)
                {
                    return;
                }

                ExerciseEntry.Focus();
            });
        }
    }

    private void EnsureDefaultRecommendationSelected()
    {
        if (BindingContext is not WorkoutViewModel vm)
        {
            return;
        }

        if (vm.SelectedRecommendationItem != null)
        {
            return;
        }

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => vm.SelectFirstRecommendedWorkout());
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => vm.SelectFirstRecommendedWorkout());
    }

    private void StartResistanceAdjustment(double delta)
    {
        ResistanceAdjust_Released(this, EventArgs.Empty);
        _hasRepeatedResistanceAdjustment = false;

        var cancellationTokenSource = new CancellationTokenSource();
        _resistanceAdjustCancellationTokenSource = cancellationTokenSource;
        _ = RepeatResistanceAdjustmentAsync(delta, cancellationTokenSource.Token);
    }

    private async Task RepeatResistanceAdjustmentAsync(double delta, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
            _hasRepeatedResistanceAdjustment = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (BindingContext is WorkoutViewModel vm)
                    {
                        vm.AdjustResistanceAdjustment(delta);
                    }
                });
                await Task.Delay(90, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private static bool TryGetResistanceDelta(object? sender, out double delta)
    {
        delta = 0;

        if (sender is not Button button || button.CommandParameter is null)
        {
            return false;
        }

        return double.TryParse(button.CommandParameter.ToString(), out delta);
    }

    private async Task ShowExerciseImageAsync(string? exerciseName)
    {
        if (!ExerciseInfoCatalog.HasInfo(exerciseName))
        {
            await DisplayAlert("Exercise Info Not Available", "No exercise details have been added for this exercise yet.", "OK");
            return;
        }

        await Navigation.PushModalAsync(new ExerciseImagePage(exerciseName!.Trim()));
    }

    private async Task<bool> PromptForBodyWeightAsync(
        WorkoutViewModel vm,
        string message,
        bool useCurrentWeightAsInitialValue)
    {
        var initialWeight = useCurrentWeightAsInitialValue
            ? _bodyWeightService.GetBodyWeight()?.ToString("0.#") ?? string.Empty
            : string.Empty;

        var result = await BodyWeightPromptPage.ShowAsync(
            this,
            "Body Weight",
            message,
            initialWeight,
            workoutPlansButtonText: "Go To Workout Plans");

        if (result == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(result.WeightText))
        {
            if (!InputSanitizer.TryParseBodyWeight(result.WeightText, out var bodyWeight))
            {
                await DisplayAlert("Invalid Weight", "Enter a valid body weight greater than 0.", "OK");
                return false;
            }

            await _bodyWeightService.SetBodyWeightAsync(bodyWeight);
            vm.RefreshBodyweightState();
        }

        if (result.NavigateToWorkoutPlans)
        {
            await Shell.Current.GoToAsync("//workout-plans");
            return true;
        }

        return false;
    }

    private static void ClampEntryText(object sender, string? newTextValue, double maxValue, bool isDecimal)
    {
        if (sender is not Entry entry)
        {
            return;
        }

        var sanitized = isDecimal
            ? InputSanitizer.SanitizePositiveDecimalText(newTextValue, maxValue)
            : InputSanitizer.SanitizePositiveIntegerText(newTextValue, (int)maxValue);

        if (!string.Equals(entry.Text, sanitized, StringComparison.Ordinal))
        {
            entry.Text = sanitized;
        }
    }

    private void StandardWeightAdjust_Pressed(object sender, EventArgs e)
    {
        if (TryGetResistanceDelta(sender, out var delta))
        {
            StartStandardWeightAdjustment(delta);
        }
    }

    private void StandardWeightAdjust_Clicked(object sender, EventArgs e)
    {
        if (_hasRepeatedStandardWeightAdjustment)
        {
            return;
        }

        if (BindingContext is WorkoutViewModel vm && TryGetResistanceDelta(sender, out var delta))
        {
            vm.AdjustDisplayedWeight(delta);
        }
    }

    private void StandardWeightAdjust_Released(object sender, EventArgs e)
    {
        _standardWeightAdjustCancellationTokenSource?.Cancel();
        _standardWeightAdjustCancellationTokenSource?.Dispose();
        _standardWeightAdjustCancellationTokenSource = null;
    }

    private void StartStandardWeightAdjustment(double delta)
    {
        StandardWeightAdjust_Released(this, EventArgs.Empty);
        _hasRepeatedStandardWeightAdjustment = false;

        var cancellationTokenSource = new CancellationTokenSource();
        _standardWeightAdjustCancellationTokenSource = cancellationTokenSource;
        _ = RepeatStandardWeightAdjustmentAsync(delta, cancellationTokenSource.Token);
    }

    private async Task RepeatStandardWeightAdjustmentAsync(double delta, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
            _hasRepeatedStandardWeightAdjustment = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (BindingContext is WorkoutViewModel vm)
                    {
                        vm.AdjustDisplayedWeight(delta);
                    }
                });
                await Task.Delay(90, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void BodyweightWeightAdjust_Pressed(object sender, EventArgs e)
    {
        if (TryGetResistanceDelta(sender, out var delta))
        {
            StartBodyweightWeightAdjustment(delta);
        }
    }

    private void BodyweightWeightAdjust_Clicked(object sender, EventArgs e)
    {
        if (_hasRepeatedBodyweightWeightAdjustment)
        {
            return;
        }

        if (BindingContext is WorkoutViewModel vm && TryGetResistanceDelta(sender, out var delta))
        {
            vm.AdjustBodyweightDisplayedWeight(delta);
        }
    }

    private void BodyweightWeightAdjust_Released(object sender, EventArgs e)
    {
        _bodyweightWeightAdjustCancellationTokenSource?.Cancel();
        _bodyweightWeightAdjustCancellationTokenSource?.Dispose();
        _bodyweightWeightAdjustCancellationTokenSource = null;
    }

    private void StartBodyweightWeightAdjustment(double delta)
    {
        BodyweightWeightAdjust_Released(this, EventArgs.Empty);
        _hasRepeatedBodyweightWeightAdjustment = false;

        var cancellationTokenSource = new CancellationTokenSource();
        _bodyweightWeightAdjustCancellationTokenSource = cancellationTokenSource;
        _ = RepeatBodyweightWeightAdjustmentAsync(delta, cancellationTokenSource.Token);
    }

    private async Task RepeatBodyweightWeightAdjustmentAsync(double delta, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
            _hasRepeatedBodyweightWeightAdjustment = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (BindingContext is WorkoutViewModel vm)
                    {
                        vm.AdjustBodyweightDisplayedWeight(delta);
                    }
                });
                await Task.Delay(90, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}
