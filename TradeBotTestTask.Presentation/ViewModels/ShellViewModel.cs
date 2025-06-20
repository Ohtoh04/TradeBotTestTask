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
    private readonly IWindowManager _windowManager;
    private readonly PortfolioViewModel _portfolioVm;

    public ShellViewModel(ITestConnector connector, IWindowManager windowManager, PortfolioViewModel portfolioVm)
    {
        _connector = connector;
        _windowManager = windowManager;
        _portfolioVm = portfolioVm;

        _connector.NewBuyTrade += OnTrade;
        _connector.NewSellTrade += OnTrade;

        _connector.CandleSeriesProcessing += OnCandle;
    }

    // Shared pair related
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

    private void UpdatePair()
    {
        Pair = new CurrencyPair
        {
            BaseCurrency = BaseCurrency.ToString(),
            QuoteCurrency = QuoteCurrency.ToString()
        }.ToString();
    }

    public ObservableCollection<Trade> Trades { get; } = new();
    public ObservableCollection<Trade> FetchedTrades { get; } = new();

    private int _tradesMaxCount = 100;
    public int TradesMaxCount
    {
        get => _tradesMaxCount;
        set { _tradesMaxCount = value; NotifyOfPropertyChange(() => TradesMaxCount); }
    }

    private bool _tradesConnected;
    public bool TradesConnected
    {
        get => _tradesConnected;
        set { _tradesConnected = value; NotifyOfPropertyChange(() => TradesConnected); NotifyOfPropertyChange(() => CanConnectTrades); NotifyOfPropertyChange(() => CanDisconnectTrades); }
    }

    public bool CanConnectTrades => !TradesConnected;
    public bool CanDisconnectTrades => TradesConnected;

    public void ConnectTrades()
    {
        _connector.SubscribeTrades(Pair, TradesMaxCount);
        TradesConnected = true;
    }

    public void DisconnectTrades()
    {
        _connector.UnsubscribeTrades(Pair);
        TradesConnected = false;
    }

    private bool _isFetchingTrades;
    public bool CanFetchTrades => !_isFetchingTrades;

    public async Task FetchTrades()
    {
        if (_isFetchingTrades) return;

        try
        {
            _isFetchingTrades = true;
            NotifyOfPropertyChange(() => CanFetchTrades);

            var list = await _connector.GetNewTradesAsync(Pair, TradesMaxCount);

            Execute.OnUIThread(() =>
            {
                FetchedTrades.Clear();
                foreach (var t in list.OrderByDescending(t => t.Time))
                    FetchedTrades.Add(t);
            });
        }
        finally
        {
            _isFetchingTrades = false;
            NotifyOfPropertyChange(() => CanFetchTrades);
        }
    }

    private void OnTrade(Trade t) => Execute.OnUIThread(() => Trades.Insert(0, t));

    #region candles
    public ObservableCollection<Candle> Candles { get; } = new();
    public ObservableCollection<Candle> FetchedCandles { get; } = new();

    private bool _candlesConnected;
    public bool CandlesConnected
    {
        get => _candlesConnected;
        set { _candlesConnected = value; NotifyOfPropertyChange(() => CandlesConnected); NotifyOfPropertyChange(() => CanConnectCandles); NotifyOfPropertyChange(() => CanDisconnectCandles); }
    }

    public bool CanConnectCandles => !CandlesConnected;
    public bool CanDisconnectCandles => CandlesConnected;

    public void ConnectCandles()
    {
        _connector.SubscribeCandles(Pair, CandleRequest.PeriodInSec, CandleRequest.From, CandleRequest.To, CandleRequest.Count);
        CandlesConnected = true;
    }

    public void DisconnectCandles()
    {
        _connector.UnsubscribeCandles(Pair);
        CandlesConnected = false;
    }

    private bool _isFetchingCandles;
    public bool CanFetchCandles => !_isFetchingCandles;

    public async Task FetchCandles()
    {
        if (_isFetchingCandles) return;

        try
        {
            _isFetchingCandles = true;
            NotifyOfPropertyChange(() => CanFetchCandles);

            var series = await _connector.GetCandleSeriesAsync(
                Pair,
                CandleRequest.PeriodInSec,
                CandleRequest.From,
                CandleRequest.To,
                CandleRequest.Count);

            Execute.OnUIThread(() =>
            {
                FetchedCandles.Clear();
                foreach (var c in series)
                    FetchedCandles.Add(c);
            });
        }
        finally
        {
            _isFetchingCandles = false;
            NotifyOfPropertyChange(() => CanFetchCandles);
        }
    }

    private void OnCandle(Candle c) => Execute.OnUIThread(() => Candles.Insert(0, c));

    public GetCandleSeriesModel CandleRequest { get; set; } = new();

    #endregion

    #region Menu commands
    public Task OpenPortfolio()
    {
        return _windowManager.ShowWindowAsync(_portfolioVm);
    }

    public async Task ExitApp() => await TryCloseAsync();
    #endregion
}