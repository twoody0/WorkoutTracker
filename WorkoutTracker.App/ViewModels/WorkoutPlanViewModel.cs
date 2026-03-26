using System.Collections.ObjectModel;
using WorkoutTracker.Models;
using WorkoutTracker.Services;
using WorkoutTracker.Views;

namespace WorkoutTracker.ViewModels;

public class WorkoutPlanViewModel : BaseViewModel
{
    private const string AllCategoriesOption = "All Categories";
    private const string CustomCategory = "Custom";

    private readonly IWorkoutPlanService _workoutPlanService;
    private readonly IWorkoutScheduleService _scheduleService;
    private string _newPlanName = string.Empty;
    private string _newPlanDescription = string.Empty;
    private string _newPlanDurationInWeeks = "4";
    private string _selectedCategory = AllCategoriesOption;
    private string _selectedNewPlanCategory = CustomCategory;
    private bool _isCreatePlanVisible;

    public ObservableCollection<WorkoutPlan> WorkoutPlans { get; set; } = new();
    public ObservableCollection<WorkoutPlanCategoryGroup> GroupedWorkoutPlans { get; } = new();
    public ObservableCollection<string> AvailableCategories { get; } = new();
    public ObservableCollection<string> AvailableNewPlanCategories { get; } = new();
    private List<WorkoutPlan> AllPlans { get; set; } = new();

    public WorkoutPlan? CurrentPlan => _scheduleService.ActivePlan;
    public bool HasActivePlan => _scheduleService.ActivePlan != null;
    public string CurrentPlanTimelineSummary => _scheduleService.GetActivePlanTimelineSummary();
    public bool ShowGroupedWorkoutPlans => SelectedCategory == AllCategoriesOption;
    public bool ShowFlatWorkoutPlans => !ShowGroupedWorkoutPlans;

    public string NewPlanName
    {
        get => _newPlanName;
        set => SetProperty(ref _newPlanName, value);
    }

    public string NewPlanDescription
    {
        get => _newPlanDescription;
        set => SetProperty(ref _newPlanDescription, value);
    }

    public string NewPlanDurationInWeeks
    {
        get => _newPlanDurationInWeeks;
        set => SetProperty(ref _newPlanDurationInWeeks, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                RefreshWorkoutPlans();
            }
        }
    }

    public string SelectedNewPlanCategory
    {
        get => _selectedNewPlanCategory;
        set => SetProperty(ref _selectedNewPlanCategory, value);
    }

    public bool IsCreatePlanVisible
    {
        get => _isCreatePlanVisible;
        set
        {
            if (SetProperty(ref _isCreatePlanVisible, value))
            {
                OnPropertyChanged(nameof(CreatePlanButtonText));
            }
        }
    }

    public string CreatePlanButtonText => IsCreatePlanVisible ? "Cancel" : "Create Your Own Plan";

    public Command AddWorkoutPlanCommand { get; }
    public Command SelectWorkoutPlanCommand { get; }
    public Command ToggleCreatePlanCommand { get; }

    public WorkoutPlanViewModel(IWorkoutPlanService workoutPlanService, IWorkoutScheduleService scheduleService)
    {
        _workoutPlanService = workoutPlanService;
        _scheduleService = scheduleService;

        AddWorkoutPlanCommand = new Command(AddWorkoutPlan);
        SelectWorkoutPlanCommand = new Command<WorkoutPlan>(SelectWorkoutPlan);
        ToggleCreatePlanCommand = new Command(ToggleCreatePlan);
        LoadWorkoutPlans();
    }

    private void LoadWorkoutPlans()
    {
        AllPlans = _workoutPlanService.GetWorkoutPlans().ToList();
        RefreshCategories();
        RefreshWorkoutPlans();
    }

    private void RefreshCategories()
    {
        var categories = AllPlans
            .Select(plan => string.IsNullOrWhiteSpace(plan.Category) ? CustomCategory : plan.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category)
            .ToList();

        AvailableCategories.Clear();
        AvailableCategories.Add(AllCategoriesOption);

        foreach (var category in categories)
        {
            AvailableCategories.Add(category);
        }

        AvailableNewPlanCategories.Clear();
        foreach (var category in categories)
        {
            AvailableNewPlanCategories.Add(category);
        }

        if (!AvailableNewPlanCategories.Contains(CustomCategory))
        {
            AvailableNewPlanCategories.Add(CustomCategory);
        }

        if (!AvailableCategories.Contains(SelectedCategory))
        {
            _selectedCategory = AllCategoriesOption;
            OnPropertyChanged(nameof(SelectedCategory));
        }

        if (!AvailableNewPlanCategories.Contains(SelectedNewPlanCategory))
        {
            _selectedNewPlanCategory = CustomCategory;
            OnPropertyChanged(nameof(SelectedNewPlanCategory));
        }
    }

    private void RefreshWorkoutPlans()
    {
        WorkoutPlans.Clear();
        GroupedWorkoutPlans.Clear();

        var filteredPlans = AllPlans
            .Where(plan => plan != _scheduleService.ActivePlan)
            .Where(plan => SelectedCategory == AllCategoriesOption ||
                           string.Equals(plan.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var plan in filteredPlans)
        {
            WorkoutPlans.Add(plan);
        }

        foreach (var group in filteredPlans
                     .GroupBy(plan => string.IsNullOrWhiteSpace(plan.Category) ? CustomCategory : plan.Category)
                     .OrderBy(group => group.Key))
        {
            GroupedWorkoutPlans.Add(new WorkoutPlanCategoryGroup(group.Key, group));
        }

        OnPropertyChanged(nameof(CurrentPlan));
        OnPropertyChanged(nameof(HasActivePlan));
        OnPropertyChanged(nameof(CurrentPlanTimelineSummary));
        OnPropertyChanged(nameof(ShowGroupedWorkoutPlans));
        OnPropertyChanged(nameof(ShowFlatWorkoutPlans));
    }

    private async void SelectWorkoutPlan(WorkoutPlan plan)
    {
        if (plan == null)
            return;

        // If selected plan is already active, go straight to WeeklySchedulePage
        if (_scheduleService.ActivePlan != null && _scheduleService.ActivePlan == plan)
        {
            var schedulePage = App.Services.GetRequiredService<WeeklySchedulePage>();
            await Shell.Current.Navigation.PushAsync(schedulePage);
        }
        else
        {
            // Otherwise, show details page first
            var detailsPage = new WorkoutPlanDetailsPage(
                App.Services.GetRequiredService<WorkoutPlanDetailsViewModel>(), plan);
            await Shell.Current.Navigation.PushAsync(detailsPage);
        }
    }

    private void AddWorkoutPlan()
    {
        if (string.IsNullOrWhiteSpace(NewPlanName)) return;
        if (!int.TryParse(NewPlanDurationInWeeks, out var durationInWeeks) || durationInWeeks <= 0)
        {
            durationInWeeks = 4;
        }

        var newPlan = new WorkoutPlan
        {
            Name = NewPlanName,
            Description = NewPlanDescription,
            Category = string.IsNullOrWhiteSpace(SelectedNewPlanCategory) ? CustomCategory : SelectedNewPlanCategory,
            DurationInWeeks = durationInWeeks,
            IsCustom = true
        };

        _workoutPlanService.AddWorkoutPlan(newPlan);
        AllPlans.Add(newPlan);

        RefreshCategories();
        RefreshWorkoutPlans();

        NewPlanName = string.Empty;
        NewPlanDescription = string.Empty;
        NewPlanDurationInWeeks = "4";
        SelectedNewPlanCategory = newPlan.Category;
        IsCreatePlanVisible = false;
    }

    private void ToggleCreatePlan()
    {
        if (IsCreatePlanVisible)
        {
            NewPlanName = string.Empty;
            NewPlanDescription = string.Empty;
            NewPlanDurationInWeeks = "4";
            SelectedNewPlanCategory = CustomCategory;
        }

        IsCreatePlanVisible = !IsCreatePlanVisible;
    }

    public void RefreshActivePlan()
    {
        RefreshWorkoutPlans();
    }
}

public class WorkoutPlanCategoryGroup : List<WorkoutPlan>
{
    public string CategoryName { get; }

    public WorkoutPlanCategoryGroup(string categoryName, IEnumerable<WorkoutPlan> plans)
        : base(plans)
    {
        CategoryName = categoryName;
    }
}
