using System.Text.Json.Serialization;
using TradeBotTestTask.Application.Converters;

namespace TradeBotTestTask.Application.Models.Trades;

[JsonConverter(typeof(BitfinexTradeArrayConverter))]
public record BitfinexTradeDto(
    [property: JsonPropertyOrder(0)] long Id,
    [property: JsonPropertyOrder(1)] long Mts,
    [property: JsonPropertyOrder(2)] decimal Amount,
    [property: JsonPropertyOrder(3)] decimal Price
);

