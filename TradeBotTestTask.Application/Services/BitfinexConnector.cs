using System.Collections.Concurrent;
using ConnectorTest;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Application.Services.Interfaces;
using TradeBotTestTask.Domain.Entities;

namespace TradeBotTestTask.Application.Services;

public class BitfinexConnector : ITestConnector, IDisposable
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

    #endregion

    #region Socket

    public event Action<Trade> NewBuyTrade;
    public event Action<Trade> NewSellTrade;
    public event Action<Candle> CandleSeriesProcessing;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tradeSubs = new();
    private readonly ConcurrentDictionary<(string Pair, int Period), CancellationTokenSource> _candleSubs = new();

    public void SubscribeTrades(string pair, int maxCount = 100)
    {
        if (string.IsNullOrWhiteSpace(pair))
            throw new ArgumentException("pair required", nameof(pair));

        if (_tradeSubs.ContainsKey(pair))
            return; 

        var cts = new CancellationTokenSource();

        if (!_tradeSubs.TryAdd(pair, cts))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var t in _ws.StreamTradesAsync(pair, cts.Token))
                {
                    if (t.Side.Equals("buy", StringComparison.OrdinalIgnoreCase)) NewBuyTrade?.Invoke(t);
                    else NewSellTrade?.Invoke(t);
                }
            }
            catch (OperationCanceledException) { }
            finally { _tradeSubs.TryRemove(pair, out _); }
        }, cts.Token);
    }

    public void UnsubscribeTrades(string pair)
    {
        if (_tradeSubs.TryRemove(pair, out var cts)) cts.Cancel();
    }

    public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
    {
        var key = (pair, periodInSec);
        if (_candleSubs.ContainsKey(key)) return;
        var cts = new CancellationTokenSource();
        if (!_candleSubs.TryAdd(key, cts)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var c in _ws.StreamCandlesAsync(pair, periodInSec, cts.Token))
                    CandleSeriesProcessing?.Invoke(c);
            }
            catch (OperationCanceledException) { }
            finally { _candleSubs.TryRemove(key, out _); }
        }, cts.Token);
    }

    public void UnsubscribeCandles(string pair)
    {
        foreach (var kv in _candleSubs.Where(k => k.Key.Pair == pair).ToList())
        {
            if (_candleSubs.TryRemove(kv.Key, out var cts)) cts.Cancel();
        }
    }
    #endregion

    public void Dispose()
    {
        foreach (var c in _tradeSubs.Values) c.Cancel();
        foreach (var c in _candleSubs.Values) c.Cancel();
    }
}
