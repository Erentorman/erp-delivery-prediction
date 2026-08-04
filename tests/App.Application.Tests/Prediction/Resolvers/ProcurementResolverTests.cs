using App.Application.Contracts.Configuration;
using App.Application.Prediction.Resolvers;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction.Resolvers;

public class ProcurementResolverTests
{
    private readonly ProcurementResolver _resolver = new();
    private readonly MvpAssumptionsOptions _options = new()
    {
        Procurement = new ProcurementAssumptionsOptions { FallbackDurationMinutes = 960 }
    };

    [Fact]
    public void ResolveAvailabilityDate_WithOpenPo_ReturnsPoDateAndNoFallback()
    {
        var expectedDate = DateTimeOffset.UtcNow.AddDays(2);
        var po = new MaterialPurchaseOrder("PO-1", "PROD-1", 100, expectedDate);
        var currentTime = DateTimeOffset.UtcNow;

        var result = _resolver.ResolveAvailabilityDate(po, currentTime, _options);

        Assert.Equal(expectedDate, result.Value);
        Assert.False(result.IsFallbackApplied);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void ResolveAvailabilityDate_WithoutOpenPo_ReturnsFallbackDate()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var expectedFallbackDate = currentTime.AddMinutes(960);

        var result = _resolver.ResolveAvailabilityDate(null, currentTime, _options);

        Assert.Equal(expectedFallbackDate, result.Value);
        Assert.True(result.IsFallbackApplied);
        Assert.NotNull(result.Reason);
        Assert.Contains("fallback lead time", result.Reason);
    }

    [Fact]
    public void ResolveAvailabilityDate_WithNullOptions_ThrowsArgumentNullException()
    {
        var currentTime = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentNullException>(() => _resolver.ResolveAvailabilityDate(null, currentTime, null!));
    }
}
