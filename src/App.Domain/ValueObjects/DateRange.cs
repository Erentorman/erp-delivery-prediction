namespace App.Domain.ValueObjects;

/// <summary>
/// Immutable, self-validating date/time range (SAD §9.4).
/// </summary>
public sealed record DateRange
{
    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public DateRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
        {
            throw new ArgumentException(
                $"End ({end:O}) cannot be earlier than Start ({start:O}).",
                nameof(end));
        }

        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;
}
