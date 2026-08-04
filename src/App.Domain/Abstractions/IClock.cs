namespace App.Domain.Abstractions;

/// <summary>
/// Domain-owned clock port (SAD §8.4/§9.4). Implemented outside Domain (Infrastructure)
/// so time-dependent domain logic stays deterministic and testable.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
