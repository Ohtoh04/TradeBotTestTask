using System.Text.Json.Serialization;
using System.Text.Json;
using TradeBotTestTask.Application.Models.Trades;

namespace TradeBotTestTask.Application.Converters;

public class BitfinexTradeArrayConverter : JsonConverter<BitfinexTradeDto>
{
    public override BitfinexTradeDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected [");

        reader.Read(); long id = reader.GetInt64();
        reader.Read(); long mts = reader.GetInt64();
        reader.Read(); decimal amount = reader.GetDecimal();
        reader.Read(); decimal price = reader.GetDecimal();
        reader.Read(); // for closing tag ]

        return new(id, mts, amount, price);
    }

    public override void Write(Utf8JsonWriter writer, BitfinexTradeDto value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Id);
        writer.WriteNumberValue(value.Mts);
        writer.WriteNumberValue(value.Amount);
        writer.WriteNumberValue(value.Price);
        writer.WriteEndArray();
    }
}