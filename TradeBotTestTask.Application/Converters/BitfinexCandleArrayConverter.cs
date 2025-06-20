using System.Text.Json.Serialization;
using System.Text.Json;
using TradeBotTestTask.Application.Models.Candles;

namespace TradeBotTestTask.Application.Converters;

public class BitfinexCandleArrayConverter : JsonConverter<BitfinexCandleDto>
{
    public override BitfinexCandleDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected [");

        reader.Read(); var mts = reader.GetInt64();
        reader.Read(); var open = reader.GetDecimal();
        reader.Read(); var close = reader.GetDecimal();
        reader.Read(); var high = reader.GetDecimal();
        reader.Read(); var low = reader.GetDecimal();
        reader.Read(); var volume = reader.GetDecimal();
        reader.Read(); // for closing tag ]

        return new(
            mts,
            open,
            close,
            high,
            low,
            volume
        );
    }

    public override void Write(Utf8JsonWriter writer, BitfinexCandleDto value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Mts);
        writer.WriteNumberValue(value.Open);
        writer.WriteNumberValue(value.Close);
        writer.WriteNumberValue(value.High);
        writer.WriteNumberValue(value.Low);
        writer.WriteNumberValue(value.Volume);
        writer.WriteEndArray();
    }
}