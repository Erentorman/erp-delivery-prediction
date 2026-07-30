using App.Domain.ValueObjects;

namespace App.Domain.Tests.ValueObjects;

public class QuantityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12.5)]
    public void Constructor_WithZeroOrPositiveValue_CreatesInstance(decimal value)
    {
        var quantity = new Quantity(value);

        Assert.Equal(value, quantity.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Constructor_WithNegativeValue_ThrowsArgumentOutOfRangeException(decimal value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(value));
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var first = new Quantity(10.5m);
        var second = new Quantity(10.5m);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        var first = new Quantity(10m);
        var second = new Quantity(20m);

        Assert.NotEqual(first, second);
    }
}
