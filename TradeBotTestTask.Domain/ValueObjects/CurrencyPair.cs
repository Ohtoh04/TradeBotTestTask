namespace TradeBotTestTask.Domain.ValueObjects;

// I would probably forget to use it in future, but it is a good thing to have value objects separately, rather than just making pair string property
public class CurrencyPair
{
    public required string BaseCurrency { get; set; }
    public required string QuoteCurrency { get; set; }

    public override string ToString() => $"t{BaseCurrency}{QuoteCurrency}";
}
