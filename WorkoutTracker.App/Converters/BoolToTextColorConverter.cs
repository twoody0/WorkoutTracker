using System.Globalization;

namespace WorkoutTracker.Converters;

public class BoolToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value as bool? ?? false;
        return isActive ? Colors.White : Colors.White; // Both stay white
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
