namespace App.Application.Prediction.Resolvers;

public sealed class CapacityResolver
{
    public FallbackResult<bool> ResolveCapacityConstraint()
    {
        return new FallbackResult<bool>(true, true, "Capacity is assumed unlimited for MVP");
    }
}
