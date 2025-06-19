using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System.Threading.Channels;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Application.Services.Interfaces;
using TradeBotTestTask.Domain.Entities;
using TradeBotTestTask.Shared.Options;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace TradeBotTestTask.Infrastructure.Services.Bitfinex;

public sealed class BitfinexWsClient : IBitfinexWsClient, IDisposable
{
    private readonly Uri _wsEndpoint = new("wss://api-pub.bitfinex.com/ws/2");

    private ClientWebSocket? _socket;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private readonly ConcurrentDictionary<int, IChannelObserver> _subscriptions = new();

    public BitfinexWsClient(IOptions<StockExchangeInfrastructureOptions> options)
    {
        if (!String.IsNullOrEmpty(options.Value.BaseWsUrl))
            _wsEndpoint = new(options.Value.BaseWsUrl);
    }

    public async IAsyncEnumerable<Trade> StreamTradesAsync(string pair, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pair))
            throw new ArgumentException("pair is required", nameof(pair));

        var channel = await EnsureSubscriptionAsync("trades", NormalizePair(pair), ct);

        await foreach (var item in channel.GetAsyncEnumerable<Trade>(ct))
            yield return item;
    }

    public async IAsyncEnumerable<Candle> StreamCandlesAsync(string pair, int periodInSec, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var key = $"trade:{periodInSec}:{NormalizePair(pair)}";
        var channel = await EnsureSubscriptionAsync("candles", key, ct);

        await foreach (var item in channel.GetAsyncEnumerable<Candle>(ct))
        {
            item.Pair = pair;
            yield return item;
        }
    }

    private async Task<IChannelObserver> EnsureSubscriptionAsync(string channel, string symbolOrKey, CancellationToken ct)
    {
        await EnsureConnectedAsync(ct);

        var subscribe = new
        {
            @event = "subscribe",
            channel = channel,
            symbol = channel == "trades" ? symbolOrKey : null,
            key = channel == "candles" ? symbolOrKey : null
        };

        var payload = JsonSerializer.Serialize(subscribe);
        await _socket!.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, ct);

        while (true)
        {
            var msg = await ReadFrameAsync(ct);
            if (msg.TryGetProperty("event", out var ev) && ev.GetString() == "subscribed")
            {
                if (msg.GetProperty("channel").GetString() == channel &&
                    (channel == "trades" && msg.GetProperty("symbol").GetString() == symbolOrKey ||
                     channel == "candles" && msg.GetProperty("key").GetString() == symbolOrKey))
                {
                    var chanId = msg.GetProperty("chanId").GetInt32();
                    var observer = channel switch
                    {
                        // "trades" => new TradesObserver(chanId),
                        "candles" => new CandlesObserver(chanId),
                        _ => throw new InvalidOperationException()
                    };
                    _subscriptions[chanId] = observer;
                    return observer;
                }
            }
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_socket?.State == WebSocketState.Open) return;

        await _connectLock.WaitAsync(ct);
        try
        {
            if (_socket?.State == WebSocketState.Open) return; 

            _socket?.Dispose();
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(_wsEndpoint, ct);

            _ = Task.Run(() => PumpAsync(_socket, ct));
        }
        finally { _connectLock.Release(); }
    }

    private async Task PumpAsync(ClientWebSocket socket, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var frame = await ReadFrameAsync(ct);

                if (frame.ValueKind == JsonValueKind.Array && frame.GetArrayLength() == 2 && frame[1].ValueKind == JsonValueKind.String)
                    continue;

                if (frame.ValueKind == JsonValueKind.Array && frame[0].ValueKind == JsonValueKind.Number)
                {
                    var chanId = frame[0].GetInt32();
                    if (_subscriptions.TryGetValue(chanId, out var obs))
                        obs.OnMessage(frame);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Could be a logger but it is just a test task so i didnt bother
            Console.WriteLine(ex.Message, "WS pump terminated – will reconnect on next subscription");
        }
    }

    private async Task<JsonElement> ReadFrameAsync(CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1 << 16);
        try
        {
            var segment = new ArraySegment<byte>(buffer);
            var result = await _socket!.ReceiveAsync(segment, ct);
            if (result.MessageType != WebSocketMessageType.Text)
                throw new InvalidOperationException("Unexpected WS frame type");

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    private interface IChannelObserver
    {
        void OnMessage(JsonElement element);
        IAsyncEnumerable<T> GetAsyncEnumerable<T>(CancellationToken ct);
    }
    
    private sealed class TradesObserver : ChannelObserverBase<Trade>
    {
        private readonly string _pair;
        public TradesObserver(int id, string pair) : base(id) => _pair = pair;
        protected override bool TryParse(JsonElement msg, out Trade item)
        {
            if (msg.GetArrayLength() < 3 || msg[1].GetString() != "tu") { item = default!; return false; }
            var arr = msg[2];
            var id = arr[0].GetInt64();
            var mts = arr[1].GetInt64();
            var amt = arr[2].GetDecimal();
            var prc = arr[3].GetDecimal();
            item = new Trade
            {
                Id = id.ToString(),
                Pair = _pair,
                Time = DateTimeOffset.FromUnixTimeMilliseconds(mts),
                Amount = Math.Abs(amt),
                Price = prc,
                Side = amt > 0 ? "buy" : "sell"
            };
            return true;
        }
    }

    private sealed class CandlesObserver : ChannelObserverBase<Candle>
    {
        public CandlesObserver(int id) : base(id) { }
        protected override bool TryParse(JsonElement msg, out Candle item)
        {
            // Message format: [chanId, [MTS, OPEN, CLOSE, HIGH, LOW, VOLUME]]
            if (msg.GetArrayLength() < 2 || msg[1].ValueKind != JsonValueKind.Array)
            {
                item = default!; return false;
            }
            var arr = msg[1];
            var mts = arr[0].GetInt64();
            item = new Candle
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(mts),
                OpenPrice = arr[1].GetDecimal(),
                ClosePrice = arr[2].GetDecimal(),
                HighPrice = arr[3].GetDecimal(),
                LowPrice = arr[4].GetDecimal(),
                TotalVolume = arr[5].GetDecimal(),
                TotalPrice = 0m // not provided by Bitfinex
            };
            return true;
        }
    }

    private abstract class ChannelObserverBase<T> : IChannelObserver
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(new() { SingleReader = true });
        protected ChannelObserverBase(int chanId) => ChanId = chanId;
        public int ChanId { get; }

        public void OnMessage(JsonElement element)
        {
            if (TryParse(element, out var item)) _channel.Writer.TryWrite(item);
        }
        protected abstract bool TryParse(JsonElement msg, out T item);
        public IAsyncEnumerable<T> GetAsyncEnumerable<T>(CancellationToken ct) => throw new NotImplementedException();
            //_channel.Reader.ReadAllAsync(ct).Adapt<T>();
    }

    private static string NormalizePair(string p) =>
        "t" + p.Replace("/", string.Empty).Replace("-", string.Empty).ToUpperInvariant();

    public void Dispose()
    {
        try 
        {
            _socket?.Abort();
            _socket?.Dispose();
        }
        catch { }

        _connectLock.Dispose();
    }
}