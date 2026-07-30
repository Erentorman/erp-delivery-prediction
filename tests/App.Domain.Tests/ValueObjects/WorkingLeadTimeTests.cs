using App.Domain.ValueObjects;

namespace App.Domain.Tests.ValueObjects;

public class WorkingLeadTimeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(480)]
    public void Constructor_WithZeroOrPositiveMinutes_CreatesInstance(long minutes)
    {
        var leadTime = new WorkingLeadTime(minutes);

        Assert.Equal(minutes, leadTime.Minutes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-480)]
    public void Constructor_WithNegativeMinutes_ThrowsArgumentOutOfRangeException(long minutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkingLeadTime(minutes));
    }

    [Fact]
    public void Equals_WithSameMinutes_ReturnsTrue()
    {
        var first = new WorkingLeadTime(120);
        var second = new WorkingLeadTime(120);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Equals_WithDifferentMinutes_ReturnsFalse()
    {
        var first = new WorkingLeadTime(60);
        var second = new WorkingLeadTime(120);

        Assert.NotEqual(first, second);
    }
}
