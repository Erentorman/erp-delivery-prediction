namespace App.Application.Contracts.Configuration;

public class MvpAssumptionsOptions
{
    public string ConfigVersion { get; set; } = string.Empty;
    public WorkingCalendarOptions WorkingCalendar { get; set; } = new();
    public ProcurementOptions Procurement { get; set; } = new();
    public ShippingOptions Shipping { get; set; } = new();
    public List<string> HolidaysPendingApproval { get; set; } = new();
}
