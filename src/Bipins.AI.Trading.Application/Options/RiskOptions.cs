namespace Bipins.AI.Trading.Application.Options;

public class RiskOptions
{
    public const string SectionName = "Risk";
    
    public double MaxPositionPercent { get; set; } = 10.0;
    public int MaxOpenPositions { get; set; } = 5;
    public double MaxDailyLossPercent { get; set; } = 5.0;
    public double AtrLimit { get; set; } = 2.0;
    public double SpreadLimit { get; set; } = 0.01;
}
