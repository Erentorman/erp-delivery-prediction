namespace App.Application.Contracts.Configuration;

public class WorkingCalendarOptions
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public int NetMinutesPerDay { get; set; }
    public List<string> WorkingDays { get; set; } = new();
    public List<string> Holidays { get; set; } = new();
    public List<string> PlannedDowntimes { get; set; } = new();
}
