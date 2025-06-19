using ConnectorTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeBotTestTask.Application.Services;

namespace TradeBotTestTask.Application;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITestConnector, BitfinexConnector>();

        return services;
    }
}
