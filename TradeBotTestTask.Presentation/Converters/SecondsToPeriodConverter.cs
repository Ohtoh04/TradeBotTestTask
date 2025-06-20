using System.Globalization;
using System.Windows.Data;
using TradeBotTestTask.Shared.Enums;

namespace TradeBotTestTask.Presentation.Converters;

public class SecondsToPeriodConverter : IValueConverter
{
    public object Convert(object value, Type _, object __, CultureInfo ___)
        => value is int s ? s switch
        {
            60 => Period.OneMinute,
            5 * 60 => Period.FiveMinutes,
            15 * 60 => Period.FifteenMinutes,
            30 * 60 => Period.ThirtyMinutes,
            60 * 60 => Period.OneHour,
            3 * 60 * 60 => Period.ThreeHours,
            6 * 60 * 60 => Period.SixHours,
            12 * 60 * 60 => Period.TwelveHours,
            24 * 60 * 60 => Period.OneDay,
            7 * 24 * 60 * 60 => Period.OneWeek,
            14 * 24 * 60 * 60 => Period.FourteenDays,
            30 * 24 * 60 * 60 => Period.OneMonth,
            _ => Period.OneMinute
        }
        : Period.OneMinute;

    public object ConvertBack(object value, Type _, object __, CultureInfo ___)
        => value is Period p ? p switch
        {
            Period.OneMinute => 60,
            Period.FiveMinutes => 5 * 60,
            Period.FifteenMinutes => 15 * 60,
            Period.ThirtyMinutes => 30 * 60,
            Period.OneHour => 60 * 60,
            Period.ThreeHours => 3 * 60 * 60,
            Period.SixHours => 6 * 60 * 60,
            Period.TwelveHours => 12 * 60 * 60,
            Period.OneDay => 24 * 60 * 60,
            Period.OneWeek => 7 * 24 * 60 * 60,
            Period.FourteenDays => 14 * 24 * 60 * 60,
            Period.OneMonth => 30 * 24 * 60 * 60,
            _ => 0
        }
        : 0;
}
