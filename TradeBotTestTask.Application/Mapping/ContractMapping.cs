using TradeBotTestTask.Domain.Entities;
using TradeBotTestTask.Application.Models.Trades;
using TradeBotTestTask.Application.Models.Candles;

namespace TradeBotTestTask.Application.Mapping;

public static class ContractMapping
{
    public static Trade ToTrade(this BitfinexTradeDto dto, string pair)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        if (pair is null)
            throw new ArgumentNullException(nameof(pair));

        return new Trade
        {
            Pair = pair,
            Price = dto.Price,
            Amount = Math.Abs(dto.Amount),
            Side = dto.Amount >= 0 ? "buy" : "sell",
            Time = DateTimeOffset.FromUnixTimeMilliseconds(dto.Mts),
            Id = dto.Id.ToString()
        };
    }

    public static IEnumerable<Trade> ToTrade(this IEnumerable<BitfinexTradeDto> dtos, string pair)
    {
        if (dtos is null)
            throw new ArgumentNullException(nameof(dtos));

        if (pair is null)
            throw new ArgumentNullException(nameof(pair));

        return dtos.Select(dto => dto.ToTrade(pair));
    }

    public static Candle ToCandle(this BitfinexCandleDto dto, string pair)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        if (pair is null)
            throw new ArgumentNullException(nameof(pair));

        return new Candle
        {
            Pair = pair,
            OpenPrice = dto.Open,
            ClosePrice = dto.Close,
            HighPrice = dto.High,
            LowPrice = dto.Low,
            TotalVolume = dto.Volume,
            TotalPrice = dto.Close * dto.Volume,
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(dto.Mts)
        };
    }

    public static IEnumerable<Candle> ToCandle(this IEnumerable<BitfinexCandleDto> dtos, string pair)
    {
        if (dtos is null)
            throw new ArgumentNullException(nameof(dtos));

        if (pair is null)
            throw new ArgumentNullException(nameof(pair));

        return dtos.Select(dto => dto.ToCandle(pair));
    }
}
