using App.Application.Prediction.Resolvers;

namespace App.Application.Tests.Prediction.Resolvers;

public class CapacityResolverTests
{
    private readonly CapacityResolver _resolver = new();

    [Fact]
    public void ResolveCapacityConstraint_ReturnsUnlimitedWithReason()
    {
        var result = _resolver.ResolveCapacityConstraint();

        Assert.True(result.Value);
        Assert.True(result.IsFallbackApplied);
        Assert.NotNull(result.Reason);
        Assert.Contains("unlimited", result.Reason);
    }
}
