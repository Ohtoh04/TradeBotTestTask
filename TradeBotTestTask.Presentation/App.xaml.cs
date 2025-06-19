using System.Configuration;
using System.Data;
using System.Windows;

namespace TradeBotTestTask.Presentation;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly AppBootstrapper _bootstrapper;

    public App()
    {
        _bootstrapper = new AppBootstrapper();
    }
}

