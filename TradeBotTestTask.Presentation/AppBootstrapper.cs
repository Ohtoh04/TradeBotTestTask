using System.IO;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeBotTestTask.Application;
using TradeBotTestTask.Infrastructure.Extensions;
using TradeBotTestTask.Presentation.ViewModels;

namespace TradeBotTestTask.Presentation;

public class AppBootstrapper : BootstrapperBase
{
    private ServiceProvider _services = null!;
    public AppBootstrapper()
    {
        Initialize();
    }

    protected override void Configure()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration configuration = builder.Build();

        var serviceCollection = new ServiceCollection();

        // Caliburn services
        serviceCollection.AddSingleton<IWindowManager, WindowManager>();
        serviceCollection.AddSingleton<IEventAggregator, EventAggregator>();
        serviceCollection.AddTransient<SequentialResult>();

        serviceCollection.AddInfrastructureExtensions(configuration);
        serviceCollection.AddApplicationExtensions(configuration);


        // View‑models
        serviceCollection.AddSingleton<ShellViewModel>();
        serviceCollection.AddSingleton<PortfolioViewModel>();

        _services = serviceCollection.BuildServiceProvider();
    }


    protected override void OnStartup(object sender, StartupEventArgs e) =>
        DisplayRootViewForAsync<ShellViewModel>();

    protected override object GetInstance(Type service, string key)
    {
        var instance = _services.GetService(service);
        if (instance != null)
            return instance;

        throw new Exception($"Could not locate any instances of contract {key ?? service.Name}.");
    }

    protected override IEnumerable<object?> GetAllInstances(Type service)
    {
        return _services.GetServices(service);
    }
}
