using System.Text.Json;
using App.Application.Contracts.Prediction;
using App.Application.Prediction;

namespace App.Application.Tests.Contracts.Prediction;

public sealed class PredictionResponseTests
{
    [Theory]
    [InlineData(FinalPredictionStatus.HybridCalculated)]
    [InlineData(FinalPredictionStatus.RuleBasedFallback)]
    [InlineData(FinalPredictionStatus.AiOnlyCandidate)]
    public void From_ProducesThreeSafeResultSections(FinalPredictionStatus status)
    {
        var payload = new AiFeaturePayload(1, "SECRET-FEATURE", null, 1, 1, 0, 0,
            null, 1, 1, null, null, null, null, null, null, null);
        var aggregate = new PredictionAggregateResult("O",
            new(PredictionProviderType.RuleBased, AiProviderStatus.Success, 100),
            new(PredictionProviderType.Ai, AiProviderStatus.Success, 110, FeaturePayload: payload),
            new(status, PredictionFallbackReason.None,
                status == FinalPredictionStatus.AiOnlyCandidate ? null : 104,
                status == FinalPredictionStatus.HybridCalculated ? "WeightedAverage" : null,
                .6m, .4m, 10, 10));

        var response = PredictionResponse.From(aggregate);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(response.RuleBasedPrediction); Assert.NotNull(response.AiPrediction); Assert.NotNull(response.FinalPrediction);
        Assert.Equal(status.ToString(), response.FinalPrediction.Status);
        Assert.DoesNotContain("featurePayload", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRET-FEATURE", json);
    }
}
