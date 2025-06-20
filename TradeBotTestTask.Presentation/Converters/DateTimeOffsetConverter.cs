using System.Globalization;
using System.Windows.Data;

namespace TradeBotTestTask.Presentation.Converters;

public class DateTimeOffsetConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
       => value is DateTimeOffset dto ? (DateTime?)dto.DateTime : null;

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DateTime dt ? (DateTimeOffset?)new DateTimeOffset(dt) : null;
}
