using Caliburn.Micro;
using ConnectorTest;
using TradeBotTestTask.Domain.ValueObjects;
using TradeBotTestTask.Presentation.Models;

namespace TradeBotTestTask.Presentation.ViewModels;

public sealed class PortfolioViewModel : Screen
{
    private readonly ITestConnector _connector;

    public PortfolioViewModel(ITestConnector connector) => _connector = connector;

    public BindableCollection<PortfolioItem> PortfolioTotals { get; } = new();

    public async Task LoadPortfolioAsync()
    {
        PortfolioTotals.Clear();

        var balances = new Dictionary<string, decimal>
        {
            ["BTC"] = 1,
            ["XRP"] = 15_000,
            ["XMR"] = 50,
            ["DASH"] = 30 // it will ignore this because binfinex doesnt seem to know about that coin (returns null)
        };

        var targetCurrencies = new[] { "USD", "BTC", "XRP", "XMR", "DASH" };

        foreach (string coin in targetCurrencies)
        {
            decimal totalValue = 0;
            foreach (var currencyBalance in balances)
            {
                try
                {
                    totalValue += await _connector.ConvertCurrencyAsync(currencyBalance.Key, coin, currencyBalance.Value);
                }
                catch
                {
                    continue;
                }
            }
            PortfolioTotals.Add(new PortfolioItem
            {
                BaseCurrency = coin,
                TotalValue = Math.Round(totalValue, 4)
            });
        }
    }

    public Task RefreshAsync() => LoadPortfolioAsync();
}