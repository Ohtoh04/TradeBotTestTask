using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Domain.Entities;

namespace TradeBotTestTask.Application.Services.Interfaces;

public interface IBitfinexRestClient
{
    Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
    Task<IEnumerable<Candle>> GetCandleSeriesAsync(GetCandleSeriesModel model);
}
