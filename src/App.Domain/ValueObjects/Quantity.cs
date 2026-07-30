namespace App.Domain.ValueObjects;

/// <summary>
/// Immutable, self-validating quantity (SAD §9.4). Zero is valid; negative values are rejected.
/// </summary>
public sealed record Quantity
{
    public decimal Value { get; }

    public Quantity(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Quantity cannot be negative.");
        }

        Value = value;
    }
}
