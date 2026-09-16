namespace App.Application.Contracts.Configuration;

public class WorkingCalendarAssumptionsOptions
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // MVP assumption added under T-912 — NOT present in
    // tools/erp-seed-converter/config/mvp-assumptions.v2.json (which only
    // carries a flat breakMinutes duration, no clock placement). Real break
    // scheduling data is not available from ERP; 12:00-13:00 is a team
    // decision, not a sourced value.
    public TimeOnly BreakStartTime { get; set; }
    public TimeOnly BreakEndTime { get; set; }
    public int BreakMinutes { get; set; }

    public long NetMinutesPerDay { get; set; }
    public IReadOnlyList<DayOfWeek> WorkingDays { get; set; } = Array.Empty<DayOfWeek>();
}
