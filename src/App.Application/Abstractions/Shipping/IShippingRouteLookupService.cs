using App.Application.Contracts.Shipping;

namespace App.Application.Abstractions.Shipping;

public interface IShippingRouteLookupService
{
    Task<ShippingRouteLookupResult> GetRouteAsync(
        string destination,
        string profile,
        CancellationToken cancellationToken = default);
}
