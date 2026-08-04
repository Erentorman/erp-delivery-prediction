namespace App.Domain.Prediction;

public sealed record OrderInput(
    string OrderReference,
    string ProductReference,
    decimal Quantity,
    DateTimeOffset RequestedDeliveryDate);
