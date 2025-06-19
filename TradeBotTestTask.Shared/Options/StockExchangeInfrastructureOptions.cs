namespace TradeBotTestTask.Shared.Options;

public class StockExchangeInfrastructureOptions
{
    public required string BaseRestUrl { get; set; }
    public required string BaseWsUrl { get; set; }
}
