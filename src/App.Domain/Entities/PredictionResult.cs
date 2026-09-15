namespace App.Domain.Entities;

public class PredictionResult
{
    public long Id { get; set; }
    public string? ErpOrderRef { get; set; }
    public bool IsSimulation { get; set; }
    public string? SimulationInputSummary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DataSufficiencyLevel { get; set; } = string.Empty;
    public long? FinalWorkingLeadTimeMinutes { get; set; }
    public DateTime? ProductionStart { get; set; }
    public DateTime? ProductionEnd { get; set; }
    public DateTime? ShipDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? CriticalPathSummary { get; set; }
    public DateTime CalculatedAt { get; set; }

    // Future actual/training fields (SAD §18.4) — nullable, not populated by this task.
    public DateTime? ActualProductionStart { get; set; }
    public DateTime? ActualProductionEnd { get; set; }
    public DateTime? ActualShippingDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public long? ActualTotalWorkingLeadTimeMinutes { get; set; }
    public bool? DeliveredLate { get; set; }

    public long? CreatedBy { get; set; }

    public ICollection<PredictionProviderResult> ProviderResults { get; set; } = new List<PredictionProviderResult>();
}
