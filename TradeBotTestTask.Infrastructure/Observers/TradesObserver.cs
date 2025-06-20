using System.Text.Json;
using TradeBotTestTask.Domain.Entities;

namespace TradeBotTestTask.Infrastructure.Observers;

public class TradesObserver : ChannelObserverBase<Trade>
{
    private readonly string _pair;
    public string Pair {  get { return _pair; } }
    public TradesObserver(int id, string pair) : base(id) => _pair = pair;
    protected override bool TryParse(JsonElement msg, out Trade item)
    {
        if (msg.GetArrayLength() < 3 || msg[1].GetString() != "tu")
        {
            item = default!;
            return false;
        }

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