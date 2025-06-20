using System.Text.Json;

namespace TradeBotTestTask.Infrastructure.Observers;

public interface IChannelObserver<out T>
{
    int ChanId { get; }
    IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct = default);
}