namespace App.Domain.Prediction;

public sealed class WorkingCalendar
{
    private readonly TimeOnly _startTime;
    private readonly TimeOnly _endTime;
    private readonly TimeOnly _breakStartTime;
    private readonly TimeOnly _breakEndTime;
    private readonly long _netMinutesPerDay;
    private readonly HashSet<DayOfWeek> _workingDays;

    public WorkingCalendar(
        TimeOnly startTime,
        TimeOnly endTime,
        TimeOnly breakStartTime,
        TimeOnly breakEndTime,
        long netMinutesPerDay,
        IReadOnlyCollection<DayOfWeek> workingDays)
    {
        if (startTime >= endTime)
            throw new ArgumentOutOfRangeException(nameof(startTime), "Shift start time must be before shift end time.");
        if (breakStartTime > breakEndTime)
            throw new ArgumentOutOfRangeException(nameof(breakStartTime), "Break start time must not be after break end time.");
        if (breakStartTime < startTime || breakEndTime > endTime)
            throw new ArgumentOutOfRangeException(nameof(breakStartTime), "Break window must fall within the shift window.");
        if (netMinutesPerDay <= 0)
            throw new ArgumentOutOfRangeException(nameof(netMinutesPerDay));
        ArgumentNullException.ThrowIfNull(workingDays);
        if (workingDays.Count == 0)
            throw new ArgumentException("At least one working day must be configured.", nameof(workingDays));

        _startTime = startTime;
        _endTime = endTime;
        _breakStartTime = breakStartTime;
        _breakEndTime = breakEndTime;
        _netMinutesPerDay = netMinutesPerDay;
        _workingDays = new HashSet<DayOfWeek>(workingDays);
    }

    public bool IsWorkingDay(DateOnly date) => _workingDays.Contains(date.DayOfWeek);

    public DateTimeOffset AddWorkingMinutes(DateTimeOffset start, long minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));

        long remaining = minutes;
        DateTimeOffset current = SnapForwardToWorkingInstant(start);
        if (remaining == 0) return current;

        while (remaining > 0)
        {
            var currentTime = TimeOnly.FromDateTime(current.DateTime);
            // Between StartTime and BreakStartTime the next boundary is the break;
            // between BreakEndTime and EndTime (break already passed) it's the shift end.
            var boundary = currentTime < _breakStartTime ? _breakStartTime : _endTime;
            var availableToday = (long)(boundary.ToTimeSpan() - currentTime.ToTimeSpan()).TotalMinutes;

            if (remaining <= availableToday)
            {
                current = current.AddMinutes(remaining);
                remaining = 0;
            }
            else
            {
                remaining -= availableToday;
                current = SnapForwardToWorkingInstant(current.AddMinutes(availableToday));
            }
        }

        return current;
    }

    public decimal ToDisplayWorkingDays(long minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        return (decimal)minutes / _netMinutesPerDay;
    }

    // Moves `instant` forward (never backward) to the nearest point in time that
    // is inside the configured shift window, on a configured working day, and
    // outside the break — skipping non-working days, pre-shift hours, the break
    // window, and post-shift hours as needed.
    private DateTimeOffset SnapForwardToWorkingInstant(DateTimeOffset instant)
    {
        while (true)
        {
            var date = DateOnly.FromDateTime(instant.DateTime);
            if (!IsWorkingDay(date))
            {
                instant = AtTime(instant.AddDays(1), _startTime);
                continue;
            }

            var time = TimeOnly.FromDateTime(instant.DateTime);
            if (time < _startTime)
            {
                instant = AtTime(instant, _startTime);
                continue;
            }

            if (time >= _endTime)
            {
                instant = AtTime(instant.AddDays(1), _startTime);
                continue;
            }

            if (time >= _breakStartTime && time < _breakEndTime)
            {
                instant = AtTime(instant, _breakEndTime);
                continue;
            }

            return instant;
        }
    }

    private static DateTimeOffset AtTime(DateTimeOffset instant, TimeOnly time) =>
        new(instant.Year, instant.Month, instant.Day, time.Hour, time.Minute, time.Second, instant.Offset);
}
