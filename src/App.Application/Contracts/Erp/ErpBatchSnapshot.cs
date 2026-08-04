using App.Application.Contracts.Erp;

namespace App.Application.Contracts.Erp;

public sealed record ErpBatchSnapshot(
    DateTimeOffset ReadAtUtc,
    OrderReadDto Order,
    IReadOnlyList<OrderItemReadDto> OrderItems,
    IReadOnlyList<ProductReadDto> Products,
    IReadOnlyList<BomItemReadDto> BomItems,
    IReadOnlyList<StockLevelReadDto> StockLevels,
    IReadOnlyList<OpenPurchaseOrderReadDto> OpenPurchaseOrders,
    IReadOnlyList<WorkOrderReadDto> WorkOrders);
