using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Application.Contracts.Erp;
using App.Domain.Abstractions;

namespace App.Application.Erp;

public sealed class ErpBatchReader : IErpBatchReader
{
    private readonly IErpDataProvider _erpDataProvider;
    private readonly IClock _clock;

    public ErpBatchReader(IErpDataProvider erpDataProvider, IClock clock)
    {
        _erpDataProvider = erpDataProvider;
        _clock = clock;
    }

    public async Task<Result<ErpBatchSnapshot>> ReadAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        var order = await _erpDataProvider.GetOrderAsync(orderReference, cancellationToken);
        if (order is null)
        {
            return Result<ErpBatchSnapshot>.Failure(new Error("ErpBatchReader.OrderNotFound", $"Order {orderReference} could not be found.", ErrorType.NotFound));
        }

        var orderItems = await _erpDataProvider.GetOrderItemsAsync(orderReference, cancellationToken);
        var safeOrderItems = (IReadOnlyList<OrderItemReadDto>?)orderItems ?? Array.Empty<OrderItemReadDto>();

        var productReferences = safeOrderItems.Select(x => x.ProductReference).Distinct().ToList();

        var products = new List<ProductReadDto>();
        var bomItems = new List<BomItemReadDto>();

        foreach (var productRef in productReferences)
        {
            var product = await _erpDataProvider.GetProductAsync(productRef, cancellationToken);
            if (product is not null)
            {
                products.Add(product);
            }

            var productBom = await _erpDataProvider.GetProductBomAsync(productRef, cancellationToken);
            if (productBom is not null)
            {
                bomItems.AddRange(productBom);
            }
        }

        var allProductRefs = productReferences
            .Concat(bomItems.Select(b => b.ComponentProductReference))
            .Distinct()
            .ToList();

        var stockLevels = await _erpDataProvider.GetStockLevelsAsync(allProductRefs, cancellationToken);
        var safeStockLevels = (IReadOnlyList<StockLevelReadDto>?)stockLevels ?? Array.Empty<StockLevelReadDto>();

        var openPurchaseOrders = await _erpDataProvider.GetOpenPurchaseOrdersAsync(allProductRefs, cancellationToken);
        var safeOpenPurchaseOrders = (IReadOnlyList<OpenPurchaseOrderReadDto>?)openPurchaseOrders ?? Array.Empty<OpenPurchaseOrderReadDto>();

        var workOrders = await _erpDataProvider.GetWorkOrdersAsync(orderReference, allProductRefs, cancellationToken);
        var safeWorkOrders = (IReadOnlyList<WorkOrderReadDto>?)workOrders ?? Array.Empty<WorkOrderReadDto>();

        var snapshot = new ErpBatchSnapshot(
            _clock.UtcNow,
            order,
            safeOrderItems,
            products,
            bomItems,
            safeStockLevels,
            safeOpenPurchaseOrders,
            safeWorkOrders
        );

        return Result<ErpBatchSnapshot>.Success(snapshot);
    }
}
