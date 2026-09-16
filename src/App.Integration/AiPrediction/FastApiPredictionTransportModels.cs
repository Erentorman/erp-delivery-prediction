using System.Text.Json.Serialization;

namespace App.Integration.AiPrediction;

internal sealed record FastApiPredictionRequest(
    string FeatureSchemaVersion,
    FastApiPredictionFeatures Features);

internal sealed record FastApiPredictionFeatures(
    [property: JsonPropertyName("product_ref")] string ProductRef,
    [property: JsonPropertyName("product_category")] string? ProductCategory,
    [property: JsonPropertyName("quantity")] decimal Quantity,
    [property: JsonPropertyName("bom_item_count")] int BomItemCount,
    [property: JsonPropertyName("missing_material_count")] int MissingMaterialCount,
    [property: JsonPropertyName("total_missing_quantity")] decimal TotalMissingQuantity,
    [property: JsonPropertyName("maximum_supplier_lead_time_days")] decimal? MaximumSupplierLeadTimeDays,
    [property: JsonPropertyName("operation_count")] int OperationCount,
    [property: JsonPropertyName("total_standard_operation_minutes")] long TotalStandardOperationMinutes,
    [property: JsonPropertyName("work_center_load_ratio")] decimal? WorkCenterLoadRatio,
    [property: JsonPropertyName("active_work_order_count")] int? ActiveWorkOrderCount,
    [property: JsonPropertyName("shift_capacity_minutes")] long? ShiftCapacityMinutes,
    [property: JsonPropertyName("holiday_count")] int? HolidayCount,
    [property: JsonPropertyName("planned_downtime_minutes")] long? PlannedDowntimeMinutes,
    [property: JsonPropertyName("shipping_duration_minutes")] long? ShippingDurationMinutes,
    [property: JsonPropertyName("requested_delivery_lead_minutes")] long? RequestedDeliveryLeadMinutes);

internal sealed record FastApiPredictionResponse(
    double? WorkingLeadTimeMinutes,
    string? ModelVersion,
    string? FeatureSchemaVersion,
    string? TrainingDatasetVersion);
