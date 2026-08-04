using App.Application.Contracts.Configuration;
using App.Application.Prediction.Resolvers;

namespace App.Application.Tests.Prediction.Resolvers;

public class ShippingResolverTests
{
    private readonly ShippingResolver _resolver = new();

    [Fact]
    public void ResolveShippingDuration_WithActualDuration_ReturnsActualAndNoFallback()
    {
        var options = new MvpAssumptionsOptions { Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = 1440 } };
        
        var result = _resolver.ResolveShippingDuration(120, options);

        Assert.Equal(TimeSpan.FromMinutes(120), result.Value);
        Assert.False(result.IsFallbackApplied);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void ResolveShippingDuration_WithoutActualDurationAndWithFallback_ReturnsFallback()
    {
        var options = new MvpAssumptionsOptions { Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = 1440 } };
        
        var result = _resolver.ResolveShippingDuration(null, options);

        Assert.Equal(TimeSpan.FromMinutes(1440), result.Value);
        Assert.True(result.IsFallbackApplied);
        Assert.NotNull(result.Reason);
        Assert.Contains("using fallback", result.Reason);
    }

    [Fact]
    public void ResolveShippingDuration_WithoutActualDurationAndWithoutFallback_ReturnsNullValue()
    {
        var options = new MvpAssumptionsOptions { Shipping = new ShippingAssumptionsOptions { FallbackDurationMinutes = null } };
        
        var result = _resolver.ResolveShippingDuration(null, options);

        Assert.Null(result.Value);
        Assert.True(result.IsFallbackApplied);
        Assert.NotNull(result.Reason);
        Assert.Contains("no fallback configured", result.Reason);
    }

    [Fact]
    public void ResolveShippingDuration_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _resolver.ResolveShippingDuration(null, null!));
    }
}
