using Caliburn.Micro;
using ConnectorTest;
using System.Collections.ObjectModel;
using TradeBotTestTask.Domain.Entities;

namespace TradeBotTestTask.Presentation.ViewModels;

public class ShellViewModel : Screen
{
    private readonly ITestConnector _connector;
    public ShellViewModel(ITestConnector connector)
    {
        _connector = connector;
        _connector.NewBuyTrade += OnTrade;
        _connector.NewSellTrade += OnTrade;
    }

    public ObservableCollection<Trade> Trades { get; } = new();

    private string _pair = "BTCUSD";
    public string Pair
    {
        get => _pair;
        set { _pair = value.ToUpper(); NotifyOfPropertyChange(); }
    }

    private bool _connected;
    public bool Connected
    {
        get => _connected;
        set { _connected = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(() => CanConnect); NotifyOfPropertyChange(() => CanDisconnect); }
    }

    public bool CanConnect => !Connected;
    public bool CanDisconnect => Connected;

    public void Connect()
    {
        _connector.SubscribeTrades(Pair);
        Connected = true;
    }

    public void Disconnect()
    {
        _connector.UnsubscribeTrades(Pair);
        Connected = false;
    }

    private void OnTrade(Trade t) => Execute.OnUIThread(() => Trades.Insert(0, t));

    //protected override void OnDeactivate(bool close)
    //{
    //    base.OnDeactivate(close);
    //    if (Connected) _connector.UnsubscribeTrades(Pair);
    //}
}