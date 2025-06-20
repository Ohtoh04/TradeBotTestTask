using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using TradeBotTestTask.Application.Mapping;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Application.Models.Trades;
using TradeBotTestTask.Application.Services.Interfaces;
using TradeBotTestTask.Domain.Entities;
using TradeBotTestTask.Shared.Extensions;
using TradeBotTestTask.Shared.Utils;

namespace TradeBotTestTask.Infrastructure.Services.Bitfinex;

//1. Класс клиента для REST API биржи Bitfinex, который реализует 2 функции: !!!! 2 !!!!
//○ Получение трейдов(trades) - 1
//○ Получение свечей(candles) - 2
//○ Получение информации о тикере(Ticker) - ???? Вобщем, там все равно один гет запросик выслать и на формочке отобразить, так что это опустим
public class BitfinexRestClient : IBitfinexRestClient
{
    private readonly HttpClient _httpClient;

    public BitfinexRestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(
        GetCandleSeriesModel model)
    {
        var path = $"candles/trade:{PeriodFactory.FromSeconds(model.PeriodInSec).GetDescription()}:{model.Pair}/hist";

        var queryBuilder = new QueryBuilder { { "sort", "1" } };

        if (model.From.HasValue)
            queryBuilder.Add("start", model.From.Value.ToUnixTimeMilliseconds().ToString());

        if (model.To.HasValue)
            queryBuilder.Add("end", model.To.Value.ToUnixTimeMilliseconds().ToString());

        if (model.Count.HasValue)
            queryBuilder.Add("limit", model.Count.Value.ToString());

        var uri = path + queryBuilder.ToQueryString();

        using var resp = await _httpClient.GetAsync(uri);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync();

        var dtos = await JsonSerializer.DeserializeAsync<BitfinexCandleDto[]>(stream)
                   ?? Array.Empty<BitfinexCandleDto>();

        return dtos.ToCandle(model.Pair!);
    }


    public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
    {
        var path = $"trades/{pair}/hist";
        var queryBuilder = new QueryBuilder { { "limit", maxCount.ToString() }, { "sort", "1" } };
        var uri = path + queryBuilder.ToQueryString();

        using var res = await _httpClient.GetAsync(uri);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync();

        var trades = await JsonSerializer.DeserializeAsync<BitfinexTradeDto[]>(stream)
                        ?? Array.Empty<BitfinexTradeDto>();

        return trades.ToTrade(pair);
    }

    public async Task<decimal> GetConversionRateAsync(string fromCcy, string toCcy)
    {
        const string uri = "calc/fx";

        var payload = JsonSerializer.Serialize(new
        {
            ccy1 = fromCcy,
            ccy2 = toCcy
        });

        using var body = new StringContent(payload, Encoding.UTF8, "application/json");
        using var result = await _httpClient.PostAsync(uri, body).ConfigureAwait(false);

        result.EnsureSuccessStatusCode();

        var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
        var values = JsonSerializer.Deserialize<decimal[]>(json);

        return values?.Length > 0 ? values[0]
            : throw new InvalidOperationException("Bitfinex /calc/fx returned no data.");
    }

}
