using TradeBotTestTask.Domain.Entities;
using TradeBotTestTask.Application.Models.Trades;

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
}
