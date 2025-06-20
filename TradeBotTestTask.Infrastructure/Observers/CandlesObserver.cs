using System.Text.Json;
using TradeBotTestTask.Application.Models.Candles;

namespace TradeBotTestTask.Infrastructure.Observers;

public class CandlesObserver : ChannelObserverBase<Candle>
{
    public CandlesObserver(int id) : base(id) { }

    protected override bool TryParse(JsonElement msg, out Candle item)
    {
        item = default!;

        if (msg.ValueKind != JsonValueKind.Array || msg.GetArrayLength() < 2)
            return false;

        var data = msg[1];

        // Bitfinex first sends a snapshot so i just take the last one
        if (data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() > 0 &&
            data[0].ValueKind == JsonValueKind.Array)
        {
            data = data[data.GetArrayLength() - 1];
        }

        if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() < 6)
            return false;

        item = new Candle
        {
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(data[0].GetInt64()),
            OpenPrice = data[1].GetDecimal(),
            ClosePrice = data[2].GetDecimal(),
            HighPrice = data[3].GetDecimal(),
            LowPrice = data[4].GetDecimal(),
            TotalVolume = data[5].GetDecimal(),
            TotalPrice = data[2].GetDecimal() * data[5].GetDecimal()
        };
        return true;
    }
}