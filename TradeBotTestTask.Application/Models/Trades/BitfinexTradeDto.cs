using System.Text.Json.Serialization;
using TradeBotTestTask.Application.Converters;

namespace TradeBotTestTask.Application.Models.Trades;

[JsonConverter(typeof(BitfinexTradeArrayConverter))]
public record BitfinexTradeDto(
    long Id,
    long Mts,
    decimal Amount,
    decimal Price
);

