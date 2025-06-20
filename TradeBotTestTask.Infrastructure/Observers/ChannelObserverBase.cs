using System.Text.Json;
using System.Threading.Channels;
using Mapster;

namespace TradeBotTestTask.Infrastructure.Observers;

public abstract class ChannelObserverBase<T> : IChannelObserver<T>
{
    private readonly Channel<T> _channel =
        Channel.CreateUnbounded<T>(new UnboundedChannelOptions { SingleReader = true });

    protected ChannelObserverBase(int chanId) => ChanId = chanId;

    public int ChanId { get; }

    public IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);

    public void OnMessage(JsonElement element)
    {
        if (TryParse(element, out var item))
            _channel.Writer.TryWrite(item);
    }

    protected abstract bool TryParse(JsonElement json, out T item);
}