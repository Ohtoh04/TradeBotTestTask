using System.Collections.Concurrent;
using ConnectorTest;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Application.Services.Interfaces;
using TradeBotTestTask.Domain.Entities;

namespace TradeBotTestTask.Application.Services;

public class BitfinexConnector : ITestConnector
{
    private readonly IBitfinexRestClient _rest;
    private readonly IBitfinexWsClient _ws;

    public BitfinexConnector(IBitfinexRestClient rest, IBitfinexWsClient ws)
    {
        _rest = rest;
        _ws = ws;
    }

    #region Rest

    public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
    {
        return await _rest.GetCandleSeriesAsync(new GetCandleSeriesModel {
            Count = count,
            PeriodInSec = periodInSec,
            From = from,
            To = to,
            Pair = pair,
        });
    }

    public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
    {
        return await _rest.GetNewTradesAsync(pair, maxCount);
    }

    public async Task<decimal> ConvertCurrencyAsync(string fromCcy, string toCcy, decimal amount)
    {
        var rate = await _rest.GetConversionRateAsync(fromCcy, toCcy);

        return rate * amount;
    }

    #endregion

    #region Socket

    public event Action<Trade> NewBuyTrade;
    public event Action<Trade> NewSellTrade;
    public event Action<Candle> CandleSeriesProcessing;

    private readonly CancellationTokenSource _tradesSubCts = new();
    private readonly CancellationTokenSource _candlesSubCts = new();

    public void SubscribeTrades(string pair, int maxCount = 100)
    {
        if (string.IsNullOrWhiteSpace(pair))
            throw new ArgumentException("pair required", nameof(pair));

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var t in _ws.StreamTradesAsync(pair, _candlesSubCts.Token))
                {
                    if (t.Side.Equals("buy", StringComparison.OrdinalIgnoreCase)) NewBuyTrade?.Invoke(t);
                    else NewSellTrade?.Invoke(t);
                }
            }
            catch (OperationCanceledException) { }
        }, _tradesSubCts.Token);
    }

    public void UnsubscribeTrades(string pair)
    {
        _tradesSubCts.Cancel();
    }

    public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var c in _ws.StreamCandlesAsync(pair, periodInSec, _candlesSubCts.Token))
                    CandleSeriesProcessing?.Invoke(c);
            }
            catch (OperationCanceledException) { }
        }, _candlesSubCts.Token);
    }

    public void UnsubscribeCandles(string pair)
    {
        _candlesSubCts.Cancel();

    }
    #endregion
}
