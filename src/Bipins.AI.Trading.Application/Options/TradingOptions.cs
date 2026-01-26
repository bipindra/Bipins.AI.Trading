namespace Bipins.AI.Trading.Application.Options;

public class TradingOptions
{
    public const string SectionName = "Trading";
    
    public bool Enabled { get; set; } = false;
    public TradingMode Mode { get; set; } = TradingMode.Ask;
    public List<string> Symbols { get; set; } = new();
    public string Timeframe { get; set; } = "5m";
    public int MaxOrdersPerCandle { get; set; } = 1;
    public int CooldownSeconds { get; set; } = 60;
}

public enum TradingMode
{
    Ask,
    Auto
}
