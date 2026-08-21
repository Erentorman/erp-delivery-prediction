namespace App.Domain.Prediction;

public sealed record MaterialProduct(
    string ProductReference,
    string UnitOfMeasure,
    string? PlanningClassification = null);

public sealed record MaterialBomItem(string ParentProductReference, string ComponentProductReference, decimal RequiredQuantity);

public sealed record MaterialStock(string ProductReference, decimal AvailableQuantity);

public sealed record MaterialPurchaseOrder(string PurchaseOrderReference, string ProductReference, decimal OpenQuantity, DateTimeOffset ExpectedAvailabilityDateTime);

public sealed record MaterialSnapshot(
    IReadOnlyList<MaterialProduct> Products,
    IReadOnlyList<MaterialBomItem> BomItems,
    IReadOnlyList<MaterialStock> StockLevels,
    IReadOnlyList<MaterialPurchaseOrder> OpenPurchaseOrders);
