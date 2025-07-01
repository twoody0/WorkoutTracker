using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkoutTracker.ViewModels;

/// <summary>
/// Base class for all ViewModels implementing INotifyPropertyChanged.
/// Provides utility methods for property change notifications.
/// </summary>
public class BaseViewModel : INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Notifies listeners that a property value has changed.
    /// </summary>
    /// <param name="propertyName">The name of the changed property.</param>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a property's backing field and raises OnPropertyChanged if the value changed.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="backingStore">Reference to the backing field.</param>
    /// <param name="value">New value to set.</param>
    /// <param name="propertyName">Name of the property (automatically filled).</param>
    /// <returns>True if the value was changed, false otherwise.</returns>
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
