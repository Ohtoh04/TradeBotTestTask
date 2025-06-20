using System.Net.WebSockets;
using System.Text.Json;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Application.Services.Interfaces;
using TradeBotTestTask.Domain.Entities;
using TradeBotTestTask.Shared.Options;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using TradeBotTestTask.Infrastructure.Observers;
using Websocket.Client;
using System.Reactive.Linq;
using TradeBotTestTask.Shared.Utils;
using TradeBotTestTask.Shared.Extensions;

namespace TradeBotTestTask.Infrastructure.Services.Bitfinex;

public sealed class BitfinexWsClient : IBitfinexWsClient, IAsyncDisposable
{
    private readonly Uri _url;

    private WebsocketClient? _tradesWs;
    private WebsocketClient? _candlesWs;

    private TradesObserver? _tradesObs;
    private CandlesObserver? _candlesObs;

    private TaskCompletionSource<int>? _tradesTcs;
    private TaskCompletionSource<int>? _candlesTcs;

    public BitfinexWsClient(IOptions<StockExchangeInfrastructureOptions> opt)
    {
        _url = !string.IsNullOrWhiteSpace(opt.Value.BaseWsUrl)
            ? new Uri(opt.Value.BaseWsUrl, UriKind.Absolute)
            : new Uri("wss://api-pub.bitfinex.com/ws/2");
    }

    public async IAsyncEnumerable<Trade> StreamTradesAsync(
        string pair,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pair)) throw new ArgumentException("pair", nameof(pair));

        var obs = await EnsureTradesSubscriptionAsync(pair, ct);
        await foreach (var t in obs.ReadAllAsync(ct))
            yield return t;
    }

    public async IAsyncEnumerable<Candle> StreamCandlesAsync(
        string pair,
        int periodSec,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (periodSec <= 0) throw new ArgumentOutOfRangeException(nameof(periodSec));

        var obs = await EnsureCandlesSubscriptionAsync(pair, periodSec, ct);
        await foreach (var c in obs.ReadAllAsync(ct))
        {
            c.Pair = pair;
            yield return c;
        }
    }

    private async Task EnsureTradesConnectedAsync()
    {
        if (_tradesWs?.IsRunning == true) return;

        _tradesWs = NewClient(HandleTradesFrame);
        await _tradesWs.StartOrFail();
    }

    private async Task EnsureCandlesConnectedAsync()
    {
        if (_candlesWs?.IsRunning == true) return;

        _candlesWs = NewClient(HandleCandlesFrame);
        await _candlesWs.StartOrFail();
    }

    private WebsocketClient NewClient(Action<JsonElement> frameHandler)
    {
        var ws = new WebsocketClient(_url)
        {
            IsReconnectionEnabled = true,
            ReconnectTimeout = TimeSpan.FromSeconds(30)
        };

        ws.MessageReceived
          .Where(m => m.MessageType == WebSocketMessageType.Text)
          .Select(m => JsonDocument.Parse(m.Text).RootElement)
          .Subscribe(frameHandler);
        return ws;
    }

    private async Task<TradesObserver> EnsureTradesSubscriptionAsync(string pair, CancellationToken ct)
    {
        if (_tradesObs is { Pair: var p } && p == pair) return _tradesObs;

        await EnsureTradesConnectedAsync();
        _tradesTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        _tradesWs!.Send(JsonSerializer.Serialize(new
        {
            @event = "subscribe",
            channel = "trades",
            symbol = pair
        }));

        var chanId = await _tradesTcs.Task.WaitAsync(ct);
        _tradesObs = new TradesObserver(chanId, pair);
        return _tradesObs;
    }

    private async Task<CandlesObserver> EnsureCandlesSubscriptionAsync(string pair, int periodSec, CancellationToken ct)
    {
        if (_candlesObs != null)
            return _candlesObs;

        await EnsureCandlesConnectedAsync();
        _candlesTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        var key = $"trade:{PeriodFactory.FromSeconds(periodSec).GetDescription()}:{pair}";
        _candlesWs!.Send(JsonSerializer.Serialize(new
        {
            @event = "subscribe",
            channel = "candles",
            key
        }));

        var chanId = await _candlesTcs.Task.WaitAsync(ct);
        _candlesObs = new CandlesObserver(chanId);
        return _candlesObs;
    }

    private void HandleTradesFrame(JsonElement f)
    {
        if (IsHeartbeat(f)) return;

        if (IsSubscribedAck(f, out var id))
        {
            _tradesTcs?.TrySetResult(id);
            return;
        }

        if (f.ValueKind == JsonValueKind.Array && f[0].GetInt32() == _tradesObs?.ChanId)
            _tradesObs.OnMessage(f);
    }

    private void HandleCandlesFrame(JsonElement f)
    {
        if (IsHeartbeat(f)) return;

        if (IsSubscribedAck(f, out var id))
        {
            _candlesTcs?.TrySetResult(id);
            return;
        }

        if (f.ValueKind == JsonValueKind.Array && f[0].GetInt32() == _candlesObs?.ChanId)
            _candlesObs.OnMessage(f);
    }

    private static bool IsHeartbeat(JsonElement f) =>
        f.ValueKind == JsonValueKind.Array &&
        f.GetArrayLength() == 2 &&
        f[1].ValueKind == JsonValueKind.String;

    private static bool IsSubscribedAck(JsonElement f, out int id)
    {
        id = 0;
        if (f.ValueKind == JsonValueKind.Object &&
            f.TryGetProperty("event", out var evt) && evt.GetString() == "subscribed")
        {
            id = f.GetProperty("chanId").GetInt32();
            return true;
        }
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_tradesWs != null)
            await _tradesWs.Stop(WebSocketCloseStatus.NormalClosure, "");

        if (_candlesWs != null)
            await _candlesWs.Stop(WebSocketCloseStatus.NormalClosure, "");

        _tradesWs?.Dispose();
        _candlesWs?.Dispose();
    }
}
