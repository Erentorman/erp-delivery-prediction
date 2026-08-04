namespace App.Application.Prediction.Resolvers;

public sealed record FallbackResult<T>(T Value, bool IsFallbackApplied, string? Reason);
