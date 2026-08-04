namespace App.Application.Abstractions.Shipping;

public interface IShippingRouteLookupService
{
    Task<ShippingRouteLookupResult> GetRouteAsync(
        string originReference,
        string destinationReference,
        string shippingProfileReference,
        CancellationToken cancellationToken = default);
}
