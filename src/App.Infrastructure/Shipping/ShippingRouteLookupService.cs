using App.Application.Abstractions.Shipping;
using App.Application.Contracts.Shipping;

namespace App.Infrastructure.Shipping;

public sealed class ShippingRouteLookupService : IShippingRouteLookupService
{
    private const string FixedOrigin = "İstanbul";
    private readonly Dictionary<(string Destination, string Profile), ShippingRouteReadModel> _routes;
    private readonly HashSet<string> _validDestinations;

    public ShippingRouteLookupService() : this(null)
    {
    }

    public ShippingRouteLookupService(IEnumerable<ShippingRouteReadModel>? routes)
    {
        var rawRoutes = routes ?? new[]
        {
            new ShippingRouteReadModel(FixedOrigin, "İstanbul", "DEFAULT", 120),
            new ShippingRouteReadModel(FixedOrigin, "Ankara", "DEFAULT", 480),
            new ShippingRouteReadModel(FixedOrigin, "İzmir", "DEFAULT", 540),
            new ShippingRouteReadModel(FixedOrigin, "Antalya", "DEFAULT", 600)
        };

        var comparer = StringComparer.OrdinalIgnoreCase;
        _routes = new Dictionary<(string, string), ShippingRouteReadModel>(
            new TupleEqualityComparer(comparer));

        _validDestinations = new HashSet<string>(comparer)
        {
            "İstanbul", "Ankara", "İzmir", "Antalya"
        };

        foreach (var route in rawRoutes)
        {
            if (!string.Equals(route.Origin, FixedOrigin, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Invalid origin '{route.Origin}'. Origin must be '{FixedOrigin}'.");
            }

            if (!_validDestinations.Contains(route.Destination))
            {
                throw new InvalidOperationException($"Invalid destination '{route.Destination}'. Valid destinations are: {string.Join(", ", _validDestinations)}.");
            }

            if (route.DurationMinutes <= 0)
            {
                throw new InvalidOperationException($"Duration for route {route.Origin} -> {route.Destination} must be positive. Provided: {route.DurationMinutes}.");
            }

            if (string.IsNullOrWhiteSpace(route.ShippingProfile))
            {
                throw new InvalidOperationException($"Shipping profile cannot be empty for route {route.Origin} -> {route.Destination}.");
            }

            var key = (route.Destination, route.ShippingProfile);
            if (!_routes.TryAdd(key, route))
            {
                throw new InvalidOperationException($"Duplicate route defined for destination '{route.Destination}' with profile '{route.ShippingProfile}'.");
            }
        }
    }

    public Task<ShippingRouteLookupResult> GetRouteAsync(
        string destination,
        string profile,
        CancellationToken cancellationToken = default)
    {
        if (!_validDestinations.Contains(destination))
        {
            return Task.FromResult<ShippingRouteLookupResult>(new ShippingRouteLookupResult.UnknownDestination(destination));
        }

        var key = (destination, profile);
        if (_routes.TryGetValue(key, out var route))
        {
            return Task.FromResult<ShippingRouteLookupResult>(new ShippingRouteLookupResult.RouteFound(route));
        }

        return Task.FromResult<ShippingRouteLookupResult>(new ShippingRouteLookupResult.UnknownRoute(FixedOrigin, destination, profile));
    }

    private sealed class TupleEqualityComparer : IEqualityComparer<(string, string)>
    {
        private readonly StringComparer _comparer;

        public TupleEqualityComparer(StringComparer comparer)
        {
            _comparer = comparer;
        }

        public bool Equals((string, string) x, (string, string) y)
        {
            return _comparer.Equals(x.Item1, y.Item1) && _comparer.Equals(x.Item2, y.Item2);
        }

        public int GetHashCode((string, string) obj)
        {
            return HashCode.Combine(_comparer.GetHashCode(obj.Item1), _comparer.GetHashCode(obj.Item2));
        }
    }
}
