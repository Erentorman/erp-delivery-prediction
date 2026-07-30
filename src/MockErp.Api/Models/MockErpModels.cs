namespace MockErp.Api.Models;

public sealed record MockErpOrder(
    string Id,
    string ProductId,
    int Quantity,
    DateOnly RequestedDeliveryDate);

public sealed record MockErpProduct(
    string Id,
    string Name,
    string Unit);

public sealed record MockErpBomLine(
    string ComponentId,
    string Description,
    decimal Quantity,
    string Unit);
