namespace App.Integration.AiPrediction;

internal sealed record FastApiPredictionRequest(
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

internal sealed record FastApiPredictionResponse(
    double? WorkingLeadTimeMinutes,
    string? ModelVersion,
    string? FeatureSchemaVersion,
    string? TrainingDatasetVersion);
