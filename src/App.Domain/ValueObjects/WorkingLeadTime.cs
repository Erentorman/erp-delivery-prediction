namespace App.Domain.ValueObjects;

/// <summary>
/// Immutable, self-validating lead time expressed in working minutes (SAD §9.4, §9.14).
/// Zero is valid; negative durations are rejected. Conversion to calendar days/dates is the
/// responsibility of the WorkingCalendar service, not this value object (SAD §17.1).
/// </summary>
public sealed record WorkingLeadTime
{
    public long Minutes { get; }

    public WorkingLeadTime(long minutes)
    {
        if (minutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minutes),
                minutes,
                "Working lead time cannot be negative.");
        }

        Minutes = minutes;
    }
}
