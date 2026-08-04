using App.Domain.Abstractions;

namespace App.Domain.Tests.Abstractions;

/// <summary>
/// Test-only IClock implementation that always returns a fixed, caller-supplied instant.
/// Enforces UTC (Offset == TimeSpan.Zero) to match the IClock contract.
/// </summary>
internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"FixedClock requires a UTC DateTimeOffset (Offset == TimeSpan.Zero); got offset {utcNow.Offset}.",
                nameof(utcNow));
        }

        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan amount)
    {
        UtcNow = UtcNow.Add(amount);
    }
}
