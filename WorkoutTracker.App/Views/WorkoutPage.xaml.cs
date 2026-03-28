using CommunityToolkit.Maui.Core.Platform;
using System.Linq;
using System.ComponentModel;
using WorkoutTracker.Models;
using WorkoutTracker.ViewModels;
using WorkoutTracker.Helpers;
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

    public WorkoutPage(WorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.PropertyChanged += OnViewModelPropertyChanged;
        TabSwipeNavigationHelper.Attach(this, "add-workout");
#if ANDROID
        RecommendationsList.HandlerChanged += OnRecommendationsListHandlerChanged;
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is WorkoutViewModel vm)
        {
            vm.RefreshPlanRecommendations();
        }

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
            await vm.UpdateExerciseSuggestionsAsync();
        }
    }

    private void ExerciseEntry_Unfocused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.IsNameFieldFocused = false;
        }
    }

    private void WeightEntry_Unfocused(object sender, FocusEventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.CommitBodyweightWeightInput();
        }
    }

    private void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is WeightliftingExercise exercise)
        {
            if (BindingContext is WorkoutViewModel vm)
            {
                vm.SelectExerciseCommand.Execute(exercise);
            }
        }
        // Clear the selection so that the same item can be selected again if needed.
        ((CollectionView)sender).SelectedItem = null;
    }
    private async void OnAddWorkoutClicked(object sender, EventArgs e)
    {
        if (BindingContext is WorkoutViewModel vm)
        {
            vm.Name = vm.ExerciseSearchQuery;
            vm.CommitBodyweightWeightInput();

            if (vm.AddWorkoutCommand.CanExecute(null))
                vm.AddWorkoutCommand.Execute(null);
        }

        // Hide keyboard
        if (!OperatingSystem.IsMacCatalyst())
        {
            await ExerciseEntry.HideKeyboardAsync();
            if (WeightEntry.IsVisible)
            {
                await WeightEntry.HideKeyboardAsync();
            }
            await RepsEntry.HideKeyboardAsync();
            await SetsEntry.HideKeyboardAsync();
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkoutViewModel.SelectedRecommendationItem) ||
            sender is not WorkoutViewModel vm ||
            vm.SelectedRecommendationItem == null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RecommendationsList?.ScrollTo(vm.SelectedRecommendationItem, position: ScrollToPosition.Center, animate: true);
        });
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
