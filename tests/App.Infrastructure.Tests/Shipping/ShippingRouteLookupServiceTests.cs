using App.Application.Abstractions.Shipping;
using App.Application.Contracts.Shipping;
using App.Infrastructure.Shipping;
using FluentAssertions;

namespace App.Infrastructure.Tests.Shipping;

public class ShippingRouteLookupServiceTests
{
    [Theory]
    [InlineData("İstanbul", 120)]
    [InlineData("Ankara", 480)]
    [InlineData("İzmir", 540)]
    [InlineData("Antalya", 600)]
    public async Task GetRouteAsync_GivenValidDestinationAndProfile_ShouldReturnRouteFound(string destination, int expectedDuration)
    {
        // Arrange
        var service = new ShippingRouteLookupService();

        // Act
        var result = await service.GetRouteAsync(destination, "DEFAULT");

        // Assert
        result.Should().BeOfType<ShippingRouteLookupResult.RouteFound>();
        var routeFound = (ShippingRouteLookupResult.RouteFound)result;
        routeFound.Route.Origin.Should().Be("İstanbul");
        routeFound.Route.Destination.Should().Be(destination);
        routeFound.Route.ShippingProfile.Should().Be("DEFAULT");
        routeFound.Route.DurationMinutes.Should().Be(expectedDuration);
    }

    [Theory]
    [InlineData("Bursa")]
    [InlineData("Adana")]
    [InlineData("Unknown")]
    [InlineData("")]
    public async Task GetRouteAsync_GivenInvalidDestination_ShouldReturnUnknownDestination(string destination)
    {
        // Arrange
        var service = new ShippingRouteLookupService();

        // Act
        var result = await service.GetRouteAsync(destination, "DEFAULT");

        // Assert
        result.Should().BeOfType<ShippingRouteLookupResult.UnknownDestination>();
        var unknownDest = (ShippingRouteLookupResult.UnknownDestination)result;
        unknownDest.Destination.Should().Be(destination);
    }

    [Fact]
    public async Task GetRouteAsync_GivenValidDestinationButUnknownProfile_ShouldReturnUnknownRoute()
    {
        // Arrange
        var service = new ShippingRouteLookupService();

        // Act
        var result = await service.GetRouteAsync("Ankara", "UNKNOWN_PROFILE");

        // Assert
        result.Should().BeOfType<ShippingRouteLookupResult.UnknownRoute>();
        var unknownRoute = (ShippingRouteLookupResult.UnknownRoute)result;
        unknownRoute.Origin.Should().Be("İstanbul");
        unknownRoute.Destination.Should().Be("Ankara");
        unknownRoute.Profile.Should().Be("UNKNOWN_PROFILE");
    }

    [Fact]
    public void Constructor_GivenNegativeDuration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidRoutes = new[]
        {
            new ShippingRouteReadModel("İstanbul", "Ankara", "DEFAULT", -10)
        };

        // Act
        Action act = () => new ShippingRouteLookupService(invalidRoutes);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*must be positive*");
    }

    [Fact]
    public void Constructor_GivenZeroDuration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidRoutes = new[]
        {
            new ShippingRouteReadModel("İstanbul", "İzmir", "DEFAULT", 0)
        };

        // Act
        Action act = () => new ShippingRouteLookupService(invalidRoutes);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*must be positive*");
    }

    [Fact]
    public void Constructor_GivenDuplicateRoutes_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var duplicateRoutes = new[]
        {
            new ShippingRouteReadModel("İstanbul", "Ankara", "DEFAULT", 100),
            new ShippingRouteReadModel("İstanbul", "Ankara", "DEFAULT", 200) // Duplicate with same profile!
        };

        // Act
        Action act = () => new ShippingRouteLookupService(duplicateRoutes);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Duplicate route defined*");
    }

    [Fact]
    public void Constructor_GivenInvalidOrigin_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidRoutes = new[]
        {
            new ShippingRouteReadModel("Ankara", "İzmir", "DEFAULT", 100) // Origin must be İstanbul
        };

        // Act
        Action act = () => new ShippingRouteLookupService(invalidRoutes);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Origin must be*");
    }
}
