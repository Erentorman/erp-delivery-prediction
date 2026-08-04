using App.Application.Abstractions.Shipping;
using App.Application.Abstractions.Erp;

namespace App.Infrastructure.Shipping;

public sealed class ShippingRouteLookupService(IErpDataProvider erpDataProvider)
    : IShippingRouteLookupService
{
    public async Task<ShippingRouteLookupResult> GetRouteAsync(
        string originReference,
        string destinationReference,
        string shippingProfileReference,
        CancellationToken cancellationToken = default)
    {
        var route = await erpDataProvider.GetShippingDurationAsync(
            originReference,
            destinationReference,
            shippingProfileReference,
            cancellationToken);

        return route is null
            ? new ShippingRouteLookupResult.NotFound()
            : new ShippingRouteLookupResult.Found(route.ShippingDurationMinutes);
    }
}
