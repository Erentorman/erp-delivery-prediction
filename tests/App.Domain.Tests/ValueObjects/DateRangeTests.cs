using App.Domain.ValueObjects;

namespace App.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_WithStartBeforeEnd_CreatesInstance()
    {
        var start = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 7, 2, 8, 0, 0, TimeSpan.Zero);

        var range = new DateRange(start, end);

        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
    }

    [Fact]
    public void Constructor_WithEqualStartAndEnd_CreatesZeroLengthRange()
    {
        var point = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

        var range = new DateRange(point, point);

        Assert.Equal(TimeSpan.Zero, range.Duration);
    }

    [Fact]
    public void Constructor_WithEndBeforeStart_ThrowsArgumentException()
    {
        var start = new DateTimeOffset(2026, 7, 2, 8, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new DateRange(start, end));
    }

    [Fact]
    public void Duration_ReturnsDifferenceBetweenStartAndEnd()
    {
        var start = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

        var range = new DateRange(start, end);

        Assert.Equal(TimeSpan.FromDays(2), range.Duration);
    }

    [Fact]
    public void Equals_WithSameStartAndEnd_ReturnsTrue()
    {
        var start = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 7, 2, 8, 0, 0, TimeSpan.Zero);

        var first = new DateRange(start, end);
        var second = new DateRange(start, end);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Equals_WithDifferentEnd_ReturnsFalse()
    {
        var start = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

        var first = new DateRange(start, start.AddDays(1));
        var second = new DateRange(start, start.AddDays(2));

        Assert.NotEqual(first, second);
    }
}
