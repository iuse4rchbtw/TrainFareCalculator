using System.Globalization;

namespace TFC.GUI.Converters;

public class FareConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not decimal fare)
            return null;
        
        return fare == 0 ? "" : fare.ToString("C");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}