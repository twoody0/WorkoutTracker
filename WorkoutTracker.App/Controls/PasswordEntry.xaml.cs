using System.Windows.Input;

namespace WorkoutTracker.Controls;

public partial class PasswordEntry : ContentView
{
    public PasswordEntry()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty PasswordProperty =
        BindableProperty.Create(nameof(Password), typeof(string), typeof(PasswordEntry), string.Empty, BindingMode.TwoWay);

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(PasswordEntry), "Password");

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    private bool _isPasswordHidden = true;

    public string EyeIcon => _isPasswordHidden ? "eye_closed.png" : "eye_open.png";

    public ICommand TogglePasswordVisibilityCommand => new Command(() =>
    {
        _isPasswordHidden = !_isPasswordHidden;
        PasswordEntryBox.IsPassword = _isPasswordHidden;
        OnPropertyChanged(nameof(EyeIcon));
    });

    public static readonly BindableProperty PasswordChangedCommandProperty =
    BindableProperty.Create(nameof(PasswordChangedCommand), typeof(ICommand), typeof(PasswordEntry));

    public ICommand PasswordChangedCommand
    {
        get => (ICommand)GetValue(PasswordChangedCommandProperty);
        set => SetValue(PasswordChangedCommandProperty, value);
    }

    private void OnPasswordChanged(object sender, TextChangedEventArgs e)
    {
        SetValue(PasswordProperty, e.NewTextValue);

        // Trigger parent ViewModel validation
        PasswordChangedCommand?.Execute(Password);
    }
}
