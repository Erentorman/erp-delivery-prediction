namespace App.Application.Contracts.Prediction;

/// <summary>
/// Versioned, data-minimized runtime AI feature contract from SAD section 9.9.
/// Nullable numeric values mean that the raw snapshot or verified transformation
/// semantics are unavailable; zero remains a real measured value.
/// </summary>
public sealed record AiFeaturePayload(
    int FeatureSchemaVersion,
    string ProductRef,
    string? ProductCategory,
    decimal Quantity,
    int BomItemCount,
    int MissingMaterialCount,
    decimal TotalMissingQuantity,
    decimal? MaximumSupplierLeadTimeDays,
    int OperationCount,
    long TotalStandardOperationMinutes,
    decimal? WorkCenterLoadRatio,
    int? ActiveWorkOrderCount,
    long? ShiftCapacityMinutes,
    int? HolidayCount,
    long? PlannedDowntimeMinutes,
    long? ShippingDurationMinutes,
    long? RequestedDeliveryLeadMinutes);
