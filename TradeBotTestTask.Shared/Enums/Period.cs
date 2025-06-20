using System.ComponentModel;

namespace TradeBotTestTask.Shared.Enums;

public enum Period
{
    [Description("1m")] OneMinute,
    [Description("5m")] FiveMinutes,
    [Description("15m")] FifteenMinutes,
    [Description("30m")] ThirtyMinutes,
    [Description("1h")] OneHour,
    [Description("3h")] ThreeHours,
    [Description("6h")] SixHours,
    [Description("12h")] TwelveHours,
    [Description("1D")] OneDay,
    [Description("1W")] OneWeek,
    [Description("14D")] FourteenDays,
    [Description("1M")] OneMonth
}
