using App.Application.Contracts.Shipping;

namespace App.Application.Abstractions.Shipping;

public abstract record ShippingRouteLookupResult
{
    private ShippingRouteLookupResult() { }

    public sealed record RouteFound(ShippingRouteReadModel Route) : ShippingRouteLookupResult;
    public sealed record UnknownDestination(string Destination) : ShippingRouteLookupResult;
    public sealed record UnknownRoute(string Origin, string Destination, string Profile) : ShippingRouteLookupResult;
    public sealed record InvalidRouteData(string Message) : ShippingRouteLookupResult;
}
