using App.Application.Contracts.Configuration;

namespace App.Application.Prediction;

public sealed class FinalPredictionCombiner : IFinalPredictionCombiner
{
    private readonly HybridPredictionOptions _options;
    public FinalPredictionCombiner(MvpAssumptionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.HybridPrediction ?? throw new ArgumentException("Hybrid prediction configuration is required.", nameof(options));
    }

    public FinalPredictionResult Combine(PredictionProviderResult ruleBased, PredictionProviderResult ai)
    {
        var rbValid = ruleBased.Status == AiProviderStatus.Success && ruleBased.WorkingLeadTimeMinutes is >= 0;
        var aiValid = ai.Status == AiProviderStatus.Success && ai.WorkingLeadTimeMinutes is > 0;

        if (!rbValid)
        {
            var reason = MapRuleFailure(ruleBased.FailureCode);
            return new FinalPredictionResult(
                aiValid ? FinalPredictionStatus.AiOnlyCandidate : FinalPredictionStatus.InsufficientData,
                reason, null, null, null, null, null, null);
        }

        var rb = ruleBased.WorkingLeadTimeMinutes!.Value;
        if (!aiValid)
            return Fallback(rb, MapAiFailure(ai));

        var aiMinutes = ai.WorkingLeadTimeMinutes!.Value;
        if (_options.AiTechnicalUpperBoundWorkingMinutes is long upper && aiMinutes > upper)
            return Fallback(rb, PredictionFallbackReason.InvalidAiValue);

        var absoluteDecimal = Math.Abs(aiMinutes - rb);
        var absolute = checked((long)Math.Round(absoluteDecimal, MidpointRounding.AwayFromZero));
        decimal? relative = rb == 0 ? null : absoluteDecimal / rb * 100m;
        if (relative > _options.AiVarianceThresholdPercent && absoluteDecimal > _options.AiVarianceThresholdWorkingMinutes)
            return Fallback(rb, PredictionFallbackReason.AiPredictionOutsideTolerance, absolute, relative);

        var hybrid = rb * _options.RuleBasedWeight + aiMinutes * _options.AiWeight;
        var rounded = checked((long)Math.Round(hybrid, MidpointRounding.AwayFromZero));
        return new FinalPredictionResult(FinalPredictionStatus.HybridCalculated,
            PredictionFallbackReason.None, rounded, "WeightedAverage", _options.RuleBasedWeight,
            _options.AiWeight, absolute, relative);
    }

    private static FinalPredictionResult Fallback(decimal rb, PredictionFallbackReason reason, long? absolute = null, decimal? relative = null) =>
        new(FinalPredictionStatus.RuleBasedFallback, reason,
            checked((long)Math.Round(rb, MidpointRounding.AwayFromZero)), "RuleBasedOnly", 1m, 0m, absolute, relative);

    private static PredictionFallbackReason MapAiFailure(PredictionProviderResult ai) => ai.Status switch
    {
        AiProviderStatus.Timeout => PredictionFallbackReason.AiPredictionTimeout,
        AiProviderStatus.ServiceUnavailable => PredictionFallbackReason.AiServiceUnavailable,
        AiProviderStatus.InvalidResponse => PredictionFallbackReason.InvalidAiResponse,
        AiProviderStatus.InsufficientFeatures => PredictionFallbackReason.InsufficientAiFeatures,
        AiProviderStatus.ModelUnavailable => PredictionFallbackReason.AiModelUnavailable,
        // T-908 intentionally exposes no reliable model/schema/dataset mismatch subtype.
        AiProviderStatus.VersionMismatch => PredictionFallbackReason.InvalidAiResponse,
        _ => PredictionFallbackReason.InvalidAiValue
    };

    private static PredictionFallbackReason MapRuleFailure(string? code) => code switch
    {
        "InvalidErpData" => PredictionFallbackReason.InvalidErpData,
        "OperationGraphCycleDetected" => PredictionFallbackReason.OperationGraphCycleDetected,
        _ => PredictionFallbackReason.RuleBasedEngineError
    };
}
