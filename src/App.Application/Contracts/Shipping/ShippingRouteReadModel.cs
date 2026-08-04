namespace App.Application.Contracts.Shipping;

public sealed record ShippingRouteReadModel(
    string Origin,
    string Destination,
    string ShippingProfile,
    int DurationMinutes);
