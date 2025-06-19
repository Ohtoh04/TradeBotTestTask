using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeBotTestTask.Application.Services.Interfaces;
using TradeBotTestTask.Infrastructure.Services.Bitfinex;
using TradeBotTestTask.Shared.Options;

namespace TradeBotTestTask.Infrastructure.Extensions;


public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StockExchangeInfrastructureOptions>(configuration.GetSection(nameof(StockExchangeInfrastructureOptions)));

        services.AddHttpClient<IBitfinexRestClient, BitfinexRestClient>(client =>
        {
            client.BaseAddress = new Uri(configuration.GetSection(nameof(StockExchangeInfrastructureOptions)).GetValue<string>("BaseRestUrl") ?? "https://ipinfo.io/");
        });

        services.AddSingleton<IBitfinexWsClient, BitfinexWsClient>();

        return services;
    }
}
