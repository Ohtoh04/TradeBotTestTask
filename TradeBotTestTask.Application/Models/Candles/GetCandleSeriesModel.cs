namespace TradeBotTestTask.Application.Models.Candles;

public class GetCandleSeriesModel
{
    public string? Pair { get; set; }
    public int PeriodInSec { get; set; } = 60;
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public long? Count { get; set; }
}
