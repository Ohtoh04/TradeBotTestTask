using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace TradeBotTestTask.Presentation.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value == null)
            return string.Empty;

        var type = value.GetType();

        if (!type.IsEnum)
            return value.ToString() ?? string.Empty;

        var field = type.GetField(value.ToString()!);

        if (field == null)
            return value.ToString() ?? string.Empty;

        var attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                           .FirstOrDefault() as DescriptionAttribute;

        return attribute?.Description ?? value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
