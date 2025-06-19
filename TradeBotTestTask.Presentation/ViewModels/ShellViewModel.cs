using Caliburn.Micro;
using ConnectorTest;
using System.Collections.ObjectModel;
using TradeBotTestTask.Application.Models.Candles;
using TradeBotTestTask.Domain.Entities;
using TradeBotTestTask.Domain.ValueObjects;
using TradeBotTestTask.Shared.Enums;

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
    public ObservableCollection<Candle> Candles { get; } = new();

    public ObservableCollection<Trade> FetchedTrades { get; } = new();
    public ObservableCollection<Candle> FetchedCandles { get; } = new();

    public Array CurrencyOptions => Enum.GetValues(typeof(CurrencyEnum));

    private CurrencyEnum _baseCurrency;
    public CurrencyEnum BaseCurrency
    {
        get => _baseCurrency;
        set
        {
            _baseCurrency = value;
            NotifyOfPropertyChange(() => BaseCurrency);
            UpdatePair();
        }
    }

    private CurrencyEnum _quoteCurrency;
    public CurrencyEnum QuoteCurrency
    {
        get => _quoteCurrency;
        set
        {
            _quoteCurrency = value;
            NotifyOfPropertyChange(() => QuoteCurrency);
            UpdatePair();
        }
    }

    private string _pair = "BTCUSD";
    public string Pair
    {
        get => _pair;
        set { _pair = value; NotifyOfPropertyChange(); }
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

    private bool _isFetching;
    public bool CanFetchTrades => !_isFetching;
    public bool CanFetchCandles => !_isFetching;

    public async Task FetchTrades()
    {
        if (_isFetching) return;
        try
        {
            _isFetching = true;
            NotifyOfPropertyChange(() => CanFetchTrades);
            NotifyOfPropertyChange(() => CanFetchCandles);

            var list = await _connector.GetNewTradesAsync(Pair, maxCount: 500);

            Execute.OnUIThread(() =>
            {
                FetchedTrades.Clear();
                foreach (var t in list.OrderByDescending(t => t.Time))
                    FetchedTrades.Add(t);
            });
        }
        finally
        {
            _isFetching = false;
            NotifyOfPropertyChange(() => CanFetchTrades);
            NotifyOfPropertyChange(() => CanFetchCandles);
        }
    }

    public async Task FetchCandles()
    {
        if (_isFetching) return;
        try
        {
            _isFetching = true;
            NotifyOfPropertyChange(() => CanFetchTrades);
            NotifyOfPropertyChange(() => CanFetchCandles);

            var to = DateTimeOffset.UtcNow;
            var from = to.AddHours(-2);

            var series = await _connector.GetCandleSeriesAsync(
                             pair: Pair,
                             periodInSec: 60,
                             from: from,
                             to: to);

            Execute.OnUIThread(() =>
            {
                Candles.Clear();
                foreach (var c in series)
                    Candles.Add(c);
            });
        }
        finally
        {
            _isFetching = false;
            NotifyOfPropertyChange(() => CanFetchTrades);
            NotifyOfPropertyChange(() => CanFetchCandles);
        }
    }

    private void OnTrade(Trade t) => Execute.OnUIThread(() => Trades.Insert(0, t));

    private void UpdatePair()
    {
        Pair = new CurrencyPair
        {
            BaseCurrency = BaseCurrency.ToString(),
            QuoteCurrency = QuoteCurrency.ToString()
        }.ToString();
    }

    //protected override void OnDeactivate(bool close)
    //{
    //    base.OnDeactivate(close);
    //    if (Connected) _connector.UnsubscribeTrades(Pair);
    //}
}