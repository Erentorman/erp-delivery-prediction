namespace App.Application.Abstractions.Shipping;

public abstract record ShippingRouteLookupResult
{
    private ShippingRouteLookupResult() { }

    public sealed record Found : ShippingRouteLookupResult
    {
        public Found(long shippingDurationMinutes)
        {
            if (shippingDurationMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shippingDurationMinutes),
                    shippingDurationMinutes,
                    "A found shipping route must have a positive duration.");
            }

            ShippingDurationMinutes = shippingDurationMinutes;
        }

        public long ShippingDurationMinutes { get; }
    }

    public sealed record NotFound : ShippingRouteLookupResult;
}
