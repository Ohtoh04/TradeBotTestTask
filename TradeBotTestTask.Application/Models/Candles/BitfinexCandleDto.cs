using System.Text.Json.Serialization;
using TradeBotTestTask.Application.Converters;

namespace TradeBotTestTask.Application.Models.Candles;

[JsonConverter(typeof(BitfinexCandleArrayConverter))]
public record BitfinexCandleDto (
    long Mts,
    decimal Open,
    decimal Close,
    decimal High,
    decimal Low,
    decimal Volume
);
