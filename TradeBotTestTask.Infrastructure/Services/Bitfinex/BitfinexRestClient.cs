using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using TradeBotTestTask.Application.Mapping;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Application.Models.Trades;
using TradeBotTestTask.Application.Services.Interfaces;
using TradeBotTestTask.Domain.Entities;

namespace TradeBotTestTask.Infrastructure.Services.Bitfinex;

public class BitfinexRestClient : IBitfinexRestClient
{
    private readonly HttpClient _httpClient;

    public BitfinexRestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(GetCandleSeriesModel model)
    {
        var path = $"candles/trade:{model.PeriodInSec}:{model.Pair}/hist";

        var queryBuilder = new QueryBuilder { { "sort", "1" } };

        if (model.From.HasValue) queryBuilder.Add("start", model.From.Value.ToUnixTimeMilliseconds().ToString());
        if (model.To.HasValue) queryBuilder.Add("end", model.To.Value.ToUnixTimeMilliseconds().ToString());
        if (model.Count.HasValue) queryBuilder.Add("limit", model.Count.Value.ToString());

        var uri = path + queryBuilder.ToQueryString();
        var candles = await _httpClient.GetFromJsonAsync<Candle[]>(uri)
                      ?? Array.Empty<Candle>();

        foreach (var c in candles)
            c.Pair = model.Pair;

        return candles;
    }

    public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
    {
        var path = $"trades/{pair}/hist";
        var queryBuilder = new QueryBuilder { { "limit", maxCount.ToString() }, { "sort", "1" } };
        var uri = path + queryBuilder.ToQueryString();

        try
        {
            using var res = await _httpClient.GetAsync(uri);
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync();

            var trades = await JsonSerializer.DeserializeAsync<BitfinexTradeDto[]>(stream)
                         ?? Array.Empty<BitfinexTradeDto>();

            return trades.ToTrade(pair);
        }
        catch ( Exception ex)
        {
            Console.WriteLine("Exception msg");
        }

        return Array.Empty<Trade>();

    }
}
