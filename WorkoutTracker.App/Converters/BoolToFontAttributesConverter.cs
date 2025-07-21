using System.Globalization;

namespace WorkoutTracker.Converters;

public class BoolToFontAttributesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isActive = (bool)value;
        return isActive ? FontAttributes.Bold : FontAttributes.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
