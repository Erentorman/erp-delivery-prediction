namespace App.Integration.Models;

internal sealed record ShippingRouteReadModel(
    string OriginReference,
    string DestinationReference,
    string ShippingProfileReference,
    long ShippingDurationMinutes);
