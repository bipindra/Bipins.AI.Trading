namespace Bipins.AI.Trading.Application.Options;

public class BrokerOptions
{
    public const string SectionName = "Broker";
    
    public string Provider { get; set; } = "Alpaca";
    
    public AlpacaOptions Alpaca { get; set; } = new();
}

public class AlpacaOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://paper-api.alpaca.markets";
}
