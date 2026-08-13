namespace App.Application.Prediction;

public sealed record PredictionHistoryListItem(
    long Id,
    string? ErpOrderRef,
    bool IsSimulation,
    string Status,
    string DataSufficiencyLevel,
    long? FinalWorkingLeadTimeMinutes,
    DateTimeOffset? DeliveryDate,
    DateTimeOffset CalculatedAt);

public sealed record PredictionHistoryProviderResult(
    string ProviderType,
    string ProviderStatus,
    long? WorkingLeadTimeMinutes,
    DateTimeOffset? EstimatedDeliveryDate,
    string? ModelVersion,
    string? FeatureSchemaVersion,
    string? TrainingDatasetVersion,
    string? Warnings);

public sealed record PredictionHistoryDetail(
    long Id,
    string? ErpOrderRef,
    bool IsSimulation,
    string? SimulationInputSummary,
    string Status,
    string DataSufficiencyLevel,
    long? FinalWorkingLeadTimeMinutes,
    DateTimeOffset? ProductionStart,
    DateTimeOffset? ProductionEnd,
    DateTimeOffset? ShipDate,
    DateTimeOffset? DeliveryDate,
    DateTimeOffset? RequestedDeliveryDate,
    string? CriticalPathSummary,
    DateTimeOffset CalculatedAt,
    DateTimeOffset? ActualDeliveryDate,
    long? ActualTotalWorkingLeadTimeMinutes,
    bool? DeliveredLate,
    IReadOnlyList<PredictionHistoryProviderResult> ProviderResults);
