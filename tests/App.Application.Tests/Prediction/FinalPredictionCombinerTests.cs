using App.Application.Contracts.Configuration;
using App.Application.Prediction;

namespace App.Application.Tests.Prediction;

public sealed class FinalPredictionCombinerTests
{
    [Fact]
    public void Defaults_AreAuthoritativeSprintOneValues()
    {
        var options = new HybridPredictionOptions();
        Assert.Equal(.60m, options.RuleBasedWeight); Assert.Equal(.40m, options.AiWeight);
        Assert.Equal(50m, options.AiVarianceThresholdPercent);
        Assert.Equal(960, options.AiVarianceThresholdWorkingMinutes);
        Assert.Null(options.AiTechnicalUpperBoundWorkingMinutes);
    }

    [Fact]
    public void BothSuccess_InTolerance_UsesConfigurableWeights()
    {
        var result = Combine(1000, 1100, o => { o.RuleBasedWeight = .7m; o.AiWeight = .3m; });
        Assert.Equal(FinalPredictionStatus.HybridCalculated, result.Status);
        Assert.Equal(1030, result.WorkingLeadTimeMinutes);
    }

    [Fact]
    public void BothSuccess_DefaultWeightsProduceSixtyFortyResult() =>
        Assert.Equal(104, Combine(100, 110).WorkingLeadTimeMinutes);

    [Fact]
    public void CustomPercentThresholdIsApplied()
    {
        var accepted = Combine(1000, 1600, o => { o.AiVarianceThresholdPercent = 60; o.AiVarianceThresholdWorkingMinutes = 1; });
        var rejected = Combine(1000, 1600, o => { o.AiVarianceThresholdPercent = 59; o.AiVarianceThresholdWorkingMinutes = 1; });
        Assert.Equal(FinalPredictionStatus.HybridCalculated, accepted.Status);
        Assert.Equal(FinalPredictionStatus.RuleBasedFallback, rejected.Status);
    }

    [Fact]
    public void CustomAbsoluteThresholdIsApplied()
    {
        var accepted = Combine(1000, 1600, o => o.AiVarianceThresholdWorkingMinutes = 600);
        var rejected = Combine(1000, 1600, o => o.AiVarianceThresholdWorkingMinutes = 599);
        Assert.Equal(FinalPredictionStatus.HybridCalculated, accepted.Status);
        Assert.Equal(FinalPredictionStatus.RuleBasedFallback, rejected.Status);
    }

    [Theory]
    [InlineData(AiProviderStatus.Timeout, PredictionFallbackReason.AiPredictionTimeout)]
    [InlineData(AiProviderStatus.ServiceUnavailable, PredictionFallbackReason.AiServiceUnavailable)]
    [InlineData(AiProviderStatus.InvalidResponse, PredictionFallbackReason.InvalidAiResponse)]
    [InlineData(AiProviderStatus.VersionMismatch, PredictionFallbackReason.InvalidAiResponse)]
    [InlineData(AiProviderStatus.Rejected, PredictionFallbackReason.InvalidAiValue)]
    public void AiFailure_FallsBack(AiProviderStatus status, PredictionFallbackReason reason)
    {
        var result = Combiner().Combine(Rule(1000), Ai(null, status));
        Assert.Equal(FinalPredictionStatus.RuleBasedFallback, result.Status);
        Assert.Equal(reason, result.FallbackReason);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void InvalidAiNumeric_FallsBack(decimal value)
    {
        var result = Combiner().Combine(Rule(1000), Ai(value));
        Assert.Equal(PredictionFallbackReason.InvalidAiValue, result.FallbackReason);
    }

    [Fact]
    public void MissingNumeric_DefensivelyRejectsNaNOrInfinityAdaptation()
    {
        var result = Combiner().Combine(Rule(1000), Ai(null));
        Assert.Equal(PredictionFallbackReason.InvalidAiValue, result.FallbackReason);
    }

    [Fact]
    public void ExactlyFiftyPercent_IsAccepted() =>
        Assert.Equal(FinalPredictionStatus.HybridCalculated,
            Combine(2000, 3000, o => o.AiVarianceThresholdWorkingMinutes = 999).Status);

    [Fact]
    public void JustAboveFiftyAndAbsoluteAbove_IsRejected() =>
        Assert.Equal(PredictionFallbackReason.AiPredictionOutsideTolerance,
            Combine(2000, 3001, o => o.AiVarianceThresholdWorkingMinutes = 999).FallbackReason);

    [Fact]
    public void ExactlyNineHundredSixty_IsAccepted() =>
        Assert.Equal(FinalPredictionStatus.HybridCalculated, Combine(1000, 1960).Status);

    [Fact]
    public void JustAboveNineHundredSixtyAndPercentAbove_IsRejected() =>
        Assert.Equal(PredictionFallbackReason.AiPredictionOutsideTolerance, Combine(1000, 1961).FallbackReason);

    [Fact]
    public void OnlyPercentExceeded_RemainsEligible() =>
        Assert.Equal(FinalPredictionStatus.HybridCalculated, Combine(100, 151).Status);

    [Fact]
    public void OnlyAbsoluteExceeded_RemainsEligible() =>
        Assert.Equal(FinalPredictionStatus.HybridCalculated, Combine(10000, 10961).Status);

    [Fact]
    public void RuleBasedZero_SkipsPercentageWithoutDivisionByZero()
    {
        var result = Combine(0, 960);
        Assert.Equal(FinalPredictionStatus.HybridCalculated, result.Status); Assert.Null(result.RelativeDifferencePercent);
    }

    [Theory]
    [InlineData("1", "2", 1)]
    [InlineData("0", "1.25", 1)]
    [InlineData("1", "2.5", 2)]
    public void FinalBoundary_RoundsAwayFromZero(string rbText, string aiText, long expected)
    {
        var result = Combine(decimal.Parse(rbText, System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(aiText, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(expected, result.WorkingLeadTimeMinutes);
    }

    [Fact]
    public void TechnicalUpperBound_NullDisablesCheck() =>
        Assert.Equal(FinalPredictionStatus.HybridCalculated,
            Combine(1000, 5000, o => { o.AiVarianceThresholdPercent = 1000; o.AiVarianceThresholdWorkingMinutes = 10000; }).Status);

    [Theory]
    [InlineData(4999, FinalPredictionStatus.HybridCalculated)]
    [InlineData(5000, FinalPredictionStatus.HybridCalculated)]
    [InlineData(5001, FinalPredictionStatus.RuleBasedFallback)]
    public void TechnicalUpperBound_UsesStrictGreaterThan(long ai, FinalPredictionStatus expected) =>
        Assert.Equal(expected, Combine(1000, ai, o => { o.AiTechnicalUpperBoundWorkingMinutes = 5000; o.AiVarianceThresholdPercent = 1000; }).Status);

    [Fact]
    public void RuleFailureWithAiSuccess_IsCandidateWithoutFinalMinutes()
    {
        var result = Combiner().Combine(new(PredictionProviderType.RuleBased, AiProviderStatus.Rejected, FailureCode: "InvalidErpData"), Ai(1000));
        Assert.Equal(FinalPredictionStatus.AiOnlyCandidate, result.Status); Assert.Null(result.WorkingLeadTimeMinutes);
    }

    [Fact]
    public void BothFail_IsInsufficientData()
    {
        var result = Combiner().Combine(new(PredictionProviderType.RuleBased, AiProviderStatus.Rejected), Ai(null, AiProviderStatus.Timeout));
        Assert.Equal(FinalPredictionStatus.InsufficientData, result.Status); Assert.Null(result.WorkingLeadTimeMinutes);
    }

    private static FinalPredictionResult Combine(decimal rb, decimal ai, Action<HybridPredictionOptions>? configure = null)
    { var options = Options(); configure?.Invoke(options.HybridPrediction); return new FinalPredictionCombiner(options).Combine(Rule(rb), Ai(ai)); }
    private static FinalPredictionCombiner Combiner() => new(Options());
    private static MvpAssumptionsOptions Options() => new() { HybridPrediction = new() };
    private static PredictionProviderResult Rule(decimal? value) => new(PredictionProviderType.RuleBased, AiProviderStatus.Success, value);
    private static PredictionProviderResult Ai(decimal? value, AiProviderStatus status = AiProviderStatus.Success) => new(PredictionProviderType.Ai, status, value);
}
