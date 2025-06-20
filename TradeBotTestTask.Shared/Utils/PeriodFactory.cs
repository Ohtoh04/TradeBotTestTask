using TradeBotTestTask.Shared.Enums;

namespace TradeBotTestTask.Shared.Utils;

public static class PeriodFactory
{
    public static Period FromSeconds(int seconds)
    {
        return seconds switch
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
        };
    }
}
