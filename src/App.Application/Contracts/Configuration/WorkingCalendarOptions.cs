namespace App.Application.Contracts.Configuration;

public class WorkingCalendarOptions
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int BreakMinutes { get; set; }
    public int NetMinutesPerDay { get; set; }
    public List<string> WorkingDays { get; set; } = new();
    public List<string> Holidays { get; set; } = new();
    public List<string> PlannedDowntimes { get; set; } = new();
}
