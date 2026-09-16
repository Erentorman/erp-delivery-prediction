using App.Application.Contracts.Configuration;
using App.Application.Prediction;
using App.Application.Prediction.Resolvers;
using App.Domain.Abstractions;
using App.Domain.Prediction;
using Moq;

namespace App.Application.Tests.Prediction;

public sealed class RuleBasedPredictionProviderTests
{
    [Fact]
    public async Task PredictAsync_AdaptsExistingEngineCpmAndMapper()
    {
        var now = new DateTimeOffset(2026, 1, 5, 8, 0, 0, TimeSpan.Zero);
        var clock = Mock.Of<IClock>(x => x.UtcNow == now);
        var options = new MvpAssumptionsOptions
        {
            WorkingCalendar = new()
            {
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0),
                BreakStartTime = new TimeOnly(12, 0),
                BreakEndTime = new TimeOnly(13, 0),
                BreakMinutes = 60,
                NetMinutesPerDay = 480,
                WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]
            },
            Procurement = new() { FallbackDurationMinutes = 960 },
            Shipping = new() { FallbackDurationMinutes = 60 }
        };
        var provider = new RuleBasedPredictionProvider(
            new RuleBasedPredictionEngine(new ProcurementResolver(), new CapacityResolver(), clock, options),
            new CriticalPathCalculator(), new PredictionResultMapper(clock, options, new ShippingResolver()));

        var result = await provider.PredictAsync(Context());

        Assert.Equal(AiProviderStatus.Success, result.Status);
        Assert.Equal(60m, result.WorkingLeadTimeMinutes);
        Assert.Equal(now.AddMinutes(60), result.RuleBasedPrediction!.EstimatedEnd);
        Assert.Null(result.FeaturePayload);
    }

    [Fact]
    public async Task PredictAsync_PropagatesPreCancelledTokenBeforeEngine()
    {
        using var source = new CancellationTokenSource(); source.Cancel();
        var clock = Mock.Of<IClock>(x => x.UtcNow == DateTimeOffset.UtcNow);
        var options = new MvpAssumptionsOptions
        {
            WorkingCalendar = new()
            {
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0),
                BreakStartTime = new TimeOnly(12, 0),
                BreakEndTime = new TimeOnly(13, 0),
                BreakMinutes = 60,
                NetMinutesPerDay = 480,
                WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]
            },
            Procurement = new() { FallbackDurationMinutes = 960 }
        };
        var provider = new RuleBasedPredictionProvider(
            new RuleBasedPredictionEngine(new ProcurementResolver(), new CapacityResolver(), clock, options),
            new CriticalPathCalculator(), new PredictionResultMapper(clock, options, new ShippingResolver()));
        await Assert.ThrowsAsync<OperationCanceledException>(() => provider.PredictAsync(Context(), source.Token));
    }

    private static PredictionContext Context() => new(
        new("O", "P", 1, DateTimeOffset.UtcNow),
        new([new("P", "EA")], [], [], []),
        new([new("OP", 1, "WC", 60, [])]), new(), new(), new());
}
