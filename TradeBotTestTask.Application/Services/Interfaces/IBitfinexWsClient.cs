using System.Runtime.CompilerServices;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Domain.Entities;

namespace TradeBotTestTask.Application.Services.Interfaces;

public interface IBitfinexWsClient
{
    IAsyncEnumerable<Trade> StreamTradesAsync(string pair, CancellationToken ct);

    IAsyncEnumerable<Candle> StreamCandlesAsync(string pair, int periodInSec, CancellationToken ct);
}
