using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Domain.Entities;

namespace ConnectorTest;

public interface ITestConnector
{
    #region Rest

    Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
    // What do you think about primitive obsession?
    Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);
    //Added this, becuase i see no other way of calculating portfolio prices without it
    Task<decimal> ConvertCurrencyAsync(string fromCcy, string toCcy, decimal amount);

    #endregion

    #region Socket

    event Action<Trade> NewBuyTrade;
    event Action<Trade> NewSellTrade;
    void SubscribeTrades(string pair, int maxCount = 100);
    void UnsubscribeTrades(string pair);

    event Action<Candle> CandleSeriesProcessing;
    void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0);
    void UnsubscribeCandles(string pair);

    #endregion

}
