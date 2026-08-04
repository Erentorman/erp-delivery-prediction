namespace App.Application.Contracts.Configuration;

public class MvpAssumptionsOptions
{
    public const string SectionName = "mvpAssumptions";

    public WorkingCalendarAssumptionsOptions WorkingCalendar { get; set; } = new();
    public ProcurementAssumptionsOptions Procurement { get; set; } = new();
    public ShippingAssumptionsOptions Shipping { get; set; } = new();
}
