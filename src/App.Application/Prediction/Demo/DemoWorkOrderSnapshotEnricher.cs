using App.Application.Contracts.Erp;

namespace App.Application.Prediction.Demo;

/// <summary>
/// The single source of T-388 synthetic work-order data. Enriches only snapshots that do not
/// already contain ERP work orders.
/// </summary>
public sealed class DemoWorkOrderSnapshotEnricher
{
    public const string WorkOrderReference = "DEMO-WO-001";

    public ErpBatchSnapshot Enrich(ErpBatchSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.WorkOrders.Count > 0)
        {
            return snapshot;
        }

        var orderItem = snapshot.OrderItems.FirstOrDefault();
        var productReference = orderItem?.ProductReference;
        if (productReference is null)
        {
            return snapshot;
        }

        // Defensive floor: OrderedQuantity should always be positive for a real
        // order line, but a zero/negative value must not collapse the demo
        // routing to a zero-minute (or negative) duration.
        var effectiveQuantity = Math.Max(1m, orderItem!.OrderedQuantity);
        var op10Duration = (long)Math.Round(60m * effectiveQuantity, MidpointRounding.AwayFromZero);
        var op20Duration = (long)Math.Round(45m * effectiveQuantity, MidpointRounding.AwayFromZero);

        var demoWorkOrder = new WorkOrderReadDto(
            WorkOrderReference,
            snapshot.Order.OrderReference,
            productReference,
            "Released",
            new RoutingReadDto(
                "DEMO-ROUTING-001",
                new List<OperationReadDto>
                {
                    new("DEMO-OP-10", 10, "DEMO-WC-001", op10Duration, Array.Empty<string>()),
                    new("DEMO-OP-20", 20, "DEMO-WC-001", op20Duration, new[] { "DEMO-OP-10" }),
                }));

        return snapshot with { WorkOrders = new List<WorkOrderReadDto> { demoWorkOrder } };
    }
}
