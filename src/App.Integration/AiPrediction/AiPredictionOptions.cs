namespace App.Integration.AiPrediction;

public sealed class AiPredictionOptions
{
    public const string SectionName = "AiPrediction";

    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutMs { get; set; }
}
