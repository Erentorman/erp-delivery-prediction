namespace App.Application.Contracts.Configuration;

public sealed class HybridPredictionOptions
{
    public decimal RuleBasedWeight { get; set; } = 0.60m;
    public decimal AiWeight { get; set; } = 0.40m;
    public decimal AiVarianceThresholdPercent { get; set; } = 50m;
    public long AiVarianceThresholdWorkingMinutes { get; set; } = 960;
    public long? AiTechnicalUpperBoundWorkingMinutes { get; set; }
}
