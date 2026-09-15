using App.Application.Contracts.Prediction;
using App.Domain.Prediction;

namespace App.Application.Prediction;

/// <summary>
/// Deterministically maps raw PredictionContext snapshots to the runtime AI
/// feature contract. It deliberately does not consume pipeline or CPM output.
/// </summary>
public sealed class AiFeatureBuilder : IAiFeatureBuilder
{
    public const int CurrentFeatureSchemaVersion = 1;

    public AiFeaturePayload Build(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var productReference = context.OrderInput.ProductReference;
        var bomItems = context.MaterialSnapshot.BomItems
            .Where(item => item.ParentProductReference == productReference)
            .ToList();

        var missingMaterialCount = 0;
        var totalMissingQuantity = 0m;

        foreach (var bomItem in bomItems)
        {
            var requiredQuantity = bomItem.RequiredQuantity * context.OrderInput.Quantity;
            var stock = context.MaterialSnapshot.StockLevels
                .FirstOrDefault(item => item.ProductReference == bomItem.ComponentProductReference);
            var availableQuantity = stock?.AvailableQuantity ?? 0m;
            var shortage = requiredQuantity - availableQuantity;

            if (shortage > 0m)
            {
                missingMaterialCount++;
                totalMissingQuantity += shortage;
            }
        }

        var productCategory = context.MaterialSnapshot.Products
            .FirstOrDefault(product => product.ProductReference == productReference)
            ?.PlanningClassification;

        return new AiFeaturePayload(
            CurrentFeatureSchemaVersion,
            productReference,
            productCategory,
            context.OrderInput.Quantity,
            bomItems.Count,
            missingMaterialCount,
            totalMissingQuantity,
            MaximumSupplierLeadTimeDays: null,
            context.RoutingSnapshot.Operations.Count,
            context.RoutingSnapshot.Operations.Sum(operation => operation.StandardDurationMinutes),
            WorkCenterLoadRatio: null,
            ActiveWorkOrderCount: null,
            ShiftCapacityMinutes: null,
            HolidayCount: null,
            PlannedDowntimeMinutes: null,
            ShippingDurationMinutes: null,
            RequestedDeliveryLeadMinutes: null);
    }
}
